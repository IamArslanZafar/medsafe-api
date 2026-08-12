using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;
using MedSafeAPI.Exceptions;

namespace MedSafeAPI.Services;

public class IncidentReportReviewService : IIncidentReportReviewService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IncidentReportReviewService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StartReviewResponse> StartReviewAsync(int incidentReportId, CancellationToken cancellationToken)
    {
        var report = await _db.IncidentReports
            .Where(r => r.Id == incidentReportId)
            .Select(r => new { r.IncidentReportNumber })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Incident report not found.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // Atomic guarded update: only the request that actually flips Pending -> UnderReview
        // proceeds to create the review. This is the first of two protections against two
        // reviewers claiming the same report at the same time (the second is the unique
        // index on IncidentReportReviews.IncidentReportId).
        var updatedRows = await _db.IncidentReports
            .Where(r => r.Id == incidentReportId && r.ReportStatus == "Pending")
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.ReportStatus, "UnderReview"), cancellationToken);

        if (updatedRows != 1)
        {
            var currentStatus = await _db.IncidentReports
                .Where(r => r.Id == incidentReportId)
                .Select(r => r.ReportStatus)
                .FirstAsync(cancellationToken);
            throw new ConflictException(currentStatus == "UnderReview"
                ? "This report is already under review."
                : $"This report cannot be started for review because it is {currentStatus}.");
        }

        var now = DateTime.UtcNow;
        var review = new IncidentReportReview
        {
            IncidentReportId = incidentReportId,
            ReviewerUserId = _currentUser.UserId,
            ResolutionStatus = "Open",
            StartedAt = now,
            CreatedAt = now
        };
        _db.IncidentReportReviews.Add(review);

        _db.IncidentReportStatusHistories.Add(new IncidentReportStatusHistory
        {
            IncidentReportId = incidentReportId,
            FromStatus = "Pending",
            ToStatus = "UnderReview",
            ChangedByUserId = _currentUser.UserId,
            ChangedAt = now,
            Reason = "Clinical review started"
        });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var reviewerName = await _db.Users
            .Where(u => u.Id == _currentUser.UserId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new StartReviewResponse
        {
            ReviewId = review.Id,
            IncidentReportId = incidentReportId,
            IncidentReportNumber = report.IncidentReportNumber,
            ReviewerUserId = review.ReviewerUserId,
            ReviewerName = reviewerName,
            ReportStatus = "UnderReview",
            ResolutionStatus = review.ResolutionStatus,
            StartedAt = review.StartedAt
        };
    }

    public async Task<SignOffReviewResponse> SignOffReviewAsync(int incidentReportId, SignOffReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await _db.IncidentReportReviews
            .FirstOrDefaultAsync(r => r.IncidentReportId == incidentReportId, cancellationToken)
            ?? throw new ConflictException("This report is not under review.");

        if (review.ReviewerUserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Only the assigned reviewer can sign off this report.");

        if (review.ResolutionStatus == "Closed")
            throw new ConflictException("This review has already been signed off.");

        if (request.ActionOwnerUserId.HasValue &&
            !await _db.Users.AnyAsync(u => u.Id == request.ActionOwnerUserId, cancellationToken))
            throw new ValidationException("Invalid action owner.");

        var incident = await _db.IncidentReports.FirstAsync(r => r.Id == incidentReportId, cancellationToken);
        var now = DateTime.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        review.ClinicalAssessmentNote = request.ClinicalAssessmentNote.Trim();
        review.FollowUpActions = request.FollowUpActions?.Trim();
        review.ActionOwnerUserId = request.ActionOwnerUserId;
        review.ResolutionStatus = "Closed";
        review.SignedOffAt = now;
        review.UpdatedAt = now;

        incident.ReportStatus = "Closed";

        _db.IncidentReportStatusHistories.Add(new IncidentReportStatusHistory
        {
            IncidentReportId = incidentReportId,
            FromStatus = "UnderReview",
            ToStatus = "Closed",
            ChangedByUserId = _currentUser.UserId,
            ChangedAt = now,
            Reason = "Clinical review signed off"
        });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var names = await _db.Users
            .Where(u => u.Id == review.ReviewerUserId || u.Id == review.ActionOwnerUserId)
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        return new SignOffReviewResponse
        {
            ReviewId = review.Id,
            IncidentReportId = incidentReportId,
            IncidentReportNumber = incident.IncidentReportNumber,
            ReviewerUserId = review.ReviewerUserId,
            ReviewerName = names.GetValueOrDefault(review.ReviewerUserId, string.Empty),
            ActionOwnerUserId = review.ActionOwnerUserId,
            ActionOwnerName = review.ActionOwnerUserId.HasValue ? names.GetValueOrDefault(review.ActionOwnerUserId.Value) : null,
            ReportStatus = incident.ReportStatus,
            ResolutionStatus = review.ResolutionStatus,
            SignedOffAt = review.SignedOffAt
        };
    }

    public async Task<IncidentReportReviewDto?> GetReviewAsync(int incidentReportId, CancellationToken cancellationToken)
    {
        var review = await _db.IncidentReportReviews
            .FirstOrDefaultAsync(r => r.IncidentReportId == incidentReportId, cancellationToken);
        if (review == null) return null;

        var incident = await _db.IncidentReports
            .Where(r => r.Id == incidentReportId)
            .Select(r => new { r.IncidentReportNumber, r.ReportStatus })
            .FirstAsync(cancellationToken);

        var names = await _db.Users
            .Where(u => u.Id == review.ReviewerUserId || u.Id == review.ActionOwnerUserId)
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        return new IncidentReportReviewDto
        {
            ReviewId = review.Id,
            IncidentReportId = incidentReportId,
            IncidentReportNumber = incident.IncidentReportNumber,
            ReviewerUserId = review.ReviewerUserId,
            ReviewerName = names.GetValueOrDefault(review.ReviewerUserId, string.Empty),
            ClinicalAssessmentNote = review.ClinicalAssessmentNote,
            FollowUpActions = review.FollowUpActions,
            ActionOwnerUserId = review.ActionOwnerUserId,
            ActionOwnerName = review.ActionOwnerUserId.HasValue ? names.GetValueOrDefault(review.ActionOwnerUserId.Value) : null,
            ResolutionStatus = review.ResolutionStatus,
            ReportStatus = incident.ReportStatus,
            StartedAt = review.StartedAt,
            SignedOffAt = review.SignedOffAt
        };
    }

    public async Task<List<ActionOwnerOptionDto>> GetActionOwnersAsync(CancellationToken cancellationToken)
    {
        return await _db.Users
            .Where(u => u.Status == "active")
            .OrderBy(u => u.Name)
            .Select(u => new ActionOwnerOptionDto
            {
                Id = u.Id,
                Name = u.Name,
                Role = u.Role,
                Title = u.Title,
                Unit = u.Unit
            })
            .ToListAsync(cancellationToken);
    }
}
