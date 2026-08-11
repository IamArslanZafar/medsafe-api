using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public class IncidentReportService : IIncidentReportService
{
    private static readonly string[] ValidHarmLevels = ["A", "B", "C", "D", "E", "F", "G", "H", "I"];
    private static readonly string[] ErrorStyleReportTypes = ["Medication Error", "Near Miss"];

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlertService _alertService;

    public IncidentReportService(AppDbContext db, ICurrentUserService currentUser, IAlertService alertService)
    {
        _db = db;
        _currentUser = currentUser;
        _alertService = alertService;
    }

    public async Task<SubmitIncidentReportResponse> SubmitAsync(SubmitIncidentReportRequest request, CancellationToken cancellationToken)
    {
        await ValidateRequestAsync(request, cancellationToken);

        var incident = BuildIncidentReport(request);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.IncidentReports.Add(incident);
            await _db.SaveChangesAsync(cancellationToken);

            AddContributingFactors(incident.Id, request.ContributingFactorIds);
            AddSeriousnessCriteria(incident.Id, request.SeriousnessCriterionIds);
            AddAllergies(incident.Id, request.KnownAllergyIds);
            AddCurrentMedications(incident.Id, request.CurrentMedicationIds);
            await _db.SaveChangesAsync(cancellationToken);

            AddAuditLog(incident);
            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // Runs after the main transaction succeeds — a failure here must not roll back the report.
        await _alertService.EvaluateIncidentAsync(incident.Id, cancellationToken);

        return new SubmitIncidentReportResponse
        {
            Id = incident.Id,
            IncidentReportNumber = incident.IncidentReportNumber,
            PatientReferenceToken = incident.PatientReferenceToken,
            SubmittedAt = incident.SubmittedAt,
            Status = incident.ReportStatus
        };
    }

    public async Task<IncidentReportDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var report = await _db.IncidentReports
            .Include(r => r.ContributingFactors)
            .Include(r => r.SeriousnessCriteria)
            .Include(r => r.AllergyLinks)
            .Include(r => r.CurrentMedicationLinks)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return report == null ? null : MapToDto(report);
    }

    public async Task<List<IncidentReportDto>> GetListAsync(CancellationToken cancellationToken)
    {
        var query = _db.IncidentReports.AsQueryable();

        if (_currentUser.Role == "Nurse")
            query = query.Where(r => r.SubmittedByUserId == _currentUser.UserId);

        var reports = await query
            .Include(r => r.ContributingFactors)
            .Include(r => r.SeriousnessCriteria)
            .Include(r => r.AllergyLinks)
            .Include(r => r.CurrentMedicationLinks)
            .Include(r => r.Attachments)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);

        return reports.Select(MapToDto).ToList();
    }

    private async Task ValidateRequestAsync(SubmitIncidentReportRequest request, CancellationToken cancellationToken)
    {
        // Step 1 - Patient
        if (request.PatientAge < 0 || request.PatientAge > 130)
            throw new ValidationException("Invalid patient age.");

        if (string.IsNullOrWhiteSpace(request.PatientSex))
            throw new ValidationException("Patient sex is required.");

        if (request.PatientWeightKg is <= 0)
            throw new ValidationException("Patient weight must be greater than zero.");

        // Step 2 - Medication
        if (string.IsNullOrWhiteSpace(request.MedicationName))
            throw new ValidationException("Medication name is required.");

        if (request.DoseValue <= 0)
            throw new ValidationException("Dose must be greater than zero.");

        if (!await _db.DoseUnits.AnyAsync(x => x.Id == request.DoseUnitId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid dose unit.");

        if (!await _db.Routes.AnyAsync(x => x.Id == request.RouteId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid medication route.");

        if (request.FrequencyId.HasValue &&
            !await _db.Frequencies.AnyAsync(x => x.Id == request.FrequencyId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid frequency.");

        if (request.FormulationId.HasValue &&
            !await _db.Formulations.AnyAsync(x => x.Id == request.FormulationId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid formulation.");

        if (string.IsNullOrWhiteSpace(request.ReportingFacilityUnit))
            throw new ValidationException("Reporting facility/unit is required.");

        // Step 3 - Incident / harm
        if (string.IsNullOrWhiteSpace(request.IncidentLocation))
            throw new ValidationException("Incident location is required.");

        if (string.IsNullOrWhiteSpace(request.IncidentNarrative))
            throw new ValidationException("Incident narrative is required.");

        if (!ValidHarmLevels.Contains(request.HarmLevelCode?.ToUpperInvariant()))
            throw new ValidationException("Invalid harm level.");

        if (ErrorStyleReportTypes.Contains(request.ReportType))
        {
            if (!request.ErrorCategoryId.HasValue)
                throw new ValidationException("Error category is required.");
            if (!request.StageOfProcessId.HasValue)
                throw new ValidationException("Stage of process is required.");
        }
        else if (request.ReportType == "ADR")
        {
            if (string.IsNullOrWhiteSpace(request.AdrReactionDescription))
                throw new ValidationException("Reaction description is required for ADR.");
            if (string.IsNullOrWhiteSpace(request.SuspectedCausality))
                throw new ValidationException("Suspected causality is required for ADR.");
        }
        else
        {
            throw new ValidationException("Invalid report type.");
        }

        if (request.ErrorCategoryId.HasValue &&
            !await _db.ErrorCategories.AnyAsync(x => x.Id == request.ErrorCategoryId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid error category.");

        if (request.StageOfProcessId.HasValue &&
            !await _db.StageOfProcesses.AnyAsync(x => x.Id == request.StageOfProcessId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid stage of process.");

        // Step 4 - Outcome
        if (!await _db.PatientOutcomes.AnyAsync(x => x.Id == request.PatientOutcomeId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid patient outcome.");

        // Multi-select lookups
        var contributingFactorIds = request.ContributingFactorIds.Distinct().ToList();
        if (contributingFactorIds.Count > 0)
        {
            var validCount = await _db.ContributingFactors
                .CountAsync(x => contributingFactorIds.Contains(x.Id) && x.IsActive, cancellationToken);
            if (validCount != contributingFactorIds.Count)
                throw new ValidationException("One or more contributing factors are invalid.");
        }

        var seriousnessCriterionIds = request.SeriousnessCriterionIds.Distinct().ToList();
        if (seriousnessCriterionIds.Count > 0)
        {
            var validCount = await _db.SeriousnessCriteria
                .CountAsync(x => seriousnessCriterionIds.Contains(x.Id) && x.IsActive, cancellationToken);
            if (validCount != seriousnessCriterionIds.Count)
                throw new ValidationException("One or more seriousness criteria are invalid.");
        }

        var allergyIds = request.KnownAllergyIds.Distinct().ToList();
        if (allergyIds.Count > 0)
        {
            var validCount = await _db.Allergies
                .CountAsync(x => allergyIds.Contains(x.Id) && x.IsActive, cancellationToken);
            if (validCount != allergyIds.Count)
                throw new ValidationException("One or more allergies are invalid.");
        }

        var currentMedicationIds = request.CurrentMedicationIds.Distinct().ToList();
        if (currentMedicationIds.Count > 0)
        {
            var validCount = await _db.CurrentMedications
                .CountAsync(x => currentMedicationIds.Contains(x.Id) && x.IsActive, cancellationToken);
            if (validCount != currentMedicationIds.Count)
                throw new ValidationException("One or more current medications are invalid.");
        }
    }

    private IncidentReport BuildIncidentReport(SubmitIncidentReportRequest request)
    {
        return new IncidentReport
        {
            // System generated
            IncidentReportNumber = GenerateIncidentReportNumber(),
            PatientReferenceToken = GeneratePatientReferenceToken(),
            SubmittedAt = DateTime.UtcNow,
            SubmittedByUserId = _currentUser.UserId,
            SubmittedByRole = _currentUser.Role,
            ReportStatus = "Submitted",

            // Location
            ReportingFacilityUnit = request.ReportingFacilityUnit.Trim(),

            // Step 1
            PatientAge = request.PatientAge,
            PatientSex = request.PatientSex.Trim(),
            PatientWeightKg = request.PatientWeightKg,
            PatientMedicalHistory = request.PatientMedicalHistory?.Trim(),

            // Step 2
            MedicationName = request.MedicationName.Trim(),
            GenericActiveIngredient = request.GenericActiveIngredient?.Trim(),
            DoseValue = request.DoseValue,
            DoseUnitId = request.DoseUnitId,
            RouteId = request.RouteId,
            FrequencyId = request.FrequencyId,
            FormulationId = request.FormulationId,
            MedicationGivenAt = request.MedicationGivenAt,
            BatchLotNumber = request.BatchLotNumber?.Trim(),

            // Step 3
            ReportType = request.ReportType.Trim(),
            ErrorCategoryId = request.ErrorCategoryId,
            StageOfProcessId = request.StageOfProcessId,
            AdrReactionDescription = request.AdrReactionDescription?.Trim(),
            SuspectedCausality = request.SuspectedCausality?.Trim(),
            HarmLevelCode = request.HarmLevelCode.Trim().ToUpperInvariant(),
            IncidentOccurredAt = request.IncidentOccurredAt,
            IncidentLocation = request.IncidentLocation.Trim(),
            IncidentNarrative = request.IncidentNarrative.Trim(),

            // Step 4
            ImmediateActionTaken = request.ImmediateActionTaken?.Trim(),
            PatientOutcomeId = request.PatientOutcomeId,
            PatientOutcomeDetails = request.PatientOutcomeDetails?.Trim()
        };
    }

    private void AddContributingFactors(int incidentReportId, List<int> contributingFactorIds)
    {
        foreach (var factorId in contributingFactorIds.Distinct())
        {
            _db.IncidentReportContributingFactors.Add(new IncidentReportContributingFactor
            {
                IncidentReportId = incidentReportId,
                ContributingFactorId = factorId,
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddSeriousnessCriteria(int incidentReportId, List<int> seriousnessCriterionIds)
    {
        foreach (var criterionId in seriousnessCriterionIds.Distinct())
        {
            _db.IncidentReportSeriousnessCriteria.Add(new IncidentReportSeriousnessCriterion
            {
                IncidentReportId = incidentReportId,
                SeriousnessCriterionId = criterionId,
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddAllergies(int incidentReportId, List<int> allergyIds)
    {
        foreach (var allergyId in allergyIds.Distinct())
        {
            _db.IncidentReportAllergies.Add(new IncidentReportAllergy
            {
                IncidentReportId = incidentReportId,
                AllergyId = allergyId,
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddCurrentMedications(int incidentReportId, List<int> currentMedicationIds)
    {
        foreach (var medicationId in currentMedicationIds.Distinct())
        {
            _db.IncidentReportCurrentMedications.Add(new IncidentReportCurrentMedication
            {
                IncidentReportId = incidentReportId,
                CurrentMedicationId = medicationId,
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddAuditLog(IncidentReport incident)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            UserName = _currentUser.Name,
            Action = "INCIDENT_REPORT_SUBMITTED",
            Details = $"Incident report {incident.IncidentReportNumber} submitted.",
            Timestamp = DateTime.UtcNow
        });
    }

    private static string GeneratePatientReferenceToken() => $"PT-{Guid.NewGuid():N}".ToUpperInvariant();

    private static string GenerateIncidentReportNumber()
    {
        var uniquePart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"IR-{DateTime.UtcNow:yyyyMMdd}-{uniquePart}";
    }

    private static IncidentReportDto MapToDto(IncidentReport r) => new()
    {
        Id = r.Id,
        IncidentReportNumber = r.IncidentReportNumber,
        SubmittedAt = r.SubmittedAt,
        SubmittedByUserId = r.SubmittedByUserId,
        SubmittedByRole = r.SubmittedByRole,
        ReportingFacilityUnit = r.ReportingFacilityUnit,
        ReportStatus = r.ReportStatus,
        PatientReferenceToken = r.PatientReferenceToken,
        PatientAge = r.PatientAge,
        PatientSex = r.PatientSex,
        PatientWeightKg = r.PatientWeightKg,
        PatientMedicalHistory = r.PatientMedicalHistory,
        KnownAllergyIds = r.AllergyLinks.Select(a => a.AllergyId).ToList(),
        CurrentMedicationIds = r.CurrentMedicationLinks.Select(m => m.CurrentMedicationId).ToList(),
        MedicationName = r.MedicationName,
        GenericActiveIngredient = r.GenericActiveIngredient,
        DoseValue = r.DoseValue,
        DoseUnitId = r.DoseUnitId,
        RouteId = r.RouteId,
        FrequencyId = r.FrequencyId,
        FormulationId = r.FormulationId,
        MedicationGivenAt = r.MedicationGivenAt,
        BatchLotNumber = r.BatchLotNumber,
        ReportType = r.ReportType,
        ErrorCategoryId = r.ErrorCategoryId,
        StageOfProcessId = r.StageOfProcessId,
        AdrReactionDescription = r.AdrReactionDescription,
        SuspectedCausality = r.SuspectedCausality,
        HarmLevelCode = r.HarmLevelCode,
        IncidentOccurredAt = r.IncidentOccurredAt,
        IncidentLocation = r.IncidentLocation,
        IncidentNarrative = r.IncidentNarrative,
        ContributingFactorIds = r.ContributingFactors.Select(f => f.ContributingFactorId).ToList(),
        SeriousnessCriterionIds = r.SeriousnessCriteria.Select(c => c.SeriousnessCriterionId).ToList(),
        ImmediateActionTaken = r.ImmediateActionTaken,
        PatientOutcomeId = r.PatientOutcomeId,
        PatientOutcomeDetails = r.PatientOutcomeDetails,
        Attachments = r.Attachments.Where(a => !a.IsDeleted).Select(a => new IncidentReportAttachmentDto
        {
            Id = a.Id,
            OriginalFileName = a.OriginalFileName,
            ContentType = a.ContentType,
            FileSizeBytes = a.FileSizeBytes,
            UploadedAt = a.UploadedAt,
            UploadedByUserId = a.UploadedByUserId
        }).ToList()
    };
}
