using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.Services;

namespace MedSafeAPI.BackgroundServices;

public class AlertMonitorService : BackgroundService
{
    private const string OverdueReviewRuleId = "AR-005";
    private const string ReminderTitle = "Assessment Reminder";
    private const string UnassignedTitle = "Unassigned Report Overdue";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertMonitorService> _logger;

    public AlertMonitorService(IServiceScopeFactory scopeFactory, ILogger<AlertMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunChecksSafeAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunChecksSafeAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Application stopped normally.
        }
    }

    private async Task RunChecksSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var alertTriggerService = scope.ServiceProvider.GetRequiredService<IAlertTriggerService>();
            await CheckOverdueAssessmentsAsync(db, alertTriggerService, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled alert monitoring failed.");
        }
    }

    // AR-005: Report awaiting review > 48 hours.
    // Assigned reviewer (IncidentReportReviews.ReviewerUserId) gets reminded;
    // if no reviewer is assigned, active Admins get the fallback notification.
    private async Task CheckOverdueAssessmentsAsync(AppDbContext db, IAlertTriggerService alertTriggerService, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-48);

        var rule = await db.AlertRules
            .FirstOrDefaultAsync(x => x.RuleId == OverdueReviewRuleId && x.Enabled && !x.IsDeleted, cancellationToken);
        if (rule == null)
            return;

        var overdueReports = await db.IncidentReports
            .AsNoTracking()
            .Where(x => x.SubmittedAt <= cutoff && (x.ReportStatus == "Pending" || x.ReportStatus == "UnderReview"))
            .Select(x => new { x.Id, x.IncidentReportNumber, x.SubmittedAt, x.ReportStatus })
            .ToListAsync(cancellationToken);

        if (overdueReports.Count == 0)
            return;

        var reportIds = overdueReports.Select(x => x.Id).ToList();

        var reviewRows = await db.IncidentReportReviews
            .AsNoTracking()
            .Where(x => reportIds.Contains(x.IncidentReportId))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.IncidentReportId, x.ReviewerUserId, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var reviewerByReport = reviewRows
            .GroupBy(x => x.IncidentReportId)
            .ToDictionary(group => group.Key, group => group.First().ReviewerUserId);

        var admins = await db.Users
            .AsNoTracking()
            .Where(x => x.Role == "Admin" && x.Status == "active")
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);

        var systemCreatedByUserId = admins.FirstOrDefault()?.Id;
        if (!systemCreatedByUserId.HasValue)
        {
            _logger.LogError("No active Admin exists — cannot create AR-005 overdue reminders.");
            return;
        }

        var recipientTypes = await db.NotificationRecipientTypes
            .AsNoTracking()
            .Where(x => x.IsActive && (x.Code == "ASSIGNED_REVIEWER" || x.Code == "ADMINISTRATOR"))
            .ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken);

        if (!recipientTypes.TryGetValue("ASSIGNED_REVIEWER", out var assignedReviewerTypeId) ||
            !recipientTypes.TryGetValue("ADMINISTRATOR", out var administratorTypeId))
        {
            _logger.LogError("Required notification recipient types are missing.");
            return;
        }

        var reminderUrgency = await db.NotificationUrgencies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == "REMINDER" && x.IsActive, cancellationToken);
        if (reminderUrgency == null)
            return;

        var reviewerIds = reviewerByReport.Values.Distinct().ToList();
        var reviewers = await db.Users
            .AsNoTracking()
            .Where(x => reviewerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var createdCount = 0;

        foreach (var report in overdueReports)
        {
            if (reviewerByReport.TryGetValue(report.Id, out var reviewerUserId))
            {
                if (!reviewers.ContainsKey(reviewerUserId))
                    continue;

                var triggerId = await alertTriggerService.CreateAsync(new CreateAlertTriggerCommand
                {
                    AlertRuleId = rule.Id,
                    IncidentReportId = report.Id,
                    UrgencyId = reminderUrgency.Id,
                    TriggerSource = "SCHEDULED_48H",
                    ConditionSummary = "Report awaiting review for more than 48 hours",
                    DedupeKey = $"REVIEW_48H:{rule.Id}:{report.Id}",
                    Title = ReminderTitle,
                    Message = $"Report {report.IncidentReportNumber} has been awaiting assessment for more than 48 hours.",
                    CreatedByUserId = systemCreatedByUserId.Value,
                    Recipients = [new AlertTriggerRecipientCommand { UserId = reviewerUserId, RecipientTypeId = assignedReviewerTypeId }]
                }, cancellationToken);

                if (triggerId.HasValue) createdCount++;
            }
            else
            {
                var triggerId = await alertTriggerService.CreateAsync(new CreateAlertTriggerCommand
                {
                    AlertRuleId = rule.Id,
                    IncidentReportId = report.Id,
                    UrgencyId = reminderUrgency.Id,
                    TriggerSource = "SCHEDULED_48H",
                    ConditionSummary = "Report awaiting review for more than 48 hours, no assigned reviewer",
                    DedupeKey = $"REVIEW_48H:{rule.Id}:{report.Id}",
                    Title = UnassignedTitle,
                    Message = $"Report {report.IncidentReportNumber} has been pending for more than 48 hours and has no assigned reviewer.",
                    CreatedByUserId = systemCreatedByUserId.Value,
                    Recipients = admins.Select(admin => new AlertTriggerRecipientCommand { UserId = admin.Id, RecipientTypeId = administratorTypeId }).ToList()
                }, cancellationToken);

                if (triggerId.HasValue) createdCount++;
            }
        }

        if (createdCount > 0)
            _logger.LogInformation("AR-005 created {Count} overdue assessment alert triggers.", createdCount);
    }
}
