using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public class AlertDashboardService : IAlertDashboardService
{
    private static readonly string[] CriticalUrgencyCodes = ["IMMEDIATE", "ESCALATED"];
    private const int MaxExportRows = 10000;

    private readonly AppDbContext _db;

    public AlertDashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AlertDashboardOverviewDto> GetOverviewAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var toDate = (to ?? DateTime.UtcNow).Date;
        var fromDate = (from ?? toDate.AddDays(-6)).Date;
        var rangeEndExclusive = toDate.AddDays(1);

        var totalRules = await _db.AlertRules.CountAsync(x => !x.IsDeleted, cancellationToken);
        var activeRules = await _db.AlertRules.CountAsync(x => !x.IsDeleted && x.Enabled, cancellationToken);

        var triggersInRange = _db.AlertTriggerHistories
            .Where(x => x.TriggeredAt >= fromDate && x.TriggeredAt < rangeEndExclusive);

        var alertsTriggered = await triggersInRange.CountAsync(cancellationToken);

        var criticalAlerts = await triggersInRange
            .Where(x => x.Urgency != null && CriticalUrgencyCodes.Contains(x.Urgency.Code))
            .CountAsync(cancellationToken);

        var notificationsSent = await _db.IncidentNotifications
            .Where(x => x.AlertTriggerId != null && x.Status == "SENT"
                && x.AlertTrigger!.TriggeredAt >= fromDate && x.AlertTrigger.TriggeredAt < rangeEndExclusive)
            .CountAsync(cancellationToken);

        var uniqueRecipients = await _db.IncidentNotifications
            .Where(x => x.AlertTriggerId != null && x.RecipientUserId != null
                && x.AlertTrigger!.TriggeredAt >= fromDate && x.AlertTrigger.TriggeredAt < rangeEndExclusive)
            .Select(x => x.RecipientUserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var trendRaw = await triggersInRange
            .GroupBy(x => x.TriggeredAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var trendMap = trendRaw.ToDictionary(x => x.Date, x => x.Count);
        var alertsOverTime = new List<AlertTrendPointDto>();
        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
            alertsOverTime.Add(new AlertTrendPointDto { Date = d, Count = trendMap.TryGetValue(d, out var c) ? c : 0 });

        var alertsByStatus = await triggersInRange
            .GroupBy(x => x.Status)
            .Select(g => new AlertStatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var alertsByRule = await triggersInRange
            .GroupBy(x => new { x.AlertRuleId, x.AlertRule.RuleId, x.AlertRule.Name })
            .Select(g => new AlertRuleCountDto { AlertRuleId = g.Key.AlertRuleId, RuleId = g.Key.RuleId, RuleName = g.Key.Name, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(cancellationToken);

        var notificationsByChannel = await _db.IncidentNotifications
            .Where(x => x.AlertTriggerId != null && x.NotificationMethod != null
                && x.AlertTrigger!.TriggeredAt >= fromDate && x.AlertTrigger.TriggeredAt < rangeEndExclusive)
            .GroupBy(x => new { x.NotificationMethod!.Code, x.NotificationMethod.Name })
            .Select(g => new NotificationChannelCountDto { MethodCode = g.Key.Code, MethodName = g.Key.Name, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new AlertDashboardOverviewDto
        {
            TotalRules = totalRules,
            ActiveRules = activeRules,
            CriticalAlerts = criticalAlerts,
            AlertsTriggered = alertsTriggered,
            NotificationsSent = notificationsSent,
            UniqueRecipients = uniqueRecipients,
            AlertsOverTime = alertsOverTime,
            AlertsByStatus = alertsByStatus,
            AlertsByRule = alertsByRule,
            NotificationsByChannel = notificationsByChannel
        };
    }

    private IQueryable<AlertTriggerHistory> BuildFilteredQuery(AlertTriggerFilterRequest request)
    {
        var query = _db.AlertTriggerHistories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                x.AlertTriggerNumber.Contains(term) ||
                x.AlertRule.Name.Contains(term) ||
                x.AlertRule.RuleId.Contains(term) ||
                x.IncidentReport.IncidentReportNumber.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(x => x.Status == request.Status);
        if (request.UrgencyId.HasValue) query = query.Where(x => x.UrgencyId == request.UrgencyId.Value);
        if (request.RuleId.HasValue) query = query.Where(x => x.AlertRuleId == request.RuleId.Value);
        if (!string.IsNullOrWhiteSpace(request.ReportType)) query = query.Where(x => x.IncidentReport.ReportType == request.ReportType);
        if (!string.IsNullOrWhiteSpace(request.TriggerSource)) query = query.Where(x => x.TriggerSource == request.TriggerSource);
        if (request.From.HasValue) query = query.Where(x => x.TriggeredAt >= request.From.Value.Date);
        if (request.To.HasValue) query = query.Where(x => x.TriggeredAt < request.To.Value.Date.AddDays(1));
        if (request.RecipientUserId.HasValue)
        {
            var recipientId = request.RecipientUserId.Value;
            query = query.Where(x => x.Notifications.Any(n => n.RecipientUserId == recipientId));
        }
        if (request.NotificationMethodId.HasValue)
        {
            var methodId = request.NotificationMethodId.Value;
            query = query.Where(x => x.Notifications.Any(n => n.NotificationMethodId == methodId));
        }
        if (request.ConditionFieldId.HasValue)
        {
            var fieldId = request.ConditionFieldId.Value;
            query = query.Where(x => x.AlertRule.Conditions.Any(c => c.ConditionFieldId == fieldId));
        }

        return query;
    }

    public async Task<AlertTriggerListResponse> GetTriggersAsync(AlertTriggerFilterRequest request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = BuildFilteredQuery(request)
            .Include(x => x.AlertRule)
            .Include(x => x.IncidentReport)
            .Include(x => x.Urgency);

        var totalCount = await query.CountAsync(cancellationToken);

        var triggers = await query
            .OrderByDescending(x => x.TriggeredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var triggerIds = triggers.Select(x => x.Id).ToList();

        var notifications = await _db.IncidentNotifications
            .AsNoTracking()
            .Include(x => x.NotificationMethod)
            .Where(x => x.AlertTriggerId != null && triggerIds.Contains(x.AlertTriggerId.Value))
            .ToListAsync(cancellationToken);
        var notificationsByTrigger = notifications.GroupBy(x => x.AlertTriggerId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var items = triggers.Select(t =>
        {
            var notifs = notificationsByTrigger.GetValueOrDefault(t.Id, []);
            return new AlertTriggerListItemDto
            {
                Id = t.Id,
                AlertId = t.AlertTriggerNumber,
                Rule = new AlertTriggerRuleRefDto { Id = t.AlertRule.Id, RuleId = t.AlertRule.RuleId, Name = t.AlertRule.Name },
                Report = new AlertTriggerReportRefDto { Id = t.IncidentReport.Id, ReportNumber = t.IncidentReport.IncidentReportNumber, ReportType = t.IncidentReport.ReportType },
                Urgency = t.Urgency == null ? null : new AlertTriggerUrgencyRefDto { Id = t.Urgency.Id, Code = t.Urgency.Code, Name = t.Urgency.Name },
                TriggerSource = t.TriggerSource,
                ConditionBase = t.ConditionSummary,
                TriggeredAt = t.TriggeredAt,
                RecipientCount = notifs.Select(n => n.RecipientUserId).Distinct().Count(),
                Channels = notifs.Where(n => n.NotificationMethod != null).Select(n => n.NotificationMethod!.Code).Distinct().ToList(),
                NotificationSummary = new AlertTriggerNotificationSummaryDto
                {
                    Sent = notifs.Count(n => n.Status == "SENT"),
                    Pending = notifs.Count(n => n.Status == "PENDING"),
                    Failed = notifs.Count(n => n.Status == "FAILED"),
                },
                Status = t.Status,
            };
        }).ToList();

        return new AlertTriggerListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<AlertTriggerDetailDto?> GetTriggerDetailAsync(long id, CancellationToken cancellationToken)
    {
        var trigger = await _db.AlertTriggerHistories
            .AsNoTracking()
            .Include(x => x.AlertRule).ThenInclude(r => r.MatchMode)
            .Include(x => x.IncidentReport)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (trigger == null) return null;

        var notifications = await _db.IncidentNotifications
            .AsNoTracking()
            .Include(x => x.RecipientUser)
            .Include(x => x.NotificationMethod)
            .Where(x => x.AlertTriggerId == id)
            .ToListAsync(cancellationToken);

        var recipientTypeNames = await _db.NotificationRecipientTypes.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var matchedConditions = new List<MatchedConditionDto>();
        if (!string.IsNullOrWhiteSpace(trigger.MatchedConditionSnapshot))
        {
            try
            {
                var raw = JsonSerializer.Deserialize<List<ConditionEvaluationResult>>(trigger.MatchedConditionSnapshot);
                matchedConditions = (raw ?? []).Select(r => new MatchedConditionDto
                {
                    Field = r.FieldCode,
                    FieldName = r.FieldName,
                    Operator = r.OperatorCode,
                    ExpectedValue = r.ExpectedValue,
                    ActualValue = r.ActualValue,
                    Matched = r.Matched
                }).ToList();
            }
            catch (JsonException)
            {
                // Older/legacy trigger rows may not carry a snapshot — show none rather than fail the request.
            }
        }

        return new AlertTriggerDetailDto
        {
            Id = trigger.Id,
            AlertId = trigger.AlertTriggerNumber,
            Rule = new AlertTriggerDetailRuleDto { Id = trigger.AlertRule.Id, RuleId = trigger.AlertRule.RuleId, Name = trigger.AlertRule.Name, MatchMode = trigger.AlertRule.MatchMode?.Name },
            Report = new AlertTriggerDetailReportDto { Id = trigger.IncidentReport.Id, ReportNumber = trigger.IncidentReport.IncidentReportNumber, ReportType = trigger.IncidentReport.ReportType, ReportStatus = trigger.IncidentReport.ReportStatus, Location = trigger.IncidentReport.IncidentLocation },
            Trigger = new AlertTriggerDetailTriggerDto { Source = trigger.TriggerSource, ConditionSummary = trigger.ConditionSummary, TriggeredAt = trigger.TriggeredAt, Status = trigger.Status },
            MatchedConditions = matchedConditions,
            Notifications = notifications.Select(n => new AlertTriggerNotificationDetailDto
            {
                NotificationId = n.Id,
                RecipientUserId = n.RecipientUserId,
                RecipientName = n.PersonName,
                Email = n.RecipientUser?.Email,
                RecipientType = recipientTypeNames.GetValueOrDefault(n.NotificationTypeId),
                Method = n.NotificationMethod?.Name,
                DeliveryStatus = n.Status,
                SentAt = n.SentAt,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                EmailAttemptCount = n.EmailAttemptCount,
                LastEmailAttemptAt = n.LastEmailAttemptAt,
                FailureReason = n.Status == "FAILED" ? n.Notes : null
            }).ToList()
        };
    }

    public async Task<AlertDashboardFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken)
    {
        var rules = await _db.AlertRules.AsNoTracking().Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new AlertDashboardFilterRuleDto { Id = x.Id, RuleId = x.RuleId, Name = x.Name })
            .ToListAsync(cancellationToken);

        var recipientIds = await _db.IncidentNotifications.AsNoTracking()
            .Where(x => x.AlertTriggerId != null && x.RecipientUserId != null)
            .Select(x => x.RecipientUserId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var recipients = await _db.Users.AsNoTracking()
            .Where(x => recipientIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new AlertDashboardFilterRecipientDto { Id = x.Id, Name = x.Name, Email = x.Email })
            .ToListAsync(cancellationToken);

        var urgencies = await _db.NotificationUrgencies.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new AlertDashboardFilterUrgencyDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);

        var methods = await _db.NotificationMethods.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new AlertDashboardFilterMethodDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);

        var conditionFields = await _db.AlertConditionFields.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new AlertDashboardFilterConditionFieldDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);

        return new AlertDashboardFilterOptionsDto
        {
            Rules = rules,
            Recipients = recipients,
            Urgencies = urgencies,
            Methods = methods,
            Statuses = ["OPEN", "ACKNOWLEDGED", "RESOLVED"],
            DeliveryStatuses = ["PENDING", "SENT", "FAILED"],
            ReportTypes = ["Medication Error", "Near Miss", "ADR"],
            TriggerSources =
            [
                new AlertDashboardFilterSourceDto { Code = "REPORT_SUBMISSION", Name = "Incident Report" },
                new AlertDashboardFilterSourceDto { Code = "SCHEDULED_48H", Name = "48 Hour Review Monitor" },
            ],
            ConditionFields = conditionFields
        };
    }

    public async Task<AlertTriggerStatusResponseDto?> AcknowledgeAsync(long id, int userId, CancellationToken cancellationToken)
    {
        var trigger = await _db.AlertTriggerHistories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (trigger == null) return null;

        trigger.Status = "ACKNOWLEDGED";
        trigger.AcknowledgedByUserId = userId;
        trigger.AcknowledgedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new AlertTriggerStatusResponseDto { Id = trigger.Id, Status = trigger.Status, AcknowledgedAt = trigger.AcknowledgedAt, ResolvedAt = trigger.ResolvedAt };
    }

    public async Task<AlertTriggerStatusResponseDto?> ResolveAsync(long id, int userId, CancellationToken cancellationToken)
    {
        var trigger = await _db.AlertTriggerHistories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (trigger == null) return null;

        trigger.Status = "RESOLVED";
        trigger.ResolvedByUserId = userId;
        trigger.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new AlertTriggerStatusResponseDto { Id = trigger.Id, Status = trigger.Status, AcknowledgedAt = trigger.AcknowledgedAt, ResolvedAt = trigger.ResolvedAt };
    }

    public async Task<List<AlertTriggerExportRowDto>> GetExportRowsAsync(AlertTriggerFilterRequest request, CancellationToken cancellationToken)
    {
        var triggers = await BuildFilteredQuery(request)
            .Include(x => x.AlertRule)
            .Include(x => x.IncidentReport)
            .Include(x => x.Urgency)
            .OrderByDescending(x => x.TriggeredAt)
            .Take(MaxExportRows)
            .ToListAsync(cancellationToken);

        var triggerIds = triggers.Select(x => x.Id).ToList();
        var notifications = await _db.IncidentNotifications
            .AsNoTracking()
            .Include(x => x.RecipientUser)
            .Include(x => x.NotificationMethod)
            .Where(x => x.AlertTriggerId != null && triggerIds.Contains(x.AlertTriggerId.Value))
            .ToListAsync(cancellationToken);
        var notificationsByTrigger = notifications.GroupBy(x => x.AlertTriggerId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<AlertTriggerExportRowDto>();
        foreach (var t in triggers)
        {
            var notifs = notificationsByTrigger.GetValueOrDefault(t.Id, []);
            if (notifs.Count == 0)
            {
                rows.Add(new AlertTriggerExportRowDto
                {
                    AlertId = t.AlertTriggerNumber,
                    RuleId = t.AlertRule.RuleId,
                    RuleName = t.AlertRule.Name,
                    ReportNumber = t.IncidentReport.IncidentReportNumber,
                    ReportType = t.IncidentReport.ReportType,
                    ConditionBase = t.ConditionSummary,
                    Urgency = t.Urgency?.Name,
                    TriggeredAt = t.TriggeredAt,
                    Recipient = "—",
                    AlertStatus = t.Status
                });
                continue;
            }

            foreach (var n in notifs)
            {
                rows.Add(new AlertTriggerExportRowDto
                {
                    AlertId = t.AlertTriggerNumber,
                    RuleId = t.AlertRule.RuleId,
                    RuleName = t.AlertRule.Name,
                    ReportNumber = t.IncidentReport.IncidentReportNumber,
                    ReportType = t.IncidentReport.ReportType,
                    ConditionBase = t.ConditionSummary,
                    Urgency = t.Urgency?.Name,
                    TriggeredAt = t.TriggeredAt,
                    Recipient = n.PersonName,
                    Email = n.RecipientUser?.Email,
                    Method = n.NotificationMethod?.Name,
                    DeliveryStatus = n.Status,
                    SentAt = n.SentAt,
                    AlertStatus = t.Status
                });
            }
        }

        return rows;
    }
}
