using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

// Diagnostic-only endpoint to verify SMTP delivery without waiting on the
// EmailNotificationWorker's poll cycle or wiring up an Alert Rule recipient.
// Calls IEmailService directly and returns success/failure synchronously —
// remove once the SMTP setup is confirmed working end to end.
[ApiController]
[Route("api/test-email")]
[Authorize(Roles = "Admin")]
public class EmailTestController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly IIncidentReportPdfService _pdfService;

    public EmailTestController(IEmailService emailService, IIncidentReportPdfService pdfService)
    {
        _emailService = emailService;
        _pdfService = pdfService;
    }

    [HttpPost]
    public async Task<IActionResult> SendTestEmail(
        [FromQuery] string to,
        [FromQuery] int? incidentReportId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(to))
            return BadRequest(new { message = "Query param 'to' (recipient email) is required." });

        var attachments = new List<EmailAttachment>();
        if (incidentReportId.HasValue)
        {
            var pdfBytes = await _pdfService.GenerateAsync(incidentReportId.Value, cancellationToken);
            attachments.Add(new EmailAttachment
            {
                FileName = $"QTCMRS_Report_TEST_{incidentReportId.Value}.pdf",
                ContentType = "application/pdf",
                Content = pdfBytes
            });
        }

        var request = new SendEmailRequest
        {
            ToEmail = to,
            ToName = to,
            Subject = "[QTCMRS] Test Email",
            HtmlBody = "<p>This is a test email from QTCMRS to confirm SMTP delivery is working.</p>" +
                       (incidentReportId.HasValue ? "<p>A sample report PDF is attached.</p>" : ""),
            Attachments = attachments
        };

        try
        {
            await _emailService.SendAsync(request, cancellationToken);
            return Ok(new { message = $"Test email sent to {to}." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Send failed.", error = ex.Message });
        }
    }
}
