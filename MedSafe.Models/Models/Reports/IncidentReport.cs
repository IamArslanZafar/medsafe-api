namespace MedSafe.Models;

public class IncidentReport
{
    public int Id { get; set; }

    // System
    public string IncidentReportNumber { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public int SubmittedByUserId { get; set; }
    public string SubmittedByRole { get; set; } = null!;
    public string ReportingFacilityUnit { get; set; } = null!;
    public string ReportStatus { get; set; } = null!;

    // Patient
    public string PatientReferenceToken { get; set; } = null!;
    public short PatientAge { get; set; }
    public string PatientSex { get; set; } = null!;
    public decimal? PatientWeightKg { get; set; }
    public string? PatientMedicalHistory { get; set; }

    // Medication
    public string MedicationName { get; set; } = null!;
    public string? GenericActiveIngredient { get; set; }
    public decimal DoseValue { get; set; }
    public int DoseUnitId { get; set; }
    public int RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
    public DateTime MedicationGivenAt { get; set; }
    public string? BatchLotNumber { get; set; }

    // Incident
    public string ReportType { get; set; } = null!;
    public int? ErrorCategoryId { get; set; }
    public int? StageOfProcessId { get; set; }
    public string? AdrReactionDescription { get; set; }
    public string? SuspectedCausality { get; set; }
    public string HarmLevelCode { get; set; } = null!;
    public DateTime IncidentOccurredAt { get; set; }
    public string IncidentLocation { get; set; } = null!;
    public string IncidentNarrative { get; set; } = null!;

    // Outcome
    public string? ImmediateActionTaken { get; set; }
    public int PatientOutcomeId { get; set; }
    public string? PatientOutcomeDetails { get; set; }

    // Navigation
    public ICollection<IncidentReportContributingFactor> ContributingFactors { get; set; } = new List<IncidentReportContributingFactor>();
    public ICollection<IncidentReportSeriousnessCriterion> SeriousnessCriteria { get; set; } = new List<IncidentReportSeriousnessCriterion>();
    public ICollection<IncidentReportAllergy> AllergyLinks { get; set; } = new List<IncidentReportAllergy>();
    public ICollection<IncidentReportCurrentMedication> CurrentMedicationLinks { get; set; } = new List<IncidentReportCurrentMedication>();
    public ICollection<IncidentReportAttachment> Attachments { get; set; } = new List<IncidentReportAttachment>();
    public ICollection<IncidentNotification> Notifications { get; set; } = new List<IncidentNotification>();
}
