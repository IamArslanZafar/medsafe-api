using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.Services;

namespace MedSafeAPI.BackgroundServices;

// Polls for automatic IncidentNotification rows that are EMAIL + PENDING
// (created by AlertRuleEvaluationService / AlertMonitorService), generates the
// report PDF, and sends the email. Runs independently of report submission —
// SMTP being down must never turn into a failed incident-report save.
public class EmailNotificationWorker : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 3;
    private static readonly string[] UrgentCodes = ["IMMEDIATE", "ESCALATED"];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailNotificationWorker> _logger;

    public EmailNotificationWorker(IServiceScopeFactory scopeFactory, ILogger<EmailNotificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try
        {
            do
            {
                await ProcessPendingEmailsSafeAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Application stopped normally.
        }
    }

    private async Task ProcessPendingEmailsSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var pdfService = scope.ServiceProvider.GetRequiredService<IIncidentReportPdfService>();
            await ProcessPendingEmailsAsync(db, emailService, pdfService, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email notification worker run failed.");
        }
    }

    private async Task ProcessPendingEmailsAsync(AppDbContext db, IEmailService emailService, IIncidentReportPdfService pdfService, CancellationToken cancellationToken)
    {
        var notifications = await db.IncidentNotifications
            .Include(x => x.RecipientUser)
            .Include(x => x.IncidentReport)
            .Include(x => x.Urgency)
            .Include(x => x.NotificationMethod)
            .Where(x => x.IsAutomatic
                && x.Status == "PENDING"
                && x.RecipientUserId != null
                && x.NotificationMethod != null && x.NotificationMethod.Code == "EMAIL")
            .OrderBy(x => x.CreatedDate)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0)
            return;

        foreach (var notification in notifications)
        {
            notification.EmailAttemptCount++;
            notification.LastEmailAttemptAt = DateTime.UtcNow;

            try
            {
                var recipientEmail = notification.RecipientUser?.Email;
                if (string.IsNullOrWhiteSpace(recipientEmail))
                    throw new InvalidOperationException("Recipient has no email address on file.");

                var report = notification.IncidentReport;
                var pdfBytes = await pdfService.GenerateAsync(notification.IncidentReportId, cancellationToken);
                var pdfFileName = $"QTCMRS_Report_{report.IncidentReportNumber}.pdf";

                var isUrgent = notification.Urgency != null && UrgentCodes.Contains(notification.Urgency.Code);
                var subjectPrefix = isUrgent ? "[URGENT][QTCMRS]" : "[QTCMRS]";
                var subject = $"{subjectPrefix} Medication Safety Report Requires Review — {report.IncidentReportNumber}";

                var request = new SendEmailRequest
                {
                    ToEmail = recipientEmail,
                    ToName = notification.PersonName,
                    Subject = subject,
                    HtmlBody = BuildEmailBody(notification.PersonName, report.IncidentReportNumber, report.ReportType,
                        notification.Urgency?.Name ?? "Normal", report.ReportStatus, report.SubmittedAt),
                    Attachments =
                    [
                        new EmailAttachment { FileName = pdfFileName, ContentType = "application/pdf", Content = pdfBytes }
                    ]
                };

                await emailService.SendAsync(request, cancellationToken);

                notification.Status = "SENT";
                notification.SentAt = DateTime.UtcNow;
                notification.Notes = null;
            }
            catch (Exception ex)
            {
                notification.Status = notification.EmailAttemptCount >= MaxAttempts ? "FAILED" : "PENDING";
                notification.Notes = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                _logger.LogWarning(ex, "Email delivery failed for notification {NotificationId} (attempt {Attempt}).", notification.Id, notification.EmailAttemptCount);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string BuildEmailBody(string recipientName, string reportNumber, string reportType, string urgency,
        string reportStatus, DateTime submittedAt)
    {
        return $"""
            <div style="font-family:Arial,sans-serif;font-size:14px;color:#1e293b;line-height:1.6;">
              <p>Dear {recipientName},</p>
              <p>A medication safety report has been assigned to you and requires your review.</p>
              <p style="font-weight:bold;margin-bottom:4px;">Report Details</p>
              <ul style="margin-top:0;">
                <li><strong>Report ID:</strong> {reportNumber}</li>
                <li><strong>Report Type:</strong> {reportType}</li>
                <li><strong>Priority:</strong> {urgency}</li>
                <li><strong>Status:</strong> {reportStatus}</li>
                <li><strong>Submitted On:</strong> {submittedAt:dd MMM yyyy, HH:mm} UTC</li>
              </ul>
              <p>A PDF copy of the report is attached for your reference. Please sign in to QTCMRS to review the report and complete the required action.</p>
              <h4>Confidentiality Notice</h4>
              <p>This email and its attachment contain confidential clinical information and are intended only for the authorized recipient. Please do not forward, share, or distribute this information outside the approved clinical workflow.</p>
              <p>If you have received this email in error, please notify the system administrator and delete the email and attachment.</p>
              <p>Regards,<br/><strong>QTCMRS</strong><br/>Qatar Trauma Center Medication Reporting System</p>
              <p style="font-size:11px;color:#64748b;">This is an automated notification. Please do not reply to this email.</p>
            </div>
            """;
    }
}
