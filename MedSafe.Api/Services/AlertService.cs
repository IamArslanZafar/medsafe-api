using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;

namespace MedSafeAPI.Services;

// Evaluates severity after an incident report commits and creates IncidentNotifications
// rows for the roles that need to know. Actual delivery (email/SMS) is intentionally not
// done here — a background worker should pick up "Pending" rows and send them, so a slow
// SMTP/SMS provider never makes the submitting nurse wait.
public class AlertService : IAlertService
{
    private static readonly string[] PhysicianLevels = ["E", "F", "G"];
    private static readonly string[] ImmediateEscalationLevels = ["H", "I"];
    private const string SystemMethodCode = "SYSTEM";
    private const string PendingStatusCode = "PENDING";

    private readonly AppDbContext _db;
    private readonly ILogger<AlertService> _logger;

    public AlertService(AppDbContext db, ILogger<AlertService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EvaluateIncidentAsync(int incidentReportId, CancellationToken cancellationToken)
    {
        var report = await _db.IncidentReports
            .Where(r => r.Id == incidentReportId)
            .Select(r => new { r.IncidentReportNumber, r.HarmLevelCode, r.SubmittedByUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (report == null) return;

        var recipientCodes = report.HarmLevelCode switch
        {
            var code when ImmediateEscalationLevels.Contains(code) => new[] { "SAFETY_OFFICER", "ADMINISTRATOR" },
            var code when PhysicianLevels.Contains(code) => new[] { "PHYSICIAN" },
            _ => []
        };

        if (recipientCodes.Length == 0) return;

        var methodId = await _db.NotificationMethods
            .Where(m => m.Code == SystemMethodCode && m.IsActive)
            .Select(m => (int?)m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var recipientTypeIds = await _db.NotificationRecipientTypes
            .Where(t => recipientCodes.Contains(t.Code) && t.IsActive)
            .Select(t => new { t.Id, t.Code })
            .ToListAsync(cancellationToken);

        if (recipientTypeIds.Count == 0)
        {
            _logger.LogWarning(
                "No active NotificationRecipientType rows found for {Codes} — skipping notification creation for {IncidentReportNumber}.",
                string.Join(", ", recipientCodes), report.IncidentReportNumber);
            return;
        }

        var isImmediate = ImmediateEscalationLevels.Contains(report.HarmLevelCode);
        foreach (var recipientType in recipientTypeIds)
        {
            _db.IncidentNotifications.Add(new IncidentNotification
            {
                IncidentReportId = incidentReportId,
                NotificationTypeId = recipientType.Id,
                NotificationMethodId = methodId,
                IsAutomatic = true,
                Status = PendingStatusCode,
                Notes = isImmediate
                    ? $"Immediate escalation — Harm Level {report.HarmLevelCode} ({report.IncidentReportNumber})."
                    : $"Escalation — Harm Level {report.HarmLevelCode} ({report.IncidentReportNumber}).",
                CreatedBy = report.SubmittedByUserId,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created {Count} notification(s) for {IncidentReportNumber} (Harm Level {HarmLevelCode}).",
            recipientTypeIds.Count, report.IncidentReportNumber, report.HarmLevelCode);
    }
}
