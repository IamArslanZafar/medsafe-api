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
    private readonly IAlertRuleEvaluationService _alertRuleEvaluationService;
    private readonly ILogger<IncidentReportService> _logger;

    public IncidentReportService(
        AppDbContext db,
        ICurrentUserService currentUser,
        IAlertRuleEvaluationService alertRuleEvaluationService,
        ILogger<IncidentReportService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _alertRuleEvaluationService = alertRuleEvaluationService;
        _logger = logger;
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

            AddMedications(incident.Id, request.Medications);
            AddAllergies(incident.Id, request.KnownAllergyIds);
            AddCurrentMedications(incident.Id, request.CurrentMedicationIds);
            AddContributingFactors(incident.Id, request.ContributingFactorIds);
            AddSeriousnessCriteria(incident.Id, request.SeriousnessCriterionIds);
            AddNotifications(incident.Id, request.Notifications);
            AddInitialStatusHistory(incident.Id);
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

        // The report is already saved and committed at this point — a failure here
        // must not turn into a 500 for report submission, so it's caught and logged
        // rather than rethrown. Runs after commit so a rule-evaluation error can
        // never roll back a successfully submitted clinical report.
        try
        {
            await _alertRuleEvaluationService.EvaluateIncidentAsync(incident.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert rule evaluation failed for incident {IncidentReportId}.", incident.Id);
        }

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
            .Include(r => r.Medications)
            .Include(r => r.ContributingFactors)
            .Include(r => r.SeriousnessCriteria)
            .Include(r => r.AllergyLinks)
            .Include(r => r.CurrentMedicationLinks)
            .Include(r => r.Attachments)
            .Include(r => r.Notifications)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return report == null ? null : MapToDto(report);
    }

    public async Task<List<IncidentReportDto>> GetListAsync(IncidentReportListRequest request, CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_db.IncidentReports.AsQueryable(), request);

        var reports = await query
            .Include(r => r.Medications)
            .Include(r => r.ContributingFactors)
            .Include(r => r.SeriousnessCriteria)
            .Include(r => r.AllergyLinks)
            .Include(r => r.CurrentMedicationLinks)
            .Include(r => r.Attachments)
            .Include(r => r.Notifications)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);

        return reports.Select(MapToDto).ToList();
    }

    public async Task<IncidentReportSummaryDto> GetSummaryAsync(IncidentReportListRequest request, CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_db.IncidentReports.AsNoTracking().AsQueryable(), request);

        var summary = await query
            .GroupBy(x => 1)
            .Select(g => new IncidentReportSummaryDto
            {
                TotalReports = g.Count(),
                AwaitingReview = g.Count(x => x.ReportStatus == "Pending" || x.ReportStatus == "UnderReview"),
                HarmEvents = g.Count(x =>
                    x.HarmLevelCode == "E" || x.HarmLevelCode == "F" || x.HarmLevelCode == "G" ||
                    x.HarmLevelCode == "H" || x.HarmLevelCode == "I"),
                ClosedCases = g.Count(x => x.ReportStatus == "Closed")
            })
            .FirstOrDefaultAsync(cancellationToken);

        return summary ?? new IncidentReportSummaryDto();
    }

    // Shared by GetListAsync/GetSummaryAsync so both stay in sync. Only Admin sees
    // every report — every other role (Nurse, Physician, Pharmacist) is scoped to
    // reports they submitted themselves. Note: this means Physician can no longer
    // see Nurse-submitted reports to review/sign off on — an explicit, confirmed
    // tradeoff, not an oversight.
    private IQueryable<IncidentReport> ApplyFilters(IQueryable<IncidentReport> query, IncidentReportListRequest request)
    {
        if (_currentUser.Role != "Admin")
            query = query.Where(r => r.SubmittedByUserId == _currentUser.UserId);

        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            var start = request.StartDate.Value.Date;
            var end = request.EndDate.Value.Date;
            query = query.Where(r => r.SubmittedAt.Date >= start && r.SubmittedAt.Date <= end);
        }

        if (!string.IsNullOrWhiteSpace(request.FacilityUnit) && request.FacilityUnit != "All Units")
            query = query.Where(r => r.IncidentLocation == request.FacilityUnit);

        if (!string.IsNullOrWhiteSpace(request.MedicationName))
            query = query.Where(r => r.Medications.Any(m => m.MedicationName == request.MedicationName));

        if (request.ErrorCategoryId.HasValue)
            query = query.Where(r => r.ErrorCategoryId == request.ErrorCategoryId);

        if (request.StageOfProcessId.HasValue)
            query = query.Where(r => r.StageOfProcessId == request.StageOfProcessId);

        if (request.PatientOutcomeId.HasValue)
            query = query.Where(r => r.PatientOutcomeId == request.PatientOutcomeId);

        if (!string.IsNullOrWhiteSpace(request.SuspectedCausality))
            query = query.Where(r => r.SuspectedCausality == request.SuspectedCausality);

        if (request.ContributingFactorId.HasValue)
            query = query.Where(r => r.ContributingFactors.Any(cf => cf.ContributingFactorId == request.ContributingFactorId));

        if (request.SeriousnessCriterionId.HasValue)
            query = query.Where(r => r.SeriousnessCriteria.Any(sc => sc.SeriousnessCriterionId == request.SeriousnessCriterionId));

        return query;
    }

    private async Task ValidateRequestAsync(SubmitIncidentReportRequest request, CancellationToken cancellationToken)
    {
        // Step 1 - Patient
        if (string.IsNullOrWhiteSpace(request.PatientName))
            throw new ValidationException("Patient name is required.");

        if (string.IsNullOrWhiteSpace(request.PatientRef))
            throw new ValidationException("Patient reference / MRN is required.");

        if (request.PatientAge < 0 || request.PatientAge > 130)
            throw new ValidationException("Invalid patient age.");

        if (string.IsNullOrWhiteSpace(request.PatientSex))
            throw new ValidationException("Patient sex is required.");

        if (request.PatientWeightKg is <= 0)
            throw new ValidationException("Patient weight must be greater than zero.");

        // Step 2 - Medications
        if (request.Medications.Count == 0)
            throw new ValidationException("At least one medication is required.");

        foreach (var medication in request.Medications)
        {
            if (string.IsNullOrWhiteSpace(medication.MedicationName))
                throw new ValidationException("Medication name is required.");

            if (medication.DoseValue <= 0)
                throw new ValidationException("Dose must be greater than zero.");

            if (medication.MedicationGivenAt == default)
                throw new ValidationException("Medication date/time is required.");

            if (!await _db.DoseUnits.AnyAsync(x => x.Id == medication.DoseUnitId && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid dose unit.");

            if (!await _db.Routes.AnyAsync(x => x.Id == medication.RouteId && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid route.");

            if (medication.FrequencyId.HasValue &&
                !await _db.Frequencies.AnyAsync(x => x.Id == medication.FrequencyId && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid frequency.");

            if (medication.FormulationId.HasValue &&
                !await _db.Formulations.AnyAsync(x => x.Id == medication.FormulationId && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid formulation.");
        }

        // Step 3 - Incident / harm
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

        if (request.IncidentOccurredAt == default)
            throw new ValidationException("Incident date and time is required.");

        if (string.IsNullOrWhiteSpace(request.IncidentLocation))
            throw new ValidationException("Incident location / unit is required.");

        if (string.IsNullOrWhiteSpace(request.IncidentNarrative))
            throw new ValidationException("Incident narrative is required.");

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

        // Step 4 - Profession / Position
        var professionExists = await _db.Professions.AnyAsync(x => x.Id == request.ProfessionId && x.IsActive, cancellationToken);
        if (!professionExists)
            throw new ValidationException("Invalid profession.");

        var validPosition = await _db.Positions.AnyAsync(
            x => x.Id == request.PositionId && x.ProfessionId == request.ProfessionId && x.IsActive, cancellationToken);
        if (!validPosition)
            throw new ValidationException("Invalid position for selected profession.");

        foreach (var notification in request.Notifications)
        {
            if (string.IsNullOrWhiteSpace(notification.PersonName))
                throw new ValidationException("Person notified name is required.");

            if (notification.NotifiedAt == default)
                throw new ValidationException("Notification date/time is required.");

            var validType = await _db.NotificationRecipientTypes
                .AnyAsync(x => x.Id == notification.NotificationTypeId && x.IsActive, cancellationToken);
            if (!validType)
                throw new ValidationException("Invalid notification type.");
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
            ReportStatus = "Pending",

            // Step 1
            PatientName = request.PatientName.Trim(),
            PatientReference = request.PatientRef.Trim(),
            PatientAge = request.PatientAge,
            PatientSex = request.PatientSex.Trim(),
            PatientWeightKg = request.PatientWeightKg,

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
            PatientOutcomeId = request.PatientOutcomeId,

            // Step 4
            ProfessionId = request.ProfessionId,
            PositionId = request.PositionId,

            // Step 5
            ImmediateActionTaken = request.ImmediateActionTaken?.Trim(),
            PatientOutcomeDetails = request.PatientOutcomeDetails?.Trim()
        };
    }

    private void AddMedications(int incidentReportId, List<IncidentMedicationRequest> medications)
    {
        foreach (var medication in medications)
        {
            _db.IncidentReportMedications.Add(new IncidentReportMedication
            {
                IncidentReportId = incidentReportId,
                MedicationName = medication.MedicationName.Trim(),
                DoseValue = medication.DoseValue,
                DoseUnitId = medication.DoseUnitId,
                RouteId = medication.RouteId,
                FrequencyId = medication.FrequencyId,
                FormulationId = medication.FormulationId,
                MedicationGivenAt = medication.MedicationGivenAt,
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
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

    private void AddNotifications(int incidentReportId, List<ManualNotificationRequest> notifications)
    {
        foreach (var notification in notifications)
        {
            _db.IncidentNotifications.Add(new IncidentNotification
            {
                IncidentReportId = incidentReportId,
                NotificationTypeId = notification.NotificationTypeId,
                PersonName = notification.PersonName.Trim(),
                NotifiedAt = notification.NotifiedAt,
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddInitialStatusHistory(int incidentReportId)
    {
        _db.IncidentReportStatusHistories.Add(new IncidentReportStatusHistory
        {
            IncidentReportId = incidentReportId,
            FromStatus = null,
            ToStatus = "Pending",
            ChangedByUserId = _currentUser.UserId,
            ChangedAt = DateTime.UtcNow,
            Reason = "Report submitted"
        });
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
        ReportStatus = r.ReportStatus,
        PatientReferenceToken = r.PatientReferenceToken,
        PatientReference = r.PatientReference,
        PatientName = r.PatientName,
        PatientAge = r.PatientAge,
        PatientSex = r.PatientSex,
        PatientWeightKg = r.PatientWeightKg,
        KnownAllergyIds = r.AllergyLinks.Select(a => a.AllergyId).ToList(),
        CurrentMedicationIds = r.CurrentMedicationLinks.Select(m => m.CurrentMedicationId).ToList(),
        Medications = r.Medications.Select(m => new IncidentMedicationDto
        {
            Id = m.Id,
            MedicationName = m.MedicationName,
            DoseValue = m.DoseValue,
            DoseUnitId = m.DoseUnitId,
            RouteId = m.RouteId,
            FrequencyId = m.FrequencyId,
            FormulationId = m.FormulationId,
            MedicationGivenAt = m.MedicationGivenAt
        }).ToList(),
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
        ProfessionId = r.ProfessionId,
        PositionId = r.PositionId,
        Notifications = r.Notifications.Select(n => new ManualNotificationDto
        {
            Id = n.Id,
            NotificationTypeId = n.NotificationTypeId,
            PersonName = n.PersonName,
            NotifiedAt = n.NotifiedAt
        }).ToList(),
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
