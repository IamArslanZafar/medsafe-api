using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;
using MedSafe.Models;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize(Roles = "Physician,Admin")]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlertRuleService _alertRuleService;

    public AlertsController(AppDbContext db, ICurrentUserService currentUser, IAlertRuleService alertRuleService)
    {
        _db = db;
        _currentUser = currentUser;
        _alertRuleService = alertRuleService;
    }

    // ?search=&isEnabled=&urgencyId= — all optional, combine with AND.
    [HttpGet]
    public async Task<IActionResult> GetAlerts([FromQuery] string? search, [FromQuery] bool? isEnabled, [FromQuery] int? urgencyId, CancellationToken cancellationToken)
    {
        var alerts = await _alertRuleService.GetAllAsync(search, isEnabled, urgencyId, cancellationToken);
        return Ok(alerts);
    }

    // KPI cards for the Alert Rules screen — must be routed before {id:int} would
    // otherwise be ambiguous, but the :int constraint below already prevents that.
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _alertRuleService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var rule = await _alertRuleService.GetByIdAsync(id, cancellationToken);
        if (rule == null) return NotFound();
        return Ok(rule);
    }

    // Which fields a rule can be built on, and which operators are valid for each.
    [HttpGet("builder-fields")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetBuilderFields(CancellationToken cancellationToken)
    {
        var fields = await _db.AlertConditionFields
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new AlertBuilderFieldDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsMultiValue = x.IsMultiValue,
                Operators = x.FieldOperators
                    .Where(fo => fo.Operator.IsActive)
                    .OrderBy(fo => fo.Operator.DisplayOrder)
                    .Select(fo => new AlertBuilderOperatorDto
                    {
                        Id = fo.Operator.Id,
                        Code = fo.Operator.Code,
                        Name = fo.Operator.Name
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(fields);
    }

    // The value dropdown options for a given builder field (lookup-table backed
    // fields hit the DB; REPORT_TYPE/HARM_LEVEL/SUSPECTED_CAUSALITY are fixed
    // sets since they don't have their own lookup tables).
    [HttpGet("builder-values/{fieldCode}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetBuilderValues(string fieldCode, CancellationToken cancellationToken)
    {
        fieldCode = fieldCode.Trim().ToUpperInvariant();

        List<AlertBuilderValueDto> values;
        switch (fieldCode)
        {
            case "REPORT_TYPE":
                values =
                [
                    new() { Value = "Medication Error", Label = "Medication Error" },
                    new() { Value = "ADR", Label = "ADR Reaction" }
                ];
                break;
            case "HARM_LEVEL":
                values =
                [
                    new() { Value = "A", Label = "Category A" },
                    new() { Value = "B", Label = "Category B" },
                    new() { Value = "C", Label = "Category C" },
                    new() { Value = "D", Label = "Category D" },
                    new() { Value = "E", Label = "Category E" },
                    new() { Value = "F", Label = "Category F" },
                    new() { Value = "G", Label = "Category G" },
                    new() { Value = "H", Label = "Category H" },
                    new() { Value = "I", Label = "Category I" }
                ];
                break;
            case "ERROR_CATEGORY":
                values = await _db.ErrorCategories
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new AlertBuilderValueDto { Value = x.Id.ToString(), Label = x.Name })
                    .ToListAsync(cancellationToken);
                break;
            case "STAGE_OF_PROCESS":
                values = await _db.StageOfProcesses
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new AlertBuilderValueDto { Value = x.Id.ToString(), Label = x.Name })
                    .ToListAsync(cancellationToken);
                break;
            case "PATIENT_OUTCOME":
                values = await _db.PatientOutcomes
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new AlertBuilderValueDto { Value = x.Id.ToString(), Label = x.Name })
                    .ToListAsync(cancellationToken);
                break;
            case "SERIOUSNESS_CRITERIA":
                values = await _db.SeriousnessCriteria
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new AlertBuilderValueDto { Value = x.Id.ToString(), Label = x.Name })
                    .ToListAsync(cancellationToken);
                break;
            case "CONTRIBUTING_FACTOR":
                values = await _db.ContributingFactors
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new AlertBuilderValueDto { Value = x.Id.ToString(), Label = x.Name })
                    .ToListAsync(cancellationToken);
                break;
            case "SUSPECTED_CAUSALITY":
                values =
                [
                    new() { Value = "Certain", Label = "Certain" },
                    new() { Value = "Probable / likely", Label = "Probable / likely" },
                    new() { Value = "Possible", Label = "Possible" },
                    new() { Value = "Unlikely", Label = "Unlikely" },
                    new() { Value = "Conditional / unclassified", Label = "Conditional / unclassified" },
                    new() { Value = "Unassessable", Label = "Unassessable" }
                ];
                break;
            default:
                return BadRequest(new { message = "Unsupported alert condition field." });
        }

        return Ok(values);
    }

    [HttpGet("match-modes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMatchModes(CancellationToken cancellationToken)
    {
        var result = await _db.AlertRuleMatchModes
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new AlertRuleMatchModeDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAlert([FromBody] CreateAlertRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await _alertRuleService.CreateAsync(request, cancellationToken);
        return Created($"/api/alerts/{result.Id}", result);
    }

    // Frontend resends the complete current configuration; conditions and
    // recipients are replaced wholesale inside one transaction.
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAlert(int id, [FromBody] UpdateAlertRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _alertRuleService.UpdateAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id:int}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Toggle(int id)
    {
        var rule = await _db.AlertRules.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (rule == null) return NotFound();

        rule.Enabled = !rule.Enabled;
        await _db.SaveChangesAsync();
        return Ok(new ToggleAlertRuleResponse { Id = rule.Id, IsEnabled = rule.Enabled });
    }

    // Soft delete — the rule is hidden from every list/detail/toggle/test endpoint
    // (all filter on !IsDeleted) but the row, its conditions and its recipients
    // stay in the DB for audit purposes rather than being hard-removed.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await _db.AlertRules.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (rule == null) return NotFound();

        rule.IsDeleted = true;
        rule.DeletedAt = DateTime.UtcNow;
        rule.DeletedByUserId = _currentUser.UserId;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Alert rule deleted" });
    }

    // Simulated — there's no email/SMS delivery integration wired up yet, so this
    // records that a test was run (LastTriggered + audit log) without actually
    // sending anything, so Admins can confirm a rule's config looks right.
    [HttpPost("{id:int}/test")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Test(int id, CancellationToken cancellationToken)
    {
        var rule = await _db.AlertRules.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (rule == null) return NotFound();

        var recipients = await _db.AlertRuleRecipients
            .Include(r => r.RecipientUser)
            .Where(r => r.AlertRuleId == id)
            .Select(r => new TestAlertRuleRecipientDto { RecipientUserId = r.RecipientUserId, RecipientName = r.RecipientUser.Name })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        rule.LastTriggered = now;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            UserName = _currentUser.Name,
            Action = "ALERT_RULE_TESTED",
            Details = $"Test notification simulated for rule {rule.RuleId} ({rule.Name}).",
            Timestamp = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new TestAlertRuleResponse
        {
            RuleId = rule.RuleId,
            Name = rule.Name,
            RecipientCount = recipients.Count,
            Recipients = recipients,
            Message = $"Test notification simulated for \"{rule.Name}\" — no real delivery channel is configured yet, so nothing was actually sent.",
            TestedAt = now
        });
    }
}
