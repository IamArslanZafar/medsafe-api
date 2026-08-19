using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;

namespace MedSafeAPI.Services;

public class AlertTriggerService : IAlertTriggerService
{
    private readonly AppDbContext _db;

    public AlertTriggerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<long?> CreateAsync(CreateAlertTriggerCommand command, CancellationToken cancellationToken)
    {
        var alreadyExists = await _db.AlertTriggerHistories
            .AnyAsync(x => x.DedupeKey == command.DedupeKey, cancellationToken);
        if (alreadyExists)
            return null;

        var now = DateTime.UtcNow;
        var trigger = new AlertTriggerHistory
        {
            AlertTriggerNumber = GenerateAlertNumber(),
            AlertRuleId = command.AlertRuleId,
            IncidentReportId = command.IncidentReportId,
            UrgencyId = command.UrgencyId,
            TriggerSource = command.TriggerSource,
            ConditionSummary = command.ConditionSummary,
            MatchedConditionSnapshot = command.MatchedConditionSnapshot,
            Status = "OPEN",
            DedupeKey = command.DedupeKey,
            TriggeredAt = now,
            CreatedAt = now
        };
        _db.AlertTriggerHistories.Add(trigger);
        await _db.SaveChangesAsync(cancellationToken);

        var emailMethodId = await _db.NotificationMethods
            .Where(x => x.Code == "EMAIL" && x.IsActive)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var rule = await _db.AlertRules.FindAsync([command.AlertRuleId], cancellationToken);

        // dbo.IncidentNotifications still enforces UX_IncidentNotifications_Report_Rule_User
        // (IncidentReportId, AlertRuleId, RecipientUserId) — a real DB-level invariant
        // predating AlertTriggerHistory. DedupeKey only stops a duplicate *trigger*;
        // it doesn't know about notifications created before this table existed, so
        // recipients already notified for this exact report+rule must still be skipped
        // here or the insert below throws a unique-constraint violation.
        var alreadyNotifiedUserIds = await _db.IncidentNotifications
            .Where(x => x.IncidentReportId == command.IncidentReportId && x.AlertRuleId == command.AlertRuleId && x.RecipientUserId != null)
            .Select(x => x.RecipientUserId!.Value)
            .ToListAsync(cancellationToken);
        var alreadyNotifiedSet = alreadyNotifiedUserIds.ToHashSet();

        foreach (var recipient in command.Recipients.DistinctBy(x => x.UserId))
        {
            if (alreadyNotifiedSet.Contains(recipient.UserId))
                continue;

            var user = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == recipient.UserId, cancellationToken);

            _db.IncidentNotifications.Add(new IncidentNotification
            {
                AlertTriggerId = trigger.Id,
                IncidentReportId = command.IncidentReportId,
                AlertRuleId = command.AlertRuleId,
                RecipientUserId = recipient.UserId,
                NotificationTypeId = recipient.RecipientTypeId,
                UrgencyId = command.UrgencyId,
                NotificationMethodId = emailMethodId,
                Status = "PENDING",
                PersonName = user.Name,
                Title = command.Title,
                Message = command.Message,
                IsAutomatic = true,
                IsRead = false,
                NotifiedAt = now,
                CreatedBy = command.CreatedByUserId,
                CreatedDate = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (rule != null)
        {
            rule.LastTriggered = now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return trigger.Id;
    }

    private static string GenerateAlertNumber()
    {
        var part = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"ALT-{DateTime.UtcNow:yyyyMMdd}-{part}";
    }
}
