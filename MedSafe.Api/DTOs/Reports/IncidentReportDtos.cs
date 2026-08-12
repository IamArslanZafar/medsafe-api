using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public sealed class SubmitIncidentReportRequest
{
    // STEP 1
    public string PatientName { get; set; } = string.Empty;
    public string PatientRef { get; set; } = string.Empty;
    public short PatientAge { get; set; }
    public string PatientSex { get; set; } = string.Empty;
    public decimal? PatientWeightKg { get; set; }
    public List<int> KnownAllergyIds { get; set; } = [];
    public List<int> CurrentMedicationIds { get; set; } = [];

    // STEP 2
    public List<IncidentMedicationRequest> Medications { get; set; } = [];

    // STEP 3
    public string ReportType { get; set; } = string.Empty;
    public int? ErrorCategoryId { get; set; }
    public int? StageOfProcessId { get; set; }
    public string? AdrReactionDescription { get; set; }
    public string? SuspectedCausality { get; set; }
    public string HarmLevelCode { get; set; } = string.Empty;
    public DateTime IncidentOccurredAt { get; set; }
    public string IncidentLocation { get; set; } = string.Empty;
    public string IncidentNarrative { get; set; } = string.Empty;
    public List<int> ContributingFactorIds { get; set; } = [];
    public List<int> SeriousnessCriterionIds { get; set; } = [];
    public int PatientOutcomeId { get; set; }

    // STEP 4
    public int ProfessionId { get; set; }
    public int PositionId { get; set; }
    public List<ManualNotificationRequest> Notifications { get; set; } = [];

    // STEP 5
    public string? ImmediateActionTaken { get; set; }
    public string? PatientOutcomeDetails { get; set; }

    // Intentionally absent: Id, IncidentReportNumber, PatientReferenceToken, SubmittedAt,
    // SubmittedByUserId, SubmittedByRole, ReportStatus — the backend generates these.
}

public sealed class IncidentMedicationRequest
{
    public string MedicationName { get; set; } = string.Empty;
    public decimal DoseValue { get; set; }
    public int DoseUnitId { get; set; }
    public int RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
    public DateTime MedicationGivenAt { get; set; }
}

public sealed class ManualNotificationRequest
{
    public int NotificationTypeId { get; set; }
    public string PersonName { get; set; } = string.Empty;
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
    public string PatientSex { get; set; } = null!;
    public decimal? PatientWeightKg { get; set; }
    public List<int> KnownAllergyIds { get; set; } = [];
    public List<int> CurrentMedicationIds { get; set; } = [];

    public List<IncidentMedicationDto> Medications { get; set; } = [];

    public string ReportType { get; set; } = null!;
    public int? ErrorCategoryId { get; set; }
    public int? StageOfProcessId { get; set; }
    public string? AdrReactionDescription { get; set; }
    public string? SuspectedCausality { get; set; }
    public string HarmLevelCode { get; set; } = null!;
    public DateTime IncidentOccurredAt { get; set; }
    public string IncidentLocation { get; set; } = null!;
    public string IncidentNarrative { get; set; } = null!;
    public List<int> ContributingFactorIds { get; set; } = [];
    public List<int> SeriousnessCriterionIds { get; set; } = [];

    public int? ProfessionId { get; set; }
    public int? PositionId { get; set; }
    public List<ManualNotificationDto> Notifications { get; set; } = [];

    public string? ImmediateActionTaken { get; set; }
    public int PatientOutcomeId { get; set; }
    public string? PatientOutcomeDetails { get; set; }

    public List<IncidentReportAttachmentDto> Attachments { get; set; } = [];
}

public sealed class IncidentMedicationDto
{
    public int Id { get; set; }
    public string MedicationName { get; set; } = null!;
    public decimal DoseValue { get; set; }
    public int DoseUnitId { get; set; }
    public int RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
    public DateTime MedicationGivenAt { get; set; }
}

public sealed class ManualNotificationDto
{
    public int Id { get; set; }
    public int NotificationTypeId { get; set; }
    public string PersonName { get; set; } = null!;
    public DateTime NotifiedAt { get; set; }
}

public sealed class IncidentReportAttachmentDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public int UploadedByUserId { get; set; }
}

public sealed class IncidentReportSummaryDto
{
    public int TotalReports { get; set; }
    public int AwaitingReview { get; set; }
    public int HarmEvents { get; set; }
    public int ClosedCases { get; set; }
}
