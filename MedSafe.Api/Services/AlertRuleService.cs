using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public class AlertRuleService : IAlertRuleService
{
    // Rules whose urgency carries this code count as "critical" for the summary
    // KPI card — there's no separate criticality flag on the schema yet.
    private const string CriticalUrgencyCode = "ESCALATED";

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AlertRuleService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreateAlertRuleResponse> CreateAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request.Name, request.MatchModeId, request.UrgencyId, request.Conditions, request.Recipients, cancellationToken);

        var currentUserId = _currentUser.UserId;

        var rule = new AlertRule
        {
            RuleId = GenerateRuleId(),
            Name = request.Name.Trim(),
            MatchModeId = request.MatchModeId,
            UrgencyId = request.UrgencyId,
            NotificationTitle = request.NotificationTitle.Trim(),
            NotificationMessage = request.NotificationMessage.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Enabled = request.IsEnabled,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            // Legacy fields — superseded by the builder-based columns above.
            TriggerCondition = string.Empty,
            TargetRoles = string.Empty,
            Urgency = string.Empty,
            DeliveryConfig = null
        };

        var recipients = request.Recipients
            .DistinctBy(x => x.RecipientUserId)
            .ToList();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.AlertRules.Add(rule);
            await _db.SaveChangesAsync(cancellationToken);

            await InsertConditionsAsync(rule.Id, request.Conditions, cancellationToken);
            await InsertRecipientsAsync(rule.Id, recipients, currentUserId, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new CreateAlertRuleResponse
        {
            Id = rule.Id,
            RuleId = rule.RuleId,
            Name = rule.Name,
            IsEnabled = rule.Enabled,
            ConditionCount = request.Conditions.Count,
            RecipientCount = recipients.Count
        };
    }

    public async Task<List<AlertRuleListItemDto>> GetAllAsync(string? search, bool? isEnabled, int? urgencyId, CancellationToken cancellationToken)
    {
        var query = _db.AlertRules
            .Where(r => !r.IsDeleted)
            .Include(r => r.MatchMode)
            .Include(r => r.NotificationUrgency)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.Name.Contains(term) || r.RuleId.Contains(term));
        }
        if (isEnabled.HasValue)
            query = query.Where(r => r.Enabled == isEnabled.Value);
        if (urgencyId.HasValue)
            query = query.Where(r => r.UrgencyId == urgencyId.Value);

        var rules = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
        var ruleIds = rules.Select(r => r.Id).ToList();

        var conditionCounts = await _db.AlertRuleConditions
            .Where(c => ruleIds.Contains(c.AlertRuleId))
            .GroupBy(c => c.AlertRuleId)
            .Select(g => new { AlertRuleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AlertRuleId, x => x.Count, cancellationToken);

        var recipientCounts = await _db.AlertRuleRecipients
            .Where(r => ruleIds.Contains(r.AlertRuleId))
            .GroupBy(r => r.AlertRuleId)
            .Select(g => new { AlertRuleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AlertRuleId, x => x.Count, cancellationToken);

        var reportTypeValues = await _db.AlertRuleConditions
            .Where(c => ruleIds.Contains(c.AlertRuleId) && c.ConditionField.Code == "REPORT_TYPE")
            .SelectMany(c => c.Values.Select(v => new { c.AlertRuleId, v.TextValue }))
            .ToListAsync(cancellationToken);
        var appliesToByRule = reportTypeValues
            .GroupBy(x => x.AlertRuleId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.TextValue).Where(v => !string.IsNullOrEmpty(v)).Distinct()));

        return rules.Select(r => new AlertRuleListItemDto
        {
            Id = r.Id,
            RuleId = r.RuleId,
            Name = r.Name,
            MatchMode = r.MatchMode == null ? null : new AlertRuleMatchModeDto { Id = r.MatchMode.Id, Code = r.MatchMode.Code, Name = r.MatchMode.Name },
            Urgency = r.NotificationUrgency == null ? null : new AlertRuleUrgencyDto { Id = r.NotificationUrgency.Id, Code = r.NotificationUrgency.Code, Name = r.NotificationUrgency.Name },
            NotificationTitle = r.NotificationTitle,
            NotificationMessage = r.NotificationMessage,
            Description = r.Description,
            IsEnabled = r.Enabled,
            ConditionCount = conditionCounts.GetValueOrDefault(r.Id),
            RecipientCount = recipientCounts.GetValueOrDefault(r.Id),
            AppliesTo = string.IsNullOrEmpty(appliesToByRule.GetValueOrDefault(r.Id)) ? "All Types" : appliesToByRule[r.Id],
            LastTriggeredAt = r.LastTriggered,
            CreatedAt = r.CreatedAt,
        }).ToList();
    }

    public async Task<AlertRuleDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var rule = await _db.AlertRules
            .Include(r => r.MatchMode)
            .Include(r => r.NotificationUrgency)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (rule == null) return null;

        var conditions = await _db.AlertRuleConditions
            .Include(c => c.ConditionField)
            .Include(c => c.Operator)
            .Include(c => c.Values)
            .Where(c => c.AlertRuleId == id)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        var conditionDtos = new List<AlertRuleConditionDetailDto>();
        foreach (var condition in conditions)
        {
            var valueDtos = new List<AlertRuleConditionValueDetailDto>();
            foreach (var value in condition.Values.OrderBy(v => v.DisplayOrder))
            {
                valueDtos.Add(new AlertRuleConditionValueDetailDto
                {
                    Id = value.Id,
                    LookupValueId = value.LookupValueId,
                    TextValue = value.TextValue,
                    DisplayValue = await ResolveDisplayValueAsync(condition.ConditionField.Code, value.LookupValueId, value.TextValue, cancellationToken)
                });
            }

            conditionDtos.Add(new AlertRuleConditionDetailDto
            {
                Id = condition.Id,
                ConditionFieldId = condition.ConditionFieldId,
                FieldCode = condition.ConditionField.Code,
                FieldName = condition.ConditionField.Name,
                OperatorId = condition.OperatorId,
                OperatorCode = condition.Operator.Code,
                OperatorName = condition.Operator.Name,
                Values = valueDtos
            });
        }

        var recipients = await _db.AlertRuleRecipients
            .Include(r => r.RecipientType)
            .Include(r => r.RecipientUser)
            .Where(r => r.AlertRuleId == id)
            .ToListAsync(cancellationToken);

        var recipientDtos = recipients.Select(r => new AlertRuleRecipientDetailDto
        {
            Id = r.Id,
            RecipientTypeId = r.RecipientTypeId,
            RecipientTypeCode = r.RecipientType.Code,
            RecipientTypeName = r.RecipientType.Name,
            RecipientUserId = r.RecipientUserId,
            RecipientName = r.RecipientUser.Name,
            Email = r.RecipientUser.Email,
            Role = r.RecipientUser.Role,
            Title = r.RecipientUser.Title,
            Unit = r.RecipientUser.Unit
        }).ToList();

        return new AlertRuleDetailDto
        {
            Id = rule.Id,
            RuleId = rule.RuleId,
            Name = rule.Name,
            MatchMode = rule.MatchMode == null ? null : new AlertRuleMatchModeDto { Id = rule.MatchMode.Id, Code = rule.MatchMode.Code, Name = rule.MatchMode.Name },
            Urgency = rule.NotificationUrgency == null ? null : new AlertRuleUrgencyDto { Id = rule.NotificationUrgency.Id, Code = rule.NotificationUrgency.Code, Name = rule.NotificationUrgency.Name },
            NotificationTitle = rule.NotificationTitle,
            NotificationMessage = rule.NotificationMessage,
            Description = rule.Description,
            IsEnabled = rule.Enabled,
            Conditions = conditionDtos,
            Recipients = recipientDtos,
            LastTriggeredAt = rule.LastTriggered,
            CreatedAt = rule.CreatedAt,
            ModifiedAt = rule.ModifiedAt
        };
    }

    public async Task<UpdateAlertRuleResponse> UpdateAsync(int id, UpdateAlertRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _db.AlertRules.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (rule == null)
            throw new KeyNotFoundException("Alert rule not found.");

        await ValidateAsync(request.Name, request.MatchModeId, request.UrgencyId, request.Conditions, request.Recipients, cancellationToken);

        var currentUserId = _currentUser.UserId;
        var recipients = request.Recipients
            .DistinctBy(x => x.RecipientUserId)
            .ToList();
        var now = DateTime.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            rule.Name = request.Name.Trim();
            rule.MatchModeId = request.MatchModeId;
            rule.UrgencyId = request.UrgencyId;
            rule.NotificationTitle = request.NotificationTitle.Trim();
            rule.NotificationMessage = request.NotificationMessage.Trim();
            rule.Description = request.Description?.Trim() ?? string.Empty;
            rule.Enabled = request.IsEnabled;
            rule.ModifiedByUserId = currentUserId;
            rule.ModifiedAt = now;

            // Replace wholesale — simplest way to keep DisplayOrder/values consistent
            // with whatever the frontend just resent, rather than diffing row-by-row.
            var existingConditionIds = await _db.AlertRuleConditions
                .Where(c => c.AlertRuleId == id)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            if (existingConditionIds.Count > 0)
            {
                await _db.AlertRuleConditionValues
                    .Where(v => existingConditionIds.Contains(v.AlertRuleConditionId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.AlertRuleConditions
                    .Where(c => c.AlertRuleId == id)
                    .ExecuteDeleteAsync(cancellationToken);
            }
            await _db.AlertRuleRecipients
                .Where(r => r.AlertRuleId == id)
                .ExecuteDeleteAsync(cancellationToken);

            await InsertConditionsAsync(id, request.Conditions, cancellationToken);
            await InsertRecipientsAsync(id, recipients, currentUserId, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new UpdateAlertRuleResponse
        {
            Id = rule.Id,
            RuleId = rule.RuleId,
            Name = rule.Name,
            IsEnabled = rule.Enabled,
            ConditionCount = request.Conditions.Count,
            RecipientCount = recipients.Count,
            ModifiedAt = now
        };
    }

    public async Task<AlertRuleSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var totalRules = await _db.AlertRules.CountAsync(r => !r.IsDeleted, cancellationToken);
        var activeRules = await _db.AlertRules.CountAsync(r => !r.IsDeleted && r.Enabled, cancellationToken);
        var criticalRules = await _db.AlertRules
            .Include(r => r.NotificationUrgency)
            .CountAsync(r => !r.IsDeleted && r.NotificationUrgency != null && r.NotificationUrgency.Code == CriticalUrgencyCode, cancellationToken);

        // Real trigger-event count (AlertTriggerHistory), not a proxy off
        // AlertRules.LastTriggered — a rule that fires 15 times today now
        // correctly counts as 15, not 1.
        var since = DateTime.UtcNow.AddHours(-24);
        var triggeredLast24h = await _db.AlertTriggerHistories
            .CountAsync(t => t.TriggeredAt >= since, cancellationToken);

        return new AlertRuleSummaryDto
        {
            TotalRules = totalRules,
            ActiveRules = activeRules,
            InactiveRules = totalRules - activeRules,
            CriticalRules = criticalRules,
            AlertsTriggeredLast24Hours = triggeredLast24h,
            FieldUsage = await GetFieldUsageAsync(cancellationToken)
        };
    }

    public async Task<List<AlertRuleFieldUsageDto>> GetFieldUsageAsync(CancellationToken cancellationToken)
    {
        return await _db.AlertConditionFields
            .Where(f => f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .Select(f => new AlertRuleFieldUsageDto
            {
                FieldId = f.Id,
                FieldCode = f.Code,
                FieldName = f.Name,
                RuleCount = _db.AlertRuleConditions
                    .Where(c => c.ConditionFieldId == f.Id && !c.AlertRule.IsDeleted)
                    .Select(c => c.AlertRuleId)
                    .Distinct()
                    .Count()
            })
            .ToListAsync(cancellationToken);
    }

    private async Task InsertConditionsAsync(int alertRuleId, List<CreateAlertRuleConditionRequest> conditions, CancellationToken cancellationToken)
    {
        var conditionOrder = 1;
        foreach (var conditionRequest in conditions)
        {
            var condition = new AlertRuleCondition
            {
                AlertRuleId = alertRuleId,
                ConditionFieldId = conditionRequest.ConditionFieldId,
                OperatorId = conditionRequest.OperatorId,
                DisplayOrder = conditionOrder++
            };
            _db.AlertRuleConditions.Add(condition);
            await _db.SaveChangesAsync(cancellationToken);

            var valueOrder = 1;
            foreach (var valueRequest in conditionRequest.Values)
            {
                _db.AlertRuleConditionValues.Add(new AlertRuleConditionValue
                {
                    AlertRuleConditionId = condition.Id,
                    LookupValueId = valueRequest.LookupValueId,
                    TextValue = valueRequest.TextValue?.Trim(),
                    DisplayOrder = valueOrder++
                });
            }
        }
    }

    private async Task InsertRecipientsAsync(int alertRuleId, List<CreateAlertRuleRecipientRequest> recipients, int currentUserId, CancellationToken cancellationToken)
    {
        foreach (var recipient in recipients)
        {
            _db.AlertRuleRecipients.Add(new AlertRuleRecipient
            {
                AlertRuleId = alertRuleId,
                RecipientTypeId = recipient.RecipientTypeId,
                RecipientUserId = recipient.RecipientUserId,
                IsActive = true,
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            });
        }
        await Task.CompletedTask;
    }

    // Turns a stored condition value back into a human-readable label — a lookup
    // id resolves against the field's own lookup table, a free-typed value gets a
    // nicer label only for HARM_LEVEL (matches GetBuilderValues' "Category X" labels).
    private async Task<string> ResolveDisplayValueAsync(string fieldCode, int? lookupValueId, string? textValue, CancellationToken cancellationToken)
    {
        if (lookupValueId.HasValue)
        {
            string? name = fieldCode switch
            {
                "ERROR_CATEGORY" => (await _db.ErrorCategories.FirstOrDefaultAsync(x => x.Id == lookupValueId.Value, cancellationToken))?.Name,
                "STAGE_OF_PROCESS" => (await _db.StageOfProcesses.FirstOrDefaultAsync(x => x.Id == lookupValueId.Value, cancellationToken))?.Name,
                "PATIENT_OUTCOME" => (await _db.PatientOutcomes.FirstOrDefaultAsync(x => x.Id == lookupValueId.Value, cancellationToken))?.Name,
                "SERIOUSNESS_CRITERIA" => (await _db.SeriousnessCriteria.FirstOrDefaultAsync(x => x.Id == lookupValueId.Value, cancellationToken))?.Name,
                "CONTRIBUTING_FACTOR" => (await _db.ContributingFactors.FirstOrDefaultAsync(x => x.Id == lookupValueId.Value, cancellationToken))?.Name,
                _ => null
            };
            return name ?? lookupValueId.Value.ToString();
        }

        if (fieldCode == "HARM_LEVEL" && !string.IsNullOrEmpty(textValue))
            return $"Category {textValue}";

        return textValue ?? string.Empty;
    }

    private async Task ValidateAsync(
        string name,
        int matchModeId,
        int urgencyId,
        List<CreateAlertRuleConditionRequest> conditions,
        List<CreateAlertRuleRecipientRequest> recipients,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Name is required.");

        var matchModeValid = await _db.AlertRuleMatchModes
            .AnyAsync(x => x.Id == matchModeId && x.IsActive, cancellationToken);
        if (!matchModeValid)
            throw new ValidationException("Selected match mode is not valid.");

        var urgencyValid = await _db.NotificationUrgencies
            .AnyAsync(x => x.Id == urgencyId && x.IsActive, cancellationToken);
        if (!urgencyValid)
            throw new ValidationException("Selected urgency is not valid.");

        if (conditions.Count == 0)
            throw new ValidationException("At least one condition is required.");

        foreach (var condition in conditions)
        {
            var fieldValid = await _db.AlertConditionFields
                .AnyAsync(x => x.Id == condition.ConditionFieldId && x.IsActive, cancellationToken);
            if (!fieldValid)
                throw new ValidationException("Selected condition field is not valid.");

            var operatorValid = await _db.AlertConditionOperators
                .AnyAsync(x => x.Id == condition.OperatorId && x.IsActive, cancellationToken);
            if (!operatorValid)
                throw new ValidationException("Selected operator is not valid.");

            var validCombination = await _db.AlertConditionFieldOperators
                .AnyAsync(x =>
                    x.ConditionFieldId == condition.ConditionFieldId &&
                    x.OperatorId == condition.OperatorId,
                    cancellationToken);
            if (!validCombination)
                throw new ValidationException("Selected operator is not allowed for this condition field.");

            if (condition.Values.Count == 0)
                throw new ValidationException("Every condition must have at least one value.");
        }

        if (recipients.Count == 0)
            throw new ValidationException("At least one recipient is required.");

        foreach (var recipient in recipients)
        {
            var recipientTypeValid = await _db.NotificationRecipientTypes
                .AnyAsync(x => x.Id == recipient.RecipientTypeId && x.IsActive, cancellationToken);
            if (!recipientTypeValid)
                throw new ValidationException("Selected recipient type is not valid.");

            var recipientUser = await _db.Users
                .FirstOrDefaultAsync(x => x.Id == recipient.RecipientUserId, cancellationToken);
            if (recipientUser == null)
                throw new ValidationException("Selected recipient user does not exist.");
            if (recipientUser.Status != "active")
                throw new ValidationException($"Recipient user \"{recipientUser.Name}\" is not active.");
        }
    }

    // Concurrency-safe — no COUNT()+1 race between two rules being created at once.
    private static string GenerateRuleId() => $"RULE-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
