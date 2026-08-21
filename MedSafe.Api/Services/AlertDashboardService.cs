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
    private readonly ICurrentUserService _currentUser;
    private readonly IAlertRuleService _alertRuleService;

    public AlertDashboardService(AppDbContext db, ICurrentUserService currentUser, IAlertRuleService alertRuleService)
    {
        _db = db;
        _currentUser = currentUser;
        _alertRuleService = alertRuleService;
    }

    // Same day/hour bucketing as the AlertsOverTime trend, applied to any raw
    // timestamp list — powers the KPI card sparklines.
    private static List<int> BucketCounts(List<DateTime> timestamps, DateTime fromDate, DateTime toDate)
    {
        var counts = new List<int>();
        if (fromDate == toDate)
        {
            var byHour = timestamps.GroupBy(t => t.Hour).ToDictionary(g => g.Key, g => g.Count());
            for (var h = 0; h < 24; h++) counts.Add(byHour.TryGetValue(h, out var c) ? c : 0);
        }
        else
        {
            var byDay = timestamps.GroupBy(t => t.Date).ToDictionary(g => g.Key, g => g.Count());
            for (var d = fromDate; d <= toDate; d = d.AddDays(1)) counts.Add(byDay.TryGetValue(d, out var c) ? c : 0);
        }
        return counts;
    }

    // Admin sees every alert system-wide. Everyone else gets exactly the same
    // "apna apna" scope already used for the Reports Hub's default view
    // (IncidentReportService.ApplyScopeFilter): alerts tied to a report they
    // submitted, review, or were personally notified about — not other users'.
    private IQueryable<AlertTriggerHistory> ApplyUserScope(IQueryable<AlertTriggerHistory> query)
    {
        if (_currentUser.Role == "Admin") return query;
        var userId = _currentUser.UserId;
        return query.Where(x =>
            x.IncidentReport.SubmittedByUserId == userId ||
            (x.IncidentReport.Review != null && x.IncidentReport.Review.ReviewerUserId == userId) ||
            x.Notifications.Any(n => n.RecipientUserId == userId));
    }

    private IQueryable<IncidentNotification> ApplyNotificationUserScope(IQueryable<IncidentNotification> query)
    {
        if (_currentUser.Role == "Admin") return query;
        var userId = _currentUser.UserId;
        return query.Where(x =>
            x.IncidentReport.SubmittedByUserId == userId ||
            (x.IncidentReport.Review != null && x.IncidentReport.Review.ReviewerUserId == userId) ||
            x.RecipientUserId == userId);
    }

    public async Task<AlertDashboardOverviewDto> GetOverviewAsync(DateTime? from, DateTime? to, string? reportType, CancellationToken cancellationToken)
    {
        var toDate = (to ?? DateTime.UtcNow).Date;
        var fromDate = (from ?? toDate.AddDays(-6)).Date;
        var rangeEndExclusive = toDate.AddDays(1);
        var hasReportType = !string.IsNullOrWhiteSpace(reportType);

        var totalRules = await _db.AlertRules.CountAsync(x => !x.IsDeleted, cancellationToken);
        var activeRules = await _db.AlertRules.CountAsync(x => !x.IsDeleted && x.Enabled, cancellationToken);
        var immediateEscalatedRules = await _db.AlertRules
            .Include(r => r.NotificationUrgency)
            .CountAsync(r => !r.IsDeleted && r.NotificationUrgency != null && CriticalUrgencyCodes.Contains(r.NotificationUrgency.Code), cancellationToken);
        // Which report type a rule is "for" is read off its own REPORT_TYPE
        // condition value(s) — not from trigger history — so a rule with no
        // REPORT_TYPE condition configured counts toward neither.
        var medicationErrorRules = await _db.AlertRules
            .CountAsync(r => !r.IsDeleted && r.Conditions.Any(c => c.ConditionField.Code == "REPORT_TYPE" && c.Values.Any(v => v.TextValue == "Medication Error")), cancellationToken);
        var adrRules = await _db.AlertRules
            .CountAsync(r => !r.IsDeleted && r.Conditions.Any(c => c.ConditionField.Code == "REPORT_TYPE" && c.Values.Any(v => v.TextValue == "ADR")), cancellationToken);
        var fieldUsage = await _alertRuleService.GetFieldUsageAsync(cancellationToken);

        var triggersInRange = ApplyUserScope(_db.AlertTriggerHistories)
            .Where(x => x.TriggeredAt >= fromDate && x.TriggeredAt < rangeEndExclusive
                && (!hasReportType || x.IncidentReport.ReportType == reportType));

        var alertsTriggered = await triggersInRange.CountAsync(cancellationToken);

        var criticalAlerts = await triggersInRange
            .Where(x => x.Urgency != null && CriticalUrgencyCodes.Contains(x.Urgency.Code))
            .CountAsync(cancellationToken);

        var notificationsSent = await ApplyNotificationUserScope(_db.IncidentNotifications)
            .Where(x => x.AlertTriggerId != null && x.Status == "SENT"
                && x.AlertTrigger!.TriggeredAt >= fromDate && x.AlertTrigger.TriggeredAt < rangeEndExclusive
                && (!hasReportType || x.IncidentReport.ReportType == reportType))
            .CountAsync(cancellationToken);

        var uniqueRecipients = await ApplyNotificationUserScope(_db.IncidentNotifications)
            .Where(x => x.AlertTriggerId != null && x.RecipientUserId != null
                && x.AlertTrigger!.TriggeredAt >= fromDate && x.AlertTrigger.TriggeredAt < rangeEndExclusive
                && (!hasReportType || x.IncidentReport.ReportType == reportType))
            .Select(x => x.RecipientUserId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Single-day ranges (Today/Yesterday) bucket by hour instead of by day —
        // a day-level chart with exactly one bar isn't useful — same treatment as
        // DashboardService's Incident Trend for the same case.
        var alertsOverTime = new List<AlertTrendPointDto>();
        if (fromDate == toDate)
        {
            var hourlyRaw = await triggersInRange
                .GroupBy(x => x.TriggeredAt.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var hourlyMap = hourlyRaw.ToDictionary(x => x.Hour, x => x.Count);
            for (var h = 0; h < 24; h++)
                alertsOverTime.Add(new AlertTrendPointDto { Date = fromDate.AddHours(h), Count = hourlyMap.TryGetValue(h, out var c) ? c : 0 });
        }
        else
        {
            var trendRaw = await triggersInRange
                .GroupBy(x => x.TriggeredAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var trendMap = trendRaw.ToDictionary(x => x.Date, x => x.Count);
            for (var d = fromDate; d <= toDate; d = d.AddDays(1))
                alertsOverTime.Add(new AlertTrendPointDto { Date = d, Count = trendMap.TryGetValue(d, out var c) ? c : 0 });
        }

        // KPI card sparklines — critical-alert and notification timestamps bucketed
        // the same way as AlertsOverTime; "unique recipients" buckets distinct
        // (bucket, recipient) pairs so a person notified twice in one bucket counts once.
        var criticalTimestamps = await triggersInRange
            .Where(x => x.Urgency != null && CriticalUrgencyCodes.Contains(x.Urgency.Code))
            .Select(x => x.TriggeredAt)
            .ToListAsync(cancellationToken);
        var criticalAlertsTrend = BucketCounts(criticalTimestamps, fromDate, toDate);

        var sentNotificationTimestamps = await ApplyNotificationUserScope(_db.IncidentNotifications)
            .Where(x => x.AlertTriggerId != null && x.Status == "SENT"
                && x.AlertTrigger!.TriggeredAt >= fromDate && x.AlertTrigger.TriggeredAt < rangeEndExclusive
                && (!hasReportType || x.IncidentReport.ReportType == reportType))
            .Select(x => x.SentAt ?? x.AlertTrigger!.TriggeredAt)
            .ToListAsync(cancellationToken);
        var notificationsSentTrend = BucketCounts(sentNotificationTimestamps, fromDate, toDate);

        var recipientBucketPairs = await ApplyNotificationUserScope(_db.IncidentNotifications)
            .Where(x => x.AlertTriggerId != null && x.RecipientUserId != null
                && x.AlertTrigger!.TriggeredAt >= fromDate && x.AlertTrigger.TriggeredAt < rangeEndExclusive
                && (!hasReportType || x.IncidentReport.ReportType == reportType))
            .Select(x => new { x.AlertTrigger!.TriggeredAt, x.RecipientUserId })
            .Distinct()
            .ToListAsync(cancellationToken);
        var uniqueRecipientsTrend = BucketCounts(recipientBucketPairs.Select(x => x.TriggeredAt).ToList(), fromDate, toDate);

        var alertsByStatus = await triggersInRange
            .GroupBy(x => x.Status)
            .Select(g => new AlertStatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var alertsByReportType = await triggersInRange
            .Where(x => x.IncidentReport.ReportType != "Near Miss")
            .GroupBy(x => x.IncidentReport.ReportType)
            .Select(g => new AlertReportTypeCountDto { ReportType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var alertsByRule = await triggersInRange
            .GroupBy(x => new { x.AlertRuleId, x.AlertRule.RuleId, x.AlertRule.Name })
            .Select(g => new AlertRuleCountDto { AlertRuleId = g.Key.AlertRuleId, RuleId = g.Key.RuleId, RuleName = g.Key.Name, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var notificationsByChannel = await ApplyNotificationUserScope(_db.IncidentNotifications)
            .Where(x => x.AlertTriggerId != null && x.NotificationMethod != null
                && x.AlertTrigger!.TriggeredAt >= fromDate && x.AlertTrigger.TriggeredAt < rangeEndExclusive
                && (!hasReportType || x.IncidentReport.ReportType == reportType))
            .GroupBy(x => new { x.NotificationMethod!.Code, x.NotificationMethod.Name })
            .Select(g => new NotificationChannelCountDto { MethodCode = g.Key.Code, MethodName = g.Key.Name, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var alertsByUrgency = await triggersInRange
            .Where(x => x.Urgency != null)
            .GroupBy(x => new { x.UrgencyId, x.Urgency!.Code, x.Urgency.Name })
            .Select(g => new AlertUrgencyCountDto { UrgencyId = g.Key.UrgencyId!.Value, Code = g.Key.Code, Name = g.Key.Name, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var topRecipients = await ApplyNotificationUserScope(_db.IncidentNotifications)
            .Where(x => x.AlertTriggerId != null && x.RecipientUserId != null
                && x.AlertTrigger!.TriggeredAt >= fromDate && x.AlertTrigger.TriggeredAt < rangeEndExclusive
                && (!hasReportType || x.IncidentReport.ReportType == reportType))
            .GroupBy(x => new { x.RecipientUserId, x.RecipientUser!.Name, x.RecipientUser.Email })
            .Select(g => new AlertTopRecipientDto { UserId = g.Key.RecipientUserId!.Value, Name = g.Key.Name, Email = g.Key.Email, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new AlertDashboardOverviewDto
        {
            TotalRules = totalRules,
            ActiveRules = activeRules,
            InactiveRules = totalRules - activeRules,
            ImmediateEscalatedRules = immediateEscalatedRules,
            MedicationErrorRules = medicationErrorRules,
            AdrRules = adrRules,
            CriticalAlerts = criticalAlerts,
            AlertsTriggered = alertsTriggered,
            NotificationsSent = notificationsSent,
            UniqueRecipients = uniqueRecipients,
            AlertsOverTime = alertsOverTime,
            CriticalAlertsTrend = criticalAlertsTrend,
            NotificationsSentTrend = notificationsSentTrend,
            UniqueRecipientsTrend = uniqueRecipientsTrend,
            AlertsByStatus = alertsByStatus,
            AlertsByReportType = alertsByReportType,
            AlertsByRule = alertsByRule,
            NotificationsByChannel = notificationsByChannel,
            AlertsByUrgency = alertsByUrgency,
            TopRecipients = topRecipients,
            FieldUsage = fieldUsage
        };
    }

    private IQueryable<AlertTriggerHistory> BuildFilteredQuery(AlertTriggerFilterRequest request)
    {
        var query = ApplyUserScope(_db.AlertTriggerHistories.AsNoTracking().AsQueryable());

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
        var trigger = await ApplyUserScope(_db.AlertTriggerHistories.AsNoTracking())
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
            ReportTypes = ["Medication Error", "ADR"],
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
        var trigger = await ApplyUserScope(_db.AlertTriggerHistories).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (trigger == null) return null;

        trigger.Status = "ACKNOWLEDGED";
        trigger.AcknowledgedByUserId = userId;
        trigger.AcknowledgedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new AlertTriggerStatusResponseDto { Id = trigger.Id, Status = trigger.Status, AcknowledgedAt = trigger.AcknowledgedAt, ResolvedAt = trigger.ResolvedAt };
    }

    public async Task<AlertTriggerStatusResponseDto?> ResolveAsync(long id, int userId, CancellationToken cancellationToken)
    {
        var trigger = await ApplyUserScope(_db.AlertTriggerHistories).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
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
