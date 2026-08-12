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
    private readonly ICurrentUserService _currentUser;

    public IncidentReportsController(
        IIncidentReportService incidentReportService,
        IIncidentAttachmentService attachmentService,
        IIncidentReportReviewService reviewService,
        ICurrentUserService currentUser)
    {
        _incidentReportService = incidentReportService;
        _attachmentService = attachmentService;
        _reviewService = reviewService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitIncidentReportRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentReportService.SubmitAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var reports = await _incidentReportService.GetListAsync(cancellationToken);
        return Ok(reports);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _incidentReportService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var report = await _incidentReportService.GetByIdAsync(id, cancellationToken);
        if (report == null) return NotFound();

        if (_currentUser.Role == "Nurse" && report.SubmittedByUserId != _currentUser.UserId)
            return Forbid();

        return Ok(report);
    }

    [HttpPost("{incidentReportId:int}/attachments")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadAttachment(int incidentReportId, IFormFile file, CancellationToken cancellationToken)
    {
        var result = await _attachmentService.UploadAsync(incidentReportId, file, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{incidentReportId:int}/attachments/{attachmentId:int}/download")]
    public async Task<IActionResult> DownloadAttachment(int incidentReportId, int attachmentId, CancellationToken cancellationToken)
    {
        var (stream, contentType, fileName) = await _attachmentService.DownloadAsync(incidentReportId, attachmentId, cancellationToken);
        return File(stream, contentType, fileName);
    }

    [HttpPost("{id:int}/reviews/start")]
    [Authorize(Roles = "Physician,Admin")]
    public async Task<IActionResult> StartReview(int id, CancellationToken cancellationToken)
    {
        var result = await _reviewService.StartReviewAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/reviews/sign-off")]
    [Authorize(Roles = "Physician,Admin")]
    public async Task<IActionResult> SignOffReview(int id, [FromBody] SignOffReviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _reviewService.SignOffReviewAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/review")]
    public async Task<IActionResult> GetReview(int id, CancellationToken cancellationToken)
    {
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
