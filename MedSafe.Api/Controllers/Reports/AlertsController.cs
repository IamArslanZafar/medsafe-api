using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafeAPI.DTOs;
using MedSafe.Models;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize(Roles = "Physician,Admin")]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AlertsController(AppDbContext db) => _db = db;

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
