using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/incident-reports")]
[Authorize]
public class IncidentReportsController : ControllerBase
{
    private readonly IIncidentReportService _incidentReportService;
    private readonly IIncidentAttachmentService _attachmentService;
    private readonly IIncidentReportReviewService _reviewService;

    public IncidentReportsController(
        IIncidentReportService incidentReportService,
        IIncidentAttachmentService attachmentService,
        IIncidentReportReviewService reviewService)
    {
        _incidentReportService = incidentReportService;
        _attachmentService = attachmentService;
        _reviewService = reviewService;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitIncidentReportRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentReportService.SubmitAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // POST (not GET) — filters go in the body, same shape/convention as
    // /api/dashboard/summary. An empty body ({}) returns exactly what the old
    // parameterless GET returned (role-scoped, no other filtering).
    [HttpPost("list")]
    public async Task<IActionResult> GetAll([FromBody] IncidentReportListRequest request, CancellationToken cancellationToken)
    {
        var reports = await _incidentReportService.GetListAsync(request, cancellationToken);
        return Ok(reports);
    }

    [HttpPost("summary")]
    public async Task<IActionResult> GetSummary([FromBody] IncidentReportListRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentReportService.GetSummaryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        // Admin, the submitter, anyone the report notified, and the assigned reviewer
        // can view it — otherwise a report a user was notified to act on would 403.
        if (!await _incidentReportService.CanCurrentUserAccessAsync(id, cancellationToken))
            return Forbid();

        var report = await _incidentReportService.GetByIdAsync(id, cancellationToken);
        if (report == null) return NotFound();

        return Ok(report);
    }

    [HttpPost("{incidentReportId:int}/attachments")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadAttachment(int incidentReportId, IFormFile file, [FromForm] string? category, [FromForm] string? description, CancellationToken cancellationToken)
    {
        if (!await _incidentReportService.CanCurrentUserAccessAsync(incidentReportId, cancellationToken))
            return Forbid();

        var result = await _attachmentService.UploadAsync(incidentReportId, file, category, description, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{incidentReportId:int}/attachments/{attachmentId:int}/download")]
    public async Task<IActionResult> DownloadAttachment(int incidentReportId, int attachmentId, CancellationToken cancellationToken)
    {
        if (!await _incidentReportService.CanCurrentUserAccessAsync(incidentReportId, cancellationToken))
            return Forbid();

        var (stream, contentType, fileName) = await _attachmentService.DownloadAsync(incidentReportId, attachmentId, cancellationToken);
        return File(stream, contentType, fileName);
    }

    [HttpPost("{id:int}/reviews/start")]
    [Authorize(Roles = "Physician,Admin")]
    public async Task<IActionResult> StartReview(int id, CancellationToken cancellationToken)
    {
        if (!await _incidentReportService.CanCurrentUserAccessAsync(id, cancellationToken))
            return Forbid();

        var result = await _reviewService.StartReviewAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/reviews/sign-off")]
    [Authorize(Roles = "Physician,Admin")]
    public async Task<IActionResult> SignOffReview(int id, [FromBody] SignOffReviewRequest request, CancellationToken cancellationToken)
    {
        if (!await _incidentReportService.CanCurrentUserAccessAsync(id, cancellationToken))
            return Forbid();

        var result = await _reviewService.SignOffReviewAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/review")]
    public async Task<IActionResult> GetReview(int id, CancellationToken cancellationToken)
    {
        if (!await _incidentReportService.CanCurrentUserAccessAsync(id, cancellationToken))
            return Forbid();

        var review = await _reviewService.GetReviewAsync(id, cancellationToken);
        if (review == null) return NotFound();
        return Ok(review);
    }

    [HttpGet("review-action-owners")]
    public async Task<IActionResult> GetReviewActionOwners(CancellationToken cancellationToken)
    {
        var owners = await _reviewService.GetActionOwnersAsync(cancellationToken);
        return Ok(owners);
    }
}
