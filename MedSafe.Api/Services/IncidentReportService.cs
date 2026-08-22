using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public class IncidentReportService : IIncidentReportService
{
    private const string MedicationErrorCode = "MEDICATION_ERROR";
    private const string AdrCode = "ADR";

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
        var reportType = await ValidateRequestAsync(request, cancellationToken);

        var incident = await BuildIncidentReport(request, reportType, cancellationToken);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.IncidentReports.Add(incident);
            await _db.SaveChangesAsync(cancellationToken);

            AddMedications(incident.Id, request.Medications);
            AddAllergies(incident.Id, request.KnownAllergyIds);
            AddCurrentMedications(incident.Id, request.CurrentMedicationIds);
            AddContributingFactors(incident.Id, request.ContributingFactorIds);
            AddHealthcareProfessionals(incident.Id, request.OtherHealthcareProfessionals);
            AddOtherDepartments(incident.Id, request.OtherDepartmentIds);
            AddWitnesses(incident.Id, request.Witnesses);
            AddReporters(incident.Id, request.Reporters);
            AddManualNotifications(incident.Id, request.ManualNotifications);

            if (reportType.Code == AdrCode)
            {
                AddSeriousnessCriteria(incident.Id, request.SeriousnessCriterionIds);
                AddConcomitantMedications(incident.Id, request.ConcomitantMedications);
            }

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
        // AsSplitQuery — see IncidentReportPdfService.GenerateAsync for why: this many
        // sibling collection Includes in one query cartesian-products into a query cost
        // that can exceed the live DB's shared-hosting query governor.
        var report = await _db.IncidentReports
            .AsSplitQuery()
            .Include(r => r.Medications)
            .Include(r => r.ContributingFactors)
            .Include(r => r.SeriousnessCriteria)
            .Include(r => r.AllergyLinks)
            .Include(r => r.CurrentMedicationLinks)
            .Include(r => r.Attachments)
            .Include(r => r.ConcomitantMedications)
            .Include(r => r.HealthcareProfessionals)
            .Include(r => r.Witnesses)
            .Include(r => r.OtherDepartments)
            .Include(r => r.Reporters)
            .Include(r => r.ManualNotifications)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return report == null ? null : await MapToDtoAsync(report, cancellationToken);
    }

    public async Task<bool> CanCurrentUserAccessAsync(int incidentReportId, CancellationToken cancellationToken)
    {
        if (_currentUser.Role == "Admin")
            return true;

        var userId = _currentUser.UserId;

        var isSubmitter = await _db.IncidentReports
            .AnyAsync(r => r.Id == incidentReportId && r.SubmittedByUserId == userId, cancellationToken);
        if (isSubmitter)
            return true;

        var isNotificationRecipient = await _db.IncidentNotifications
            .AnyAsync(n => n.IncidentReportId == incidentReportId && n.RecipientUserId == userId, cancellationToken);
        if (isNotificationRecipient)
            return true;

        return await _db.IncidentReportReviews
            .AnyAsync(r => r.IncidentReportId == incidentReportId && r.ReviewerUserId == userId, cancellationToken);
    }

    public async Task<List<IncidentReportDto>> GetListAsync(IncidentReportListRequest request, CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_db.IncidentReports.AsQueryable(), request);

        // AsSplitQuery — see IncidentReportPdfService.GenerateAsync for why: this many
        // sibling collection Includes cartesian-products per report, and here it's applied
        // across the whole matching list at once, making this the most expensive query
        // in the app under the live DB's shared-hosting query governor.
        var reports = await query
            .AsSplitQuery()
            .Include(r => r.Medications)
            .Include(r => r.ContributingFactors)
            .Include(r => r.SeriousnessCriteria)
            .Include(r => r.AllergyLinks)
            .Include(r => r.CurrentMedicationLinks)
            .Include(r => r.Attachments)
            .Include(r => r.ConcomitantMedications)
            .Include(r => r.HealthcareProfessionals)
            .Include(r => r.Witnesses)
            .Include(r => r.OtherDepartments)
            .Include(r => r.Reporters)
            .Include(r => r.ManualNotifications)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);

        var results = new List<IncidentReportDto>();
        foreach (var report in reports)
            results.Add(await MapToDtoAsync(report, cancellationToken));
        return results;
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
                ClosedCases = g.Count(x => x.ReportStatus == "Closed"),
                MedicationErrors = g.Count(x => x.ReportType == "Medication Error"),
                AdrReactions = g.Count(x => x.ReportType == "ADR"),
                PendingReview = g.Count(x => x.ReportStatus == "Pending"),
                UnderReview = g.Count(x => x.ReportStatus == "UnderReview"),
                OverdueCount = g.Count(x =>
                    (x.ReportStatus == "Pending" || x.ReportStatus == "UnderReview")
                    && x.SubmittedAt < DateTime.UtcNow.AddHours(-48)),
                UnassignedCount = g.Count(x => x.ReportStatus == "UnderReview" && (x.Review == null || x.Review.ActionOwnerUserId == null)),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return summary ?? new IncidentReportSummaryDto();
    }

    // Powers the Reports Hub's scope tabs. "assigned"/"submitted" are deliberately
    // narrow (reviewer-of-record / submitter only) so each tab means exactly what it
    // says; "all" (and no scope, for backward compatibility) is the broad view —
    // Admin sees every report, everyone else sees their own plus anything they were
    // notified about or are the assigned reviewer for.
    private IQueryable<IncidentReport> ApplyScopeFilter(IQueryable<IncidentReport> query, string? scope)
    {
        var userId = _currentUser.UserId;

        switch (scope?.Trim().ToLowerInvariant())
        {
            case "assigned":
                query = query.Where(r => r.Review != null && r.Review.ReviewerUserId == userId);
                break;

            case "submitted":
                query = query.Where(r => r.SubmittedByUserId == userId);
                break;

            default:
                if (_currentUser.Role != "Admin")
                {
                    query = query.Where(r =>
                        r.SubmittedByUserId == userId ||
                        (r.Review != null && r.Review.ReviewerUserId == userId) ||
                        r.Notifications.Any(n => n.RecipientUserId == userId));
                }
                break;
        }

        return query;
    }

    // Shared by GetListAsync/GetSummaryAsync so both stay in sync.
    private IQueryable<IncidentReport> ApplyFilters(IQueryable<IncidentReport> query, IncidentReportListRequest request)
    {
        query = ApplyScopeFilter(query, request.Scope);

        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            var start = request.StartDate.Value.Date;
            var end = request.EndDate.Value.Date;
            query = query.Where(r => r.SubmittedAt.Date >= start && r.SubmittedAt.Date <= end);
        }

        if (!string.IsNullOrWhiteSpace(request.FacilityUnit) && request.FacilityUnit != "All Units")
            query = query.Where(r => r.IncidentLocation == request.FacilityUnit);

        if (!string.IsNullOrWhiteSpace(request.ReportType))
            query = query.Where(r => r.ReportType == request.ReportType);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(r => r.ReportStatus == request.Status);

        if (request.Severity == "Harm")
            query = query.Where(r => r.HarmLevelCode == "E" || r.HarmLevelCode == "F" || r.HarmLevelCode == "G" || r.HarmLevelCode == "H" || r.HarmLevelCode == "I");
        else if (request.Severity == "NoHarm")
            query = query.Where(r => r.HarmLevelCode == "A" || r.HarmLevelCode == "B" || r.HarmLevelCode == "C" || r.HarmLevelCode == "D");

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

    private async Task<ReportType> ValidateRequestAsync(SubmitIncidentReportRequest request, CancellationToken cancellationToken)
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

        // Report type — resolved first, everything else branches on it.
        var reportType = await _db.ReportTypes
            .FirstOrDefaultAsync(x => x.Id == request.ReportTypeId && x.IsActive, cancellationToken);

        if (reportType == null)
            throw new ValidationException("Invalid report type.");

        if (reportType.Code != MedicationErrorCode && reportType.Code != AdrCode)
            throw new ValidationException("Unsupported report type.");

        var isMedicationError = reportType.Code == MedicationErrorCode;

        // Step 2 - Medications
        if (request.Medications.Count == 0)
            throw new ValidationException("At least one medication is required.");

        foreach (var medication in request.Medications)
        {
            if (string.IsNullOrWhiteSpace(medication.MedicationName))
                throw new ValidationException("Medication name is required.");

            if (isMedicationError)
            {
                if (!medication.DoseValue.HasValue || medication.DoseValue <= 0)
                    throw new ValidationException("Medication dose is required.");

                if (!medication.DoseUnitId.HasValue)
                    throw new ValidationException("Dose unit is required.");

                if (!medication.RouteId.HasValue)
                    throw new ValidationException("Route is required.");

                if (!medication.MedicationGivenAt.HasValue)
                    throw new ValidationException("Medication date/time is required.");
            }

            if (medication.DoseUnitId.HasValue &&
                !await _db.DoseUnits.AnyAsync(x => x.Id == medication.DoseUnitId && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid dose unit.");

            if (medication.RouteId.HasValue &&
                !await _db.Routes.AnyAsync(x => x.Id == medication.RouteId && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid route.");

            if (medication.FrequencyId.HasValue &&
                !await _db.Frequencies.AnyAsync(x => x.Id == medication.FrequencyId && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid frequency.");

            if (medication.FormulationId.HasValue &&
                !await _db.Formulations.AnyAsync(x => x.Id == medication.FormulationId && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid formulation.");

            if (medication.TherapyStartAt.HasValue && medication.TherapyStopAt.HasValue &&
                medication.TherapyStopAt < medication.TherapyStartAt)
                throw new ValidationException("Therapy stop date cannot be before therapy start date.");
        }

        // Step 3 - Incident / harm (report-type specific)
        if (isMedicationError)
        {
            if (!request.HarmLevelId.HasValue)
                throw new ValidationException("NCC MERP harm level is required.");

            if (!await _db.HarmLevels.AnyAsync(x => x.Id == request.HarmLevelId.Value && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid harm level.");

            if (!request.ErrorCategoryId.HasValue)
                throw new ValidationException("Error category is required.");

            if (!request.StageOfProcessId.HasValue)
                throw new ValidationException("Stage of process is required.");
        }
        else
        {
            if (!request.AdrSeverityId.HasValue)
                throw new ValidationException("ADR severity is required.");

            if (!await _db.AdrSeverities.AnyAsync(x => x.Id == request.AdrSeverityId.Value && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid ADR severity.");

            if (!request.SuspectedCausalityId.HasValue)
                throw new ValidationException("WHO-UMC suspected causality is required.");

            if (!await _db.SuspectedCausalities.AnyAsync(x => x.Id == request.SuspectedCausalityId.Value && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid suspected causality.");

            if (string.IsNullOrWhiteSpace(request.AdrReactionDescription))
                throw new ValidationException("ADR reaction description is required.");
        }

        if (request.ErrorCategoryId.HasValue &&
            !await _db.ErrorCategories.AnyAsync(x => x.Id == request.ErrorCategoryId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid error category.");

        if (request.StageOfProcessId.HasValue &&
            !await _db.StageOfProcesses.AnyAsync(x => x.Id == request.StageOfProcessId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid stage of process.");

        if (request.IncidentOccurredAt == default)
            throw new ValidationException("Incident date and time is required.");

        if (!await _db.UnitDepartments.AnyAsync(x => x.Id == request.IncidentUnitId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid incident location / unit.");

        if (request.SectionId.HasValue &&
            !await _db.Sections.AnyAsync(x => x.Id == request.SectionId && x.UnitDepartmentId == request.IncidentUnitId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid section for the selected unit / department.");

        if (request.ReportedIncidentSeverityId.HasValue &&
            !await _db.ReportedIncidentSeverities.AnyAsync(x => x.Id == request.ReportedIncidentSeverityId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid reported incident severity.");

        if (!isMedicationError && request.ReactionStartAt.HasValue && request.ReactionStoppedAt.HasValue &&
            request.ReactionStoppedAt < request.ReactionStartAt)
            throw new ValidationException("Reaction stop time cannot be before reaction start time.");

        if (string.IsNullOrWhiteSpace(request.IncidentNarrative))
            throw new ValidationException("Incident narrative is required.");

        // Step 4 - Outcome / reporter / visit
        if (!await _db.PatientOutcomes.AnyAsync(x => x.Id == request.PatientOutcomeId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid patient outcome.");

        if (!await _db.VisitTypes.AnyAsync(x => x.Id == request.VisitTypeId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid visit type.");

        if (request.ReportingSourceId.HasValue &&
            !await _db.ReportingSources.AnyAsync(x => x.Id == request.ReportingSourceId && x.IsActive, cancellationToken))
            throw new ValidationException("Invalid reporting source.");

        // Multi-select lookups
        var contributingFactorIds = request.ContributingFactorIds.Distinct().ToList();
        if (contributingFactorIds.Count > 0)
        {
            var validCount = await _db.ContributingFactors
                .CountAsync(x => contributingFactorIds.Contains(x.Id) && x.IsActive, cancellationToken);
            if (validCount != contributingFactorIds.Count)
                throw new ValidationException("One or more contributing factors are invalid.");
        }

        if (isMedicationError)
        {
            if (request.SeriousnessCriterionIds.Count > 0)
                throw new ValidationException("Seriousness criteria only apply to ADR reports.");
            if (request.ConcomitantMedications.Count > 0)
                throw new ValidationException("Concomitant medications only apply to ADR reports.");
        }
        else
        {
            var seriousnessCriterionIds = request.SeriousnessCriterionIds.Distinct().ToList();
            if (seriousnessCriterionIds.Count > 0)
            {
                var validCount = await _db.SeriousnessCriteria
                    .CountAsync(x => seriousnessCriterionIds.Contains(x.Id) && x.IsActive, cancellationToken);
                if (validCount != seriousnessCriterionIds.Count)
                    throw new ValidationException("One or more seriousness criteria are invalid.");
            }

            foreach (var item in request.ConcomitantMedications)
            {
                var careSetting = item.CareSettingCode?.Trim().ToUpperInvariant();
                if (careSetting is not ("INPATIENT" or "OUTPATIENT"))
                    throw new ValidationException("Invalid concomitant medication care setting.");

                if (string.IsNullOrWhiteSpace(item.MedicationText))
                    throw new ValidationException("Concomitant medication is required.");
            }
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

        // Other Healthcare Professionals
        foreach (var professional in request.OtherHealthcareProfessionals)
        {
            if (string.IsNullOrWhiteSpace(professional.Name))
                throw new ValidationException("Other healthcare professional name is required.");

            if (!await _db.Professions.AnyAsync(x => x.Id == professional.ProfessionId && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid profession for other healthcare professional.");

            if (!await _db.Positions.AnyAsync(
                x => x.Id == professional.PositionId && x.ProfessionId == professional.ProfessionId && x.IsActive, cancellationToken))
                throw new ValidationException("Invalid position for other healthcare professional.");
        }

        return reportType;
    }

    private async Task<IncidentReport> BuildIncidentReport(SubmitIncidentReportRequest request, ReportType reportType, CancellationToken cancellationToken)
    {
        var isMedicationError = reportType.Code == MedicationErrorCode;

        var harmLevel = isMedicationError && request.HarmLevelId.HasValue
            ? await _db.HarmLevels.FirstOrDefaultAsync(x => x.Id == request.HarmLevelId.Value, cancellationToken)
            : null;

        var causality = !isMedicationError && request.SuspectedCausalityId.HasValue
            ? await _db.SuspectedCausalities.FirstOrDefaultAsync(x => x.Id == request.SuspectedCausalityId.Value, cancellationToken)
            : null;

        var unit = await _db.UnitDepartments.FirstAsync(x => x.Id == request.IncidentUnitId, cancellationToken);

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
            PatientDateOfBirth = request.PatientDateOfBirth,
            PatientSex = request.PatientSex.Trim(),
            PatientWeightKg = request.PatientWeightKg,
            RelevantMedicalHistory = request.RelevantMedicalHistory?.Trim(),
            AdmissionDate = request.AdmissionDate,
            CurrentDiagnosis = request.CurrentDiagnosis?.Trim(),

            // Lookup ids
            ReportTypeId = request.ReportTypeId,
            HarmLevelId = isMedicationError ? request.HarmLevelId : null,
            AdrSeverityId = !isMedicationError ? request.AdrSeverityId : null,
            SuspectedCausalityId = !isMedicationError ? request.SuspectedCausalityId : null,
            ErrorCategoryId = isMedicationError ? request.ErrorCategoryId : null,
            StageOfProcessId = isMedicationError ? request.StageOfProcessId : null,
            AdrReactionDescription = !isMedicationError ? request.AdrReactionDescription?.Trim() : null,
            AdrAdditionalInformation = !isMedicationError ? request.AdrAdditionalInformation?.Trim() : null,
            ReactionStartAt = !isMedicationError ? request.ReactionStartAt : null,
            ReactionStoppedAt = !isMedicationError ? request.ReactionStoppedAt : null,
            IncidentUnitId = request.IncidentUnitId,
            SectionId = request.SectionId,
            VisitTypeId = request.VisitTypeId,
            ReportingSourceId = request.ReportingSourceId,
            ReportedIncidentSeverityId = request.ReportedIncidentSeverityId,
            IsResearchStudyRelated = request.IsResearchStudyRelated,

            // Legacy dual-write — AlertRuleEvaluationService/DashboardService/Report Hub
            // filters still read these string snapshots directly (see IncidentReport comment).
            ReportType = isMedicationError ? "Medication Error" : "ADR",
            HarmLevelCode = isMedicationError ? harmLevel?.Code : null,
            SuspectedCausality = !isMedicationError ? causality?.Name : null,
            IncidentLocation = unit.Name,

            IncidentOccurredAt = request.IncidentOccurredAt,
            IncidentNarrative = request.IncidentNarrative.Trim(),
            PatientOutcomeId = request.PatientOutcomeId,

            // Step 4
            ProfessionId = request.ProfessionId,
            PositionId = request.PositionId,
            EnteredByTitle = request.EnteredByTitle?.Trim(),
            ReporterPhoneNumber = request.ReporterPhoneNumber?.Trim(),

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
                GenericName = medication.GenericName?.Trim(),
                DrugClass = medication.DrugClass?.Trim(),
                DoseValue = medication.DoseValue,
                DoseUnitId = medication.DoseUnitId,
                RouteId = medication.RouteId,
                FrequencyId = medication.FrequencyId,
                FormulationId = medication.FormulationId,
                MedicationGivenAt = medication.MedicationGivenAt,
                Manufacturer = medication.Manufacturer?.Trim(),
                BatchLotNumber = medication.BatchLotNumber?.Trim(),
                TherapyStartAt = medication.TherapyStartAt,
                TherapyStopAt = medication.TherapyStopAt,
                ExpiryDate = medication.ExpiryDate,
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddConcomitantMedications(int incidentReportId, List<ConcomitantMedicationRequest> medications)
    {
        foreach (var medication in medications)
        {
            _db.IncidentReportConcomitantMedications.Add(new IncidentReportConcomitantMedication
            {
                IncidentReportId = incidentReportId,
                CareSettingCode = medication.CareSettingCode.Trim().ToUpperInvariant(),
                MedicationText = medication.MedicationText.Trim(),
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddHealthcareProfessionals(int incidentReportId, List<HealthcareProfessionalRequest> professionals)
    {
        foreach (var professional in professionals)
        {
            _db.IncidentReportHealthcareProfessionals.Add(new IncidentReportHealthcareProfessional
            {
                IncidentReportId = incidentReportId,
                Name = professional.Name.Trim(),
                ProfessionId = professional.ProfessionId,
                PositionId = professional.PositionId,
                ContactNumber = professional.ContactNumber?.Trim(),
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddOtherDepartments(int incidentReportId, List<int> unitDepartmentIds)
    {
        foreach (var unitDepartmentId in unitDepartmentIds.Distinct())
        {
            _db.IncidentReportOtherDepartments.Add(new IncidentReportOtherDepartment
            {
                IncidentReportId = incidentReportId,
                UnitDepartmentId = unitDepartmentId
            });
        }
    }

    private void AddWitnesses(int incidentReportId, List<WitnessRequest> witnesses)
    {
        foreach (var witness in witnesses)
        {
            _db.IncidentReportWitnesses.Add(new IncidentReportWitness
            {
                IncidentReportId = incidentReportId,
                Name = witness.Name.Trim(),
                Address = witness.Address?.Trim(),
                PhoneNumber = witness.PhoneNumber?.Trim(),
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddReporters(int incidentReportId, List<ReporterRequest> reporters)
    {
        foreach (var reporter in reporters)
        {
            _db.IncidentReportReporters.Add(new IncidentReportReporter
            {
                IncidentReportId = incidentReportId,
                Name = reporter.Name.Trim(),
                ReportedDate = reporter.ReportedDate,
                ProfessionId = reporter.ProfessionId,
                CreatedBy = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddManualNotifications(int incidentReportId, List<ManualNotificationRequest> notifications)
    {
        foreach (var notification in notifications)
        {
            _db.IncidentReportManualNotifications.Add(new IncidentReportManualNotification
            {
                IncidentReportId = incidentReportId,
                TypeOfPersonNotified = notification.TypeOfPersonNotified.Trim(),
                Name = notification.Name.Trim(),
                NotifiedAt = notification.NotifiedAt,
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

    private async Task<IncidentReportDto> MapToDtoAsync(IncidentReport r, CancellationToken cancellationToken)
    {
        // Small, cheap point lookups for the readable *Name fields — these tables
        // are tiny (single-digit to low-hundreds rows) so no caching needed.
        var reportTypeName = r.ReportTypeId.HasValue
            ? await _db.ReportTypes.Where(x => x.Id == r.ReportTypeId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var harmLevelName = r.HarmLevelId.HasValue
            ? await _db.HarmLevels.Where(x => x.Id == r.HarmLevelId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var adrSeverityName = r.AdrSeverityId.HasValue
            ? await _db.AdrSeverities.Where(x => x.Id == r.AdrSeverityId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var causalityName = r.SuspectedCausalityId.HasValue
            ? await _db.SuspectedCausalities.Where(x => x.Id == r.SuspectedCausalityId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var unitName = r.IncidentUnitId.HasValue
            ? await _db.UnitDepartments.Where(x => x.Id == r.IncidentUnitId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var visitTypeName = r.VisitTypeId.HasValue
            ? await _db.VisitTypes.Where(x => x.Id == r.VisitTypeId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var reportingSourceName = r.ReportingSourceId.HasValue
            ? await _db.ReportingSources.Where(x => x.Id == r.ReportingSourceId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var sectionName = r.SectionId.HasValue
            ? await _db.Sections.Where(x => x.Id == r.SectionId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var reportedIncidentSeverityName = r.ReportedIncidentSeverityId.HasValue
            ? await _db.ReportedIncidentSeverities.Where(x => x.Id == r.ReportedIncidentSeverityId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        var otherDepartmentIds = r.OtherDepartments.Select(d => d.UnitDepartmentId).ToList();
        var otherDepartmentNames = otherDepartmentIds.Count == 0
            ? []
            : await _db.UnitDepartments.Where(x => otherDepartmentIds.Contains(x.Id)).Select(x => x.Name).ToListAsync(cancellationToken);

        var reporterProfessionIds = r.Reporters.Where(rep => rep.ProfessionId.HasValue).Select(rep => rep.ProfessionId!.Value).Distinct().ToList();
        var reporterProfessionNames = reporterProfessionIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Professions.Where(x => reporterProfessionIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        return new IncidentReportDto
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
            PatientDateOfBirth = r.PatientDateOfBirth,
            PatientSex = r.PatientSex,
            PatientWeightKg = r.PatientWeightKg,
            RelevantMedicalHistory = r.RelevantMedicalHistory,
            AdmissionDate = r.AdmissionDate,
            CurrentDiagnosis = r.CurrentDiagnosis,
            KnownAllergyIds = r.AllergyLinks.Select(a => a.AllergyId).ToList(),
            CurrentMedicationIds = r.CurrentMedicationLinks.Select(m => m.CurrentMedicationId).ToList(),
            Medications = r.Medications.Select(m => new IncidentMedicationDto
            {
                Id = m.Id,
                MedicationName = m.MedicationName,
                GenericName = m.GenericName,
                DrugClass = m.DrugClass,
                DoseValue = m.DoseValue,
                DoseUnitId = m.DoseUnitId,
                RouteId = m.RouteId,
                FrequencyId = m.FrequencyId,
                FormulationId = m.FormulationId,
                MedicationGivenAt = m.MedicationGivenAt,
                Manufacturer = m.Manufacturer,
                BatchLotNumber = m.BatchLotNumber,
                TherapyStartAt = m.TherapyStartAt,
                TherapyStopAt = m.TherapyStopAt,
                ExpiryDate = m.ExpiryDate
            }).ToList(),
            ConcomitantMedications = r.ConcomitantMedications.Select(c => new ConcomitantMedicationDto
            {
                Id = c.Id,
                CareSettingCode = c.CareSettingCode,
                MedicationText = c.MedicationText
            }).ToList(),
            ReportType = r.ReportType,
            HarmLevelCode = r.HarmLevelCode,
            SuspectedCausality = r.SuspectedCausality,
            IncidentLocation = r.IncidentLocation,
            ReportTypeId = r.ReportTypeId ?? 0,
            HarmLevelId = r.HarmLevelId,
            AdrSeverityId = r.AdrSeverityId,
            SuspectedCausalityId = r.SuspectedCausalityId,
            IncidentUnitId = r.IncidentUnitId ?? 0,
            SectionId = r.SectionId,
            VisitTypeId = r.VisitTypeId ?? 0,
            ReportingSourceId = r.ReportingSourceId,
            ReportedIncidentSeverityId = r.ReportedIncidentSeverityId,
            ReportTypeName = reportTypeName,
            HarmLevelName = harmLevelName,
            AdrSeverityName = adrSeverityName,
            SuspectedCausalityName = causalityName,
            IncidentUnitName = unitName,
            SectionName = sectionName,
            VisitTypeName = visitTypeName,
            ReportingSourceName = reportingSourceName,
            ReportedIncidentSeverityName = reportedIncidentSeverityName,
            OtherDepartmentIds = otherDepartmentIds,
            OtherDepartmentNames = otherDepartmentNames,
            Witnesses = r.Witnesses.Select(w => new WitnessDto
            {
                Id = w.Id,
                Name = w.Name,
                Address = w.Address,
                PhoneNumber = w.PhoneNumber
            }).ToList(),
            ErrorCategoryId = r.ErrorCategoryId,
            StageOfProcessId = r.StageOfProcessId,
            AdrReactionDescription = r.AdrReactionDescription,
            AdrAdditionalInformation = r.AdrAdditionalInformation,
            ReactionStartAt = r.ReactionStartAt,
            ReactionStoppedAt = r.ReactionStoppedAt,
            IsResearchStudyRelated = r.IsResearchStudyRelated,
            IncidentOccurredAt = r.IncidentOccurredAt,
            IncidentNarrative = r.IncidentNarrative,
            ContributingFactorIds = r.ContributingFactors.Select(f => f.ContributingFactorId).ToList(),
            SeriousnessCriterionIds = r.SeriousnessCriteria.Select(c => c.SeriousnessCriterionId).ToList(),
            ProfessionId = r.ProfessionId,
            PositionId = r.PositionId,
            EnteredByTitle = r.EnteredByTitle,
            ReporterPhoneNumber = r.ReporterPhoneNumber,
            OtherHealthcareProfessionals = r.HealthcareProfessionals.Select(p => new HealthcareProfessionalDto
            {
                Id = p.Id,
                Name = p.Name,
                ProfessionId = p.ProfessionId,
                PositionId = p.PositionId,
                ContactNumber = p.ContactNumber
            }).ToList(),
            Reporters = r.Reporters.Select(rep => new ReporterDto
            {
                Id = rep.Id,
                Name = rep.Name,
                ReportedDate = rep.ReportedDate,
                ProfessionId = rep.ProfessionId,
                ProfessionName = rep.ProfessionId.HasValue && reporterProfessionNames.TryGetValue(rep.ProfessionId.Value, out var pn) ? pn : null
            }).ToList(),
            ManualNotifications = r.ManualNotifications.Select(n => new ManualNotificationDto
            {
                Id = n.Id,
                TypeOfPersonNotified = n.TypeOfPersonNotified,
                Name = n.Name,
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
                UploadedByUserId = a.UploadedByUserId,
                Category = a.Category,
                Description = a.Description
            }).ToList()
        };
    }
}
