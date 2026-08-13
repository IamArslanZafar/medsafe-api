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

    public AlertsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAlerts()
    {
        var alerts = await _db.AlertRules.OrderBy(a => a.RuleId).ToListAsync();
        return Ok(alerts.Select(MapToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAlert(AlertRuleCreateDto dto)
    {
        var count = await _db.AlertRules.CountAsync();
        var ruleId = $"RULE-{(count + 1):D3}";

        var rule = new AlertRule
        {
            RuleId = ruleId,
            Name = dto.Name,
            TriggerCondition = dto.TriggerCondition,
            TargetRoles = dto.TargetRoles,
            Urgency = dto.Urgency,
            Description = dto.Description,
            DeliveryConfig = dto.DeliveryConfig
        };

        _db.AlertRules.Add(rule);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(rule));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAlert(int id, AlertRuleUpdateDto dto)
    {
        var rule = await _db.AlertRules.FindAsync(id);
        if (rule == null) return NotFound();

        rule.Name = dto.Name;
        rule.TriggerCondition = dto.TriggerCondition;
        rule.TargetRoles = dto.TargetRoles;
        rule.Urgency = dto.Urgency;
        rule.Description = dto.Description;
        rule.DeliveryConfig = dto.DeliveryConfig;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(rule));
    }

    [HttpPut("{id}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Toggle(int id)
    {
        var rule = await _db.AlertRules.FindAsync(id);
        if (rule == null) return NotFound();

        rule.Enabled = !rule.Enabled;
        await _db.SaveChangesAsync();
        return Ok(new { enabled = rule.Enabled });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await _db.AlertRules.FindAsync(id);
        if (rule == null) return NotFound();

        _db.AlertRules.Remove(rule);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Alert rule deleted" });
    }

    // Simulated — there's no email/SMS delivery integration wired up yet, so this
    // records that a test was run (LastTriggered + audit log) without actually
    // sending anything, so Admins can confirm a rule's config looks right.
    [HttpPost("{id}/test")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Test(int id)
    {
        var rule = await _db.AlertRules.FindAsync(id);
        if (rule == null) return NotFound();

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

        await _db.SaveChangesAsync();

        return Ok(new AlertRuleTestResponseDto
        {
            RuleId = rule.RuleId,
            Name = rule.Name,
            TargetRoles = rule.TargetRoles,
            Urgency = rule.Urgency,
            Message = $"Test notification simulated for \"{rule.Name}\" — no real delivery channel is configured yet, so nothing was actually sent.",
            TestedAt = now
        });
    }

    private static AlertRuleResponseDto MapToDto(AlertRule a) => new()
    {
        Id = a.Id,
        RuleId = a.RuleId,
        Name = a.Name,
        TriggerCondition = a.TriggerCondition,
        TargetRoles = a.TargetRoles,
        Urgency = a.Urgency,
        Enabled = a.Enabled,
        Description = a.Description,
        LastTriggered = a.LastTriggered,
        DeliveryConfig = a.DeliveryConfig
    };
}
