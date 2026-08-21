using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

// SMTP credentials — kept Admin-only on the backend regardless of the
// "Manage Email Settings" permission tag (which only controls whether the
// frontend shows the page), same pattern as UsersController and the other
// credential-holding endpoints in this app.
[ApiController]
[Route("api/email-settings")]
[Authorize(Roles = "Admin")]
public class EmailSettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;

    public EmailSettingsController(AppDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var settings = await _db.EmailSettings.AsNoTracking().FirstOrDefaultAsync();
        return Ok(settings == null ? new EmailSettingsDto { Port = 587, UseSsl = true } : MapToDto(settings));
    }

    [HttpPut]
    public async Task<IActionResult> Update(EmailSettingsUpdateDto dto)
    {
        var settings = await _db.EmailSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new EmailSettings();
            _db.EmailSettings.Add(settings);
        }

        settings.Host = dto.Host.Trim();
        settings.Port = dto.Port;
        settings.Username = dto.Username.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Password)) settings.Password = dto.Password;
        settings.FromAddress = dto.FromAddress.Trim();
        settings.FromName = dto.FromName.Trim();
        settings.UseSsl = dto.UseSsl;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedByUserId = CurrentUserId();

        await _db.SaveChangesAsync();
        return Ok(MapToDto(settings));
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTest([FromQuery] string to, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(to))
            return BadRequest(new { message = "Query param 'to' (recipient email) is required." });

        try
        {
            await _emailService.SendAsync(new SendEmailRequest
            {
                ToEmail = to,
                ToName = to,
                Subject = "[QTCMRS] Test Email",
                HtmlBody = "<p>This is a test email confirming your Email Settings are working.</p>"
            }, cancellationToken);
            return Ok(new { message = $"Test email sent to {to}." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Send failed.", error = ex.Message });
        }
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static EmailSettingsDto MapToDto(EmailSettings s) => new()
    {
        Host = s.Host,
        Port = s.Port,
        Username = s.Username,
        PasswordSet = !string.IsNullOrEmpty(s.Password),
        FromAddress = s.FromAddress,
        FromName = s.FromName,
        UseSsl = s.UseSsl,
        UpdatedAt = s.UpdatedAt
    };
}
