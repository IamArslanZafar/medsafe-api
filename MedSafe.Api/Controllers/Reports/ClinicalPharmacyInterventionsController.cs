using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

// Backs the "Clinical Pharmacy Intervention" module's dashboard + New Intervention form.
[ApiController]
[Route("api/clinical-pharmacy-interventions")]
[Authorize]
public class ClinicalPharmacyInterventionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IClinicalPharmacyInterventionAttachmentService _attachments;
    private readonly ICurrentUserService _currentUser;

    public ClinicalPharmacyInterventionsController(AppDbContext db, IClinicalPharmacyInterventionAttachmentService attachments, ICurrentUserService currentUser)
    {
        _db = db;
        _attachments = attachments;
        _currentUser = currentUser;
    }

    // Uploaded standalone from the form's "+ Add Attachment" picker, before
    // the intervention itself is created (the form supports multiple files)
    // — each returned id is collected into AttachmentIdsJson on the POST below.
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
    public async Task<IActionResult> Submit(ClinicalPharmacyInterventionCreateDto dto)
    {
        var intervention = new ClinicalPharmacyIntervention
        {
            InterventionCode = $"CPI-{Random.Shared.Next(1000000, 9999999)}",
            Mrn = dto.Mrn,
            PatientName = dto.PatientName,
            Dob = dto.Dob,
            Sex = dto.Sex,
            VisitType = dto.VisitType,
            Facility = dto.Facility,
            DepartmentUnit = dto.DepartmentUnit,
            Diagnosis = dto.Diagnosis,
            ReportingFacility = dto.ReportingFacility,
            ServiceType = dto.ServiceType,
            Medication = dto.Medication,
            Strength = dto.Strength,
            Route = dto.Route,
            Frequency = dto.Frequency,
            CurrentDose = dto.CurrentDose,
            Indication = dto.Indication,
            StartDate = dto.StartDate,
            LastDose = dto.LastDose,
            DrugClass = dto.DrugClass,
            InterventionType = dto.InterventionType,
            InterventionSubtype = dto.InterventionSubtype,
            IdentifiedProblem = dto.IdentifiedProblem,
            RecommendedAction = dto.RecommendedAction,
            RecommendedDose = dto.RecommendedDose,
            RecommendedFrequency = dto.RecommendedFrequency,
            RecommendedMonitoring = dto.RecommendedMonitoring,
            ClinicalRationale = dto.ClinicalRationale,
            Importance = dto.Importance,
            TimeSpent = dto.TimeSpent,
            EstimatedSaving = dto.EstimatedSaving,
            Currency = dto.Currency,
            MedError = dto.MedError,
            Merp = dto.Merp,
            Adr = dto.Adr,
            PrescriberName = dto.PrescriberName,
            PrescriberDepartment = dto.PrescriberDepartment,
            ContactMethod = dto.ContactMethod,
            ContactDateTime = dto.ContactDateTime,
            PrescriberResponse = dto.PrescriberResponse,
            ResponseDetails = dto.ResponseDetails,
            Outcome = dto.Outcome,
            FollowupRequired = dto.FollowupRequired,
            FollowupDate = dto.FollowupDate,
            AdditionalNotes = dto.AdditionalNotes,
            DraftStatus = dto.DraftStatus,
            AttachmentIdsJson = dto.AttachmentIdsJson,
            SubmittedByUserId = _currentUser.UserId,
            SubmittedByRole = _currentUser.Role,
        };

        _db.ClinicalPharmacyInterventions.Add(intervention);
        await _db.SaveChangesAsync();
        return Ok(MapToDto(intervention));
    }

    // Matches the "clinical_review" permission already gating the dashboard route.
    // Admin (or a user granted "Admin Data Access") sees every intervention;
    // everyone else sees only what they themselves submitted — same rule as
    // GET /incident-reports.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = _db.ClinicalPharmacyInterventions.AsQueryable();
        if (_currentUser.Role != "Admin" && !_currentUser.HasFullDataAccess)
            query = query.Where(i => i.SubmittedByUserId == _currentUser.UserId);

        var list = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
        return Ok(list.Select(MapToDto));
    }

    private static ClinicalPharmacyInterventionDto MapToDto(ClinicalPharmacyIntervention i) => new()
    {
        Id = i.Id,
        InterventionCode = i.InterventionCode,
        Mrn = i.Mrn,
        PatientName = i.PatientName,
        Dob = i.Dob,
        Sex = i.Sex,
        VisitType = i.VisitType,
        Facility = i.Facility,
        DepartmentUnit = i.DepartmentUnit,
        Diagnosis = i.Diagnosis,
        ReportingFacility = i.ReportingFacility,
        ServiceType = i.ServiceType,
        Medication = i.Medication,
        Strength = i.Strength,
        Route = i.Route,
        Frequency = i.Frequency,
        CurrentDose = i.CurrentDose,
        Indication = i.Indication,
        StartDate = i.StartDate,
        LastDose = i.LastDose,
        DrugClass = i.DrugClass,
        InterventionType = i.InterventionType,
        InterventionSubtype = i.InterventionSubtype,
        IdentifiedProblem = i.IdentifiedProblem,
        RecommendedAction = i.RecommendedAction,
        RecommendedDose = i.RecommendedDose,
        RecommendedFrequency = i.RecommendedFrequency,
        RecommendedMonitoring = i.RecommendedMonitoring,
        ClinicalRationale = i.ClinicalRationale,
        Importance = i.Importance,
        TimeSpent = i.TimeSpent,
        EstimatedSaving = i.EstimatedSaving,
        Currency = i.Currency,
        MedError = i.MedError,
        Merp = i.Merp,
        Adr = i.Adr,
        PrescriberName = i.PrescriberName,
        PrescriberDepartment = i.PrescriberDepartment,
        ContactMethod = i.ContactMethod,
        ContactDateTime = i.ContactDateTime,
        PrescriberResponse = i.PrescriberResponse,
        ResponseDetails = i.ResponseDetails,
        Outcome = i.Outcome,
        FollowupRequired = i.FollowupRequired,
        FollowupDate = i.FollowupDate,
        AdditionalNotes = i.AdditionalNotes,
        DraftStatus = i.DraftStatus,
        AttachmentIdsJson = i.AttachmentIdsJson,
        SubmittedByUserId = i.SubmittedByUserId,
        SubmittedByRole = i.SubmittedByRole,
        CreatedAt = i.CreatedAt,
    };
}
