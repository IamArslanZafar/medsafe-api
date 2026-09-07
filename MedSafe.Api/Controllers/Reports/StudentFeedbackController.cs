using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

// Backs the "Student Feedback" module's Feedback Form + Feedback List pages.
// Distinct table from the existing Feedback/FeedbackController — that's an
// unrelated, already-live module in this same codebase.
[ApiController]
[Route("api/student-feedback")]
[Authorize]
public class StudentFeedbackController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public StudentFeedbackController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // Any logged-in user can submit — matches FeedbackController.Submit (no role restriction).
    [HttpPost]
    public async Task<IActionResult> Submit(StudentFeedbackCreateDto dto)
    {
        var entry = new StudentFeedback
        {
            StudentName = dto.StudentName,
            OverallQuality = dto.OverallQuality,
            InstructorClarity = dto.InstructorClarity,
            InstructorRatingsJson = dto.InstructorRatingsJson,
            InstructorComments = dto.InstructorComments,
            MaterialsSufficient = dto.MaterialsSufficient,
            MaterialsComments = dto.MaterialsComments,
            EnvironmentRating = dto.EnvironmentRating,
            SupportServicesJson = dto.SupportServicesJson,
            SubmittedByUserId = _currentUser.UserId,
            SubmittedByRole = _currentUser.Role,
        };

        _db.StudentFeedbacks.Add(entry);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(entry));
    }

    // Matches the "review_feedback" permission already gating the Feedback List route.
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.StudentFeedbacks.OrderByDescending(f => f.CreatedAt).ToListAsync();
        return Ok(list.Select(MapToDto));
    }

    private static StudentFeedbackDto MapToDto(StudentFeedback f) => new()
    {
        Id = f.Id,
        StudentName = f.StudentName,
        OverallQuality = f.OverallQuality,
        InstructorClarity = f.InstructorClarity,
        InstructorRatingsJson = f.InstructorRatingsJson,
        InstructorComments = f.InstructorComments,
        MaterialsSufficient = f.MaterialsSufficient,
        MaterialsComments = f.MaterialsComments,
        EnvironmentRating = f.EnvironmentRating,
        SupportServicesJson = f.SupportServicesJson,
        SubmittedByUserId = f.SubmittedByUserId,
        SubmittedByRole = f.SubmittedByRole,
        CreatedAt = f.CreatedAt,
    };
}
