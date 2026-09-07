using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

// Backs the "CPD & Education" module's New CPD Activity form + CPD Dashboard.
[ApiController]
[Route("api/cpd-activities")]
[Authorize]
public class CpdActivitiesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICpdActivityAttachmentService _attachments;
    private readonly ICurrentUserService _currentUser;

    public CpdActivitiesController(AppDbContext db, ICpdActivityAttachmentService attachments, ICurrentUserService currentUser)
    {
        _db = db;
        _attachments = attachments;
        _currentUser = currentUser;
    }

    // Uploaded standalone from the form's file picker, before the activity
    // itself is created — the returned id is sent back as AttachmentId on
    // the POST above.
    [HttpPost("attachments")]
    public async Task<IActionResult> UploadAttachment(IFormFile file, CancellationToken cancellationToken)
    {
        var dto = await _attachments.UploadAsync(file, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("attachments/{id:int}/download")]
    public async Task<IActionResult> DownloadAttachment(int id, CancellationToken cancellationToken)
    {
        var (stream, contentType, fileName) = await _attachments.DownloadAsync(id, cancellationToken);
        return File(stream, contentType, fileName);
    }

    [HttpPost]
    public async Task<IActionResult> Submit(CpdActivityCreateDto dto)
    {
        // Category 2 activities cap eligible credits at 10 hours — matches
        // CPDActivityForm.jsx's client-side `eligibleCredits` calculation,
        // recomputed here so the stored value can't be spoofed by the client.
        var credits = dto.Category == "cat2" ? Math.Min(dto.Hours, 10m) : dto.Hours;

        var activity = new CpdActivity
        {
            ReferenceCode = $"CPD-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(1000, 9999)}",
            Category = dto.Category,
            Subcategory = dto.Subcategory,
            Activity = dto.Activity,
            ActivityType = dto.ActivityType,
            Location = dto.Location,
            Format = dto.Format,
            Title = dto.Title,
            ActivityDate = dto.ActivityDate,
            Hours = dto.Hours,
            Credits = credits,
            Reflection = dto.Reflection,
            Comments = dto.Comments,
            AttachmentId = dto.AttachmentId,
            Status = dto.SaveAsDraft ? "draft" : "pending",
            SubmittedByUserId = _currentUser.UserId,
            SubmittedByRole = _currentUser.Role,
        };

        _db.CpdActivities.Add(activity);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(activity));
    }

    // Matches the "view_dashboard" permission already gating the CPD Dashboard route.
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.CpdActivities.OrderByDescending(a => a.CreatedAt).ToListAsync();
        return Ok(list.Select(MapToDto));
    }

    private static CpdActivityDto MapToDto(CpdActivity a) => new()
    {
        Id = a.Id,
        ReferenceCode = a.ReferenceCode,
        Category = a.Category,
        Subcategory = a.Subcategory,
        Activity = a.Activity,
        ActivityType = a.ActivityType,
        Location = a.Location,
        Format = a.Format,
        Title = a.Title,
        ActivityDate = a.ActivityDate,
        Hours = a.Hours,
        Credits = a.Credits,
        Reflection = a.Reflection,
        Comments = a.Comments,
        AttachmentId = a.AttachmentId,
        Status = a.Status,
        ReviewFeedback = a.ReviewFeedback,
        SubmittedByUserId = a.SubmittedByUserId,
        SubmittedByRole = a.SubmittedByRole,
        CreatedAt = a.CreatedAt,
    };
}
