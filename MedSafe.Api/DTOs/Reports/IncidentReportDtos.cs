using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public sealed class SubmitIncidentReportRequest
{
    // =====================================================
    // STEP 1 - PATIENT / DEMOGRAPHICS
    // =====================================================
    public string PatientName { get; set; } = string.Empty;
    public string PatientRef { get; set; } = string.Empty;
    public short PatientAge { get; set; }
    public DateTime? PatientDateOfBirth { get; set; }
    public string PatientSex { get; set; } = string.Empty;
    public decimal? PatientWeightKg { get; set; }
    public string? RelevantMedicalHistory { get; set; }
    public DateTime? AdmissionDate { get; set; }
    public string? CurrentDiagnosis { get; set; }
    public List<int> KnownAllergyIds { get; set; } = [];
    public List<int> CurrentMedicationIds { get; set; } = [];

    // =====================================================
    // STEP 2 - MEDICATIONS
    // =====================================================
    public List<IncidentMedicationRequest> Medications { get; set; } = [];

    // ADR only
    public List<ConcomitantMedicationRequest> ConcomitantMedications { get; set; } = [];

    // =====================================================
    // STEP 3 - INCIDENT / ADR ASSESSMENT
    // =====================================================
    public int ReportTypeId { get; set; }

    // Medication Error only
    public int? HarmLevelId { get; set; }
    public int? ErrorCategoryId { get; set; }
    public int? StageOfProcessId { get; set; }

    // ADR only
    public int? AdrSeverityId { get; set; }
    public int? SuspectedCausalityId { get; set; }
    public string? AdrReactionDescription { get; set; }
    public string? AdrAdditionalInformation { get; set; }
    public List<int> SeriousnessCriterionIds { get; set; } = [];
    public DateTime? ReactionStartAt { get; set; }
    public DateTime? ReactionStoppedAt { get; set; }

    // Common
    public List<int> ContributingFactorIds { get; set; } = [];
    public DateTime IncidentOccurredAt { get; set; }
    public int IncidentUnitId { get; set; }
    public int? SectionId { get; set; }
    public int PatientOutcomeId { get; set; }
    public string IncidentNarrative { get; set; } = string.Empty;
    public int? ReportedIncidentSeverityId { get; set; }
    public bool? IsResearchStudyRelated { get; set; }
    public List<int> OtherDepartmentIds { get; set; } = [];
    public List<WitnessRequest> Witnesses { get; set; } = [];

    // =====================================================
    // STEP 4 - REPORTER / VISIT
    // =====================================================
    public int ProfessionId { get; set; }
    public int PositionId { get; set; }
    public string? EnteredByTitle { get; set; }
    public string? ReporterPhoneNumber { get; set; }
    public int VisitTypeId { get; set; }
    public int? ReportingSourceId { get; set; }
    public List<HealthcareProfessionalRequest> OtherHealthcareProfessionals { get; set; } = [];
    public List<ReporterRequest> Reporters { get; set; } = [];
    public List<ManualNotificationRequest> ManualNotifications { get; set; } = [];

    // =====================================================
    // STEP 5 - OUTCOME
    // =====================================================
    public string? ImmediateActionTaken { get; set; }
    public string? PatientOutcomeDetails { get; set; }

    // Intentionally absent: Id, IncidentReportNumber, PatientReferenceToken, SubmittedAt,
    // SubmittedByUserId, SubmittedByRole, ReportStatus — the backend generates these.
}

public sealed class IncidentMedicationRequest
{
    public string MedicationName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? DrugClass { get; set; }

    // Nullable — required for Medication Error, optional for ADR (validated
    // conditionally in the service based on report type).
    public decimal? DoseValue { get; set; }
    public int? DoseUnitId { get; set; }
    public int? RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
    public DateTime? MedicationGivenAt { get; set; }

    // ADR / client fields
    public string? Manufacturer { get; set; }
    public string? BatchLotNumber { get; set; }
    public DateTime? TherapyStartAt { get; set; }
    public DateTime? TherapyStopAt { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

public sealed class ConcomitantMedicationRequest
{
    public string CareSettingCode { get; set; } = string.Empty; // INPATIENT | OUTPATIENT
    public string MedicationText { get; set; } = string.Empty;
}

public sealed class HealthcareProfessionalRequest
{
    public string Name { get; set; } = string.Empty;
    public int ProfessionId { get; set; }
    public int PositionId { get; set; }
    public string? ContactNumber { get; set; }
}

public sealed class WitnessRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
}

public sealed class ReporterRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime ReportedDate { get; set; }
    public int? ProfessionId { get; set; }
}

public sealed class ManualNotificationRequest
{
    public string TypeOfPersonNotified { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime NotifiedAt { get; set; }
}

public sealed class SubmitIncidentReportResponse
{
    public int Id { get; set; }
    public string IncidentReportNumber { get; set; } = null!;
    public string PatientReferenceToken { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = null!;
}

public sealed class IncidentReportDto
{
    public int Id { get; set; }
    public string IncidentReportNumber { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public int SubmittedByUserId { get; set; }
    public string SubmittedByRole { get; set; } = null!;
    public string ReportStatus { get; set; } = null!;

    public string PatientReferenceToken { get; set; } = null!;
    public string? PatientReference { get; set; }
    public string? PatientName { get; set; }
    public short PatientAge { get; set; }
    public DateTime? PatientDateOfBirth { get; set; }
    public string PatientSex { get; set; } = null!;
    public decimal? PatientWeightKg { get; set; }
    public string? RelevantMedicalHistory { get; set; }
    public DateTime? AdmissionDate { get; set; }
    public string? CurrentDiagnosis { get; set; }
    public List<int> KnownAllergyIds { get; set; } = [];
    public List<int> CurrentMedicationIds { get; set; } = [];

    public List<IncidentMedicationDto> Medications { get; set; } = [];
    public List<ConcomitantMedicationDto> ConcomitantMedications { get; set; } = [];

    // Legacy string snapshots (still populated, see IncidentReport entity comment)
    public string ReportType { get; set; } = null!;
    public string? HarmLevelCode { get; set; }
    public string? SuspectedCausality { get; set; }
    public string IncidentLocation { get; set; } = null!;

    // Lookup ids (new Medication Error / ADR schema)
    public int ReportTypeId { get; set; }
    public int? HarmLevelId { get; set; }
    public int? AdrSeverityId { get; set; }
    public int? SuspectedCausalityId { get; set; }
    public int IncidentUnitId { get; set; }
    public int? SectionId { get; set; }
    public int VisitTypeId { get; set; }
    public int? ReportingSourceId { get; set; }
    public int? ReportedIncidentSeverityId { get; set; }

    // Readable lookup names — saves the frontend a second round trip for the detail view.
    public string? ReportTypeName { get; set; }
    public string? HarmLevelName { get; set; }
    public string? AdrSeverityName { get; set; }
    public string? SuspectedCausalityName { get; set; }
    public string? IncidentUnitName { get; set; }
    public string? SectionName { get; set; }
    public string? VisitTypeName { get; set; }
    public string? ReportingSourceName { get; set; }
    public string? ReportedIncidentSeverityName { get; set; }
    public List<int> OtherDepartmentIds { get; set; } = [];
    public List<string> OtherDepartmentNames { get; set; } = [];
    public List<WitnessDto> Witnesses { get; set; } = [];

    public int? ErrorCategoryId { get; set; }
    public int? StageOfProcessId { get; set; }
    public string? AdrReactionDescription { get; set; }
    public string? AdrAdditionalInformation { get; set; }
    public DateTime? ReactionStartAt { get; set; }
    public DateTime? ReactionStoppedAt { get; set; }
    public bool? IsResearchStudyRelated { get; set; }
    public DateTime IncidentOccurredAt { get; set; }
    public string IncidentNarrative { get; set; } = null!;
    public List<int> ContributingFactorIds { get; set; } = [];
    public List<int> SeriousnessCriterionIds { get; set; } = [];

    public int? ProfessionId { get; set; }
    public int? PositionId { get; set; }
    public string? EnteredByTitle { get; set; }
    public string? ReporterPhoneNumber { get; set; }
    public List<HealthcareProfessionalDto> OtherHealthcareProfessionals { get; set; } = [];
    public List<ReporterDto> Reporters { get; set; } = [];
    public List<ManualNotificationDto> ManualNotifications { get; set; } = [];

    public string? ImmediateActionTaken { get; set; }
    public int PatientOutcomeId { get; set; }
    public string? PatientOutcomeDetails { get; set; }

    public List<IncidentReportAttachmentDto> Attachments { get; set; } = [];
}

public sealed class IncidentMedicationDto
{
    public int Id { get; set; }
    public string MedicationName { get; set; } = null!;
    public string? GenericName { get; set; }
    public string? DrugClass { get; set; }
    public decimal? DoseValue { get; set; }
    public int? DoseUnitId { get; set; }
    public int? RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
    public DateTime? MedicationGivenAt { get; set; }
    public string? Manufacturer { get; set; }
    public string? BatchLotNumber { get; set; }
    public DateTime? TherapyStartAt { get; set; }
    public DateTime? TherapyStopAt { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

public sealed class ConcomitantMedicationDto
{
    public int Id { get; set; }
    public string CareSettingCode { get; set; } = null!;
    public string MedicationText { get; set; } = null!;
}

public sealed class HealthcareProfessionalDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int ProfessionId { get; set; }
    public int PositionId { get; set; }
    public string? ContactNumber { get; set; }
}

public sealed class IncidentReportAttachmentDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public int UploadedByUserId { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
}

public sealed class WitnessDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
}

public sealed class ReporterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime ReportedDate { get; set; }
    public int? ProfessionId { get; set; }
    public string? ProfessionName { get; set; }
}

public sealed class ManualNotificationDto
{
    public int Id { get; set; }
    public string TypeOfPersonNotified { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime NotifiedAt { get; set; }
}

public sealed class IncidentReportSummaryDto
{
    public int TotalReports { get; set; }
    public int AwaitingReview { get; set; }
    public int HarmEvents { get; set; }
    public int ClosedCases { get; set; }
    public int MedicationErrors { get; set; }
    public int AdrReactions { get; set; }
    // AwaitingReview split into its two statuses individually.
    public int PendingReview { get; set; }
    public int UnderReview { get; set; }
    // Pending/UnderReview reports still unreviewed 48h+ after submission — same
    // threshold as the Dashboard's Overdue >48h KPI.
    public int OverdueCount { get; set; }
    // UnderReview reports with no Action Owner set yet (IncidentReportReview.ActionOwnerUserId
    // is null) — review has started but nobody owns the follow-up actions.
    public int UnassignedCount { get; set; }
}

// Same optional filter set as DashboardSummaryRequest, reused here so the Reports
// Hub can filter server-side the same way the Dashboard does. Every field is
// optional — an empty/default request (POST {}) returns exactly what the old
// parameterless GET returned: every report the caller's role can see, no date or
// classification restriction (unlike Dashboard, there is no "previous week"
// fallback here — omitting the dates means no date filter at all).
public sealed class IncidentReportListRequest
{
    // "all" (default/null) | "assigned" (I'm the reviewer) | "submitted" (I submitted it) —
    // drives the Reports Hub's scope tabs. See IncidentReportService.ApplyScopeFilter.
    public string? Scope { get; set; }
    public string? FacilityUnit { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    // Raw values, matching what's actually stored: ReportType is "Medication Error" |
    // "Near Miss" | "ADR"; Status is "Pending" | "UnderReview" | "Closed";
    // Severity is "Harm" (NCC MERP E-I) | "NoHarm" (A-D) — the frontend's Reports Hub
    // quick-filter dropdowns convert their local values to these before sending.
    public string? ReportType { get; set; }
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public string? MedicationName { get; set; }
    public int? ErrorCategoryId { get; set; }
    public int? StageOfProcessId { get; set; }
    public int? PatientOutcomeId { get; set; }
    public string? SuspectedCausality { get; set; }
    public int? ContributingFactorId { get; set; }
    public int? SeriousnessCriterionId { get; set; }
}
