using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/feedback")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FeedbackController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Feedbacks.OrderByDescending(f => f.CreatedAt).ToListAsync();
        return Ok(list.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var feedback = await _db.Feedbacks.FindAsync(id);
        if (feedback == null) return NotFound();
        return Ok(MapToDto(feedback));
    }

    [HttpPost]
    public async Task<IActionResult> Submit(FeedbackCreateDto dto)
    {
        var feedback = new Feedback
        {
            Rating = dto.Rating,
            Category = dto.Category,
            Comments = dto.Comments,
            SubmittedBy = _currentUser.Name,
            SubmittedByUserId = _currentUser.UserId,
            SubmittedByRole = _currentUser.Role,
        };

        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(feedback));
    }

    // Feedback content itself isn't editable — only the review workflow status changes
    // (e.g. "Pending Review" -> "Reviewed"), so this is the only mutation Admin gets.
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, FeedbackStatusUpdateDto dto)
    {
        var feedback = await _db.Feedbacks.FindAsync(id);
        if (feedback == null) return NotFound();

        feedback.Status = dto.Status;
        await _db.SaveChangesAsync();
        return Ok(MapToDto(feedback));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var feedback = await _db.Feedbacks.FindAsync(id);
        if (feedback == null) return NotFound();

        _db.Feedbacks.Remove(feedback);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Feedback deleted" });
    }

    private static FeedbackDto MapToDto(Feedback f) => new()
    {
        Id = f.Id,
        ReferenceCode = $"FB-{f.Id:D2}",
        Rating = f.Rating,
        Category = f.Category,
        Comments = f.Comments,
        SubmittedBy = f.SubmittedBy,
        SubmittedByRole = f.SubmittedByRole,
        Status = f.Status,
        CreatedAt = f.CreatedAt
    };
}
