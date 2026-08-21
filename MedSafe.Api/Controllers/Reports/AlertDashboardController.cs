using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedSafeAPI.Services;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Controllers;

// Any authenticated user — same trust model as DashboardController (the main
// Dashboard): access is gated by the frontend's view_alert_triggers_dashboard
// permission tag (Roles & Permissions), not a hardcoded role name, so a custom
// role granted that tag actually gets data instead of silent all-zero results.
[ApiController]
[Route("api/alerts/dashboard")]
[Authorize]
public class AlertDashboardController : ControllerBase
{
    private readonly IAlertDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUser;

    public AlertDashboardController(IAlertDashboardService dashboardService, ICurrentUserService currentUser)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? reportType, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetOverviewAsync(from, to, reportType, cancellationToken);
        return Ok(result);
    }

    [HttpGet("triggers")]
    public async Task<IActionResult> GetTriggers(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? urgencyId,
        [FromQuery] int? ruleId,
        [FromQuery] int? recipientUserId,
        [FromQuery] int? notificationMethodId,
        [FromQuery] string? reportType,
        [FromQuery] int? conditionFieldId,
        [FromQuery] string? triggerSource,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetTriggersAsync(new AlertTriggerFilterRequest
        {
            Search = search, Status = status, UrgencyId = urgencyId, RuleId = ruleId,
            RecipientUserId = recipientUserId, NotificationMethodId = notificationMethodId,
            ReportType = reportType, ConditionFieldId = conditionFieldId, TriggerSource = triggerSource,
            From = from, To = to, Page = page, PageSize = pageSize
        }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("triggers/{id:long}")]
    public async Task<IActionResult> GetTrigger(long id, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetTriggerDetailAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetFilterOptionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("triggers/{id:long}/acknowledge")]
    public async Task<IActionResult> Acknowledge(long id, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.AcknowledgeAsync(id, _currentUser.UserId, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("triggers/{id:long}/resolve")]
    public async Task<IActionResult> Resolve(long id, [FromBody] ResolveAlertTriggerRequest? request, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.ResolveAsync(id, _currentUser.UserId, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // Don't include patient PHI here — recipient/email/rule/report-number/status
    // only, matching the columns actually needed to audit "who was told what".
    [HttpGet("triggers/export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? urgencyId,
        [FromQuery] int? ruleId,
        [FromQuery] int? recipientUserId,
        [FromQuery] int? notificationMethodId,
        [FromQuery] string? reportType,
        [FromQuery] int? conditionFieldId,
        [FromQuery] string? triggerSource,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dashboardService.GetExportRowsAsync(new AlertTriggerFilterRequest
        {
            Search = search, Status = status, UrgencyId = urgencyId, RuleId = ruleId,
            RecipientUserId = recipientUserId, NotificationMethodId = notificationMethodId,
            ReportType = reportType, ConditionFieldId = conditionFieldId, TriggerSource = triggerSource,
            From = from, To = to, Page = 1, PageSize = int.MaxValue
        }, cancellationToken);

        var csv = BuildCsv(rows);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"alert-triggers-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private static string BuildCsv(List<AlertTriggerExportRowDto> rows)
    {
        static string Escape(string? value)
        {
            value ??= string.Empty;
            return value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Alert ID,Rule ID,Rule Name,Report ID,Report Type,Condition,Urgency,Triggered At,Recipient,Email,Method,Delivery Status,Sent At,Alert Status");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",",
                Escape(r.AlertId), Escape(r.RuleId), Escape(r.RuleName), Escape(r.ReportNumber), Escape(r.ReportType),
                Escape(r.ConditionBase), Escape(r.Urgency), Escape(r.TriggeredAt.ToString("yyyy-MM-dd HH:mm:ss")),
                Escape(r.Recipient), Escape(r.Email), Escape(r.Method), Escape(r.DeliveryStatus),
                Escape(r.SentAt?.ToString("yyyy-MM-dd HH:mm:ss")), Escape(r.AlertStatus)));
        }
        return sb.ToString();
    }
}
