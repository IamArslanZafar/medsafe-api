using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/feedback")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly AppDbContext _db;

    public FeedbackController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetFeedback()
    {
        var list = await _db.Feedbacks.OrderByDescending(f => f.CreatedAt).ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Submit(FeedbackCreateDto dto)
    {
        var name = User.FindFirst(ClaimTypes.Name)!.Value;

        _db.Feedbacks.Add(new Feedback
        {
            Rating = dto.Rating,
            Category = dto.Category,
            Comments = dto.Comments,
            SubmittedBy = name
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = "Feedback submitted" });
    }
}

public class FeedbackCreateDto
{
    [Range(1, 5)] public int Rating { get; set; }
    [Required] public string Category { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
}
