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
    private readonly ICurrentUserService _currentUser;

    public IncidentReportsController(
        IIncidentReportService incidentReportService,
        IIncidentAttachmentService attachmentService,
        ICurrentUserService currentUser)
    {
        _incidentReportService = incidentReportService;
        _attachmentService = attachmentService;
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
}
