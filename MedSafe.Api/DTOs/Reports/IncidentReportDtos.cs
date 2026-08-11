using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public sealed class SubmitIncidentReportRequest
{
    // STEP 1 - PATIENT
    public short PatientAge { get; set; }
    public string PatientSex { get; set; } = string.Empty;
    public decimal? PatientWeightKg { get; set; }
    public string? PatientMedicalHistory { get; set; }
    public List<int> KnownAllergyIds { get; set; } = [];
    public List<int> CurrentMedicationIds { get; set; } = [];

    // STEP 2 - MEDICATION
    public string MedicationName { get; set; } = string.Empty;
    public string? GenericActiveIngredient { get; set; }
    public decimal DoseValue { get; set; }
    public int DoseUnitId { get; set; }
    public int RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
    public DateTime MedicationGivenAt { get; set; }
    public string? BatchLotNumber { get; set; }

    // STEP 3 - INCIDENT / HARM
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

    // STEP 4 - OUTCOME
    public string? ImmediateActionTaken { get; set; }
    public int PatientOutcomeId { get; set; }
    public string? PatientOutcomeDetails { get; set; }

    // REPORTING LOCATION
    public string ReportingFacilityUnit { get; set; } = string.Empty;

    // Intentionally absent: Id, IncidentReportNumber, PatientReferenceToken, SubmittedAt,
    // SubmittedByUserId, SubmittedByRole, ReportStatus — the backend generates these.
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
    public string ReportingFacilityUnit { get; set; } = null!;
    public string ReportStatus { get; set; } = null!;

    public string PatientReferenceToken { get; set; } = null!;
    public short PatientAge { get; set; }
    public string PatientSex { get; set; } = null!;
    public decimal? PatientWeightKg { get; set; }
    public string? PatientMedicalHistory { get; set; }
    public List<int> KnownAllergyIds { get; set; } = [];
    public List<int> CurrentMedicationIds { get; set; } = [];

    public string MedicationName { get; set; } = null!;
    public string? GenericActiveIngredient { get; set; }
    public decimal DoseValue { get; set; }
    public int DoseUnitId { get; set; }
    public int RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
    public DateTime MedicationGivenAt { get; set; }
    public string? BatchLotNumber { get; set; }

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

    public string? ImmediateActionTaken { get; set; }
    public int PatientOutcomeId { get; set; }
    public string? PatientOutcomeDetails { get; set; }

    public List<IncidentReportAttachmentDto> Attachments { get; set; } = [];
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
