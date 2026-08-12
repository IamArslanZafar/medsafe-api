namespace MedSafe.Models;

public class IncidentReport
{
    public int Id { get; set; }

    // Backend generated
    public string IncidentReportNumber { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public int SubmittedByUserId { get; set; }
    public string SubmittedByRole { get; set; } = null!;
    public string ReportStatus { get; set; } = null!;

    // Step 1 - Patient
    public string PatientReferenceToken { get; set; } = null!;
    public string? PatientReference { get; set; }
    public string? PatientName { get; set; }
    public short PatientAge { get; set; }
    public string PatientSex { get; set; } = null!;
    public decimal? PatientWeightKg { get; set; }

    // Step 3 - Incident & Harm
    public string ReportType { get; set; } = null!;
    public int? ErrorCategoryId { get; set; }
    public int? StageOfProcessId { get; set; }
    public string? AdrReactionDescription { get; set; }
    public string? SuspectedCausality { get; set; }
    public string HarmLevelCode { get; set; } = null!;
    public DateTime IncidentOccurredAt { get; set; }
    public string IncidentLocation { get; set; } = null!;
    public string IncidentNarrative { get; set; } = null!;
    public int PatientOutcomeId { get; set; }

    // Step 4
    public int? ProfessionId { get; set; }
    public int? PositionId { get; set; }

    // Step 5
    public string? ImmediateActionTaken { get; set; }
    public string? PatientOutcomeDetails { get; set; }

    // Child records
    public ICollection<IncidentReportMedication> Medications { get; set; } = new List<IncidentReportMedication>();
    public ICollection<IncidentReportContributingFactor> ContributingFactors { get; set; } = new List<IncidentReportContributingFactor>();
    public ICollection<IncidentReportSeriousnessCriterion> SeriousnessCriteria { get; set; } = new List<IncidentReportSeriousnessCriterion>();
    public ICollection<IncidentReportAllergy> AllergyLinks { get; set; } = new List<IncidentReportAllergy>();
    public ICollection<IncidentReportCurrentMedication> CurrentMedicationLinks { get; set; } = new List<IncidentReportCurrentMedication>();
    public ICollection<IncidentReportAttachment> Attachments { get; set; } = new List<IncidentReportAttachment>();
    public ICollection<IncidentNotification> Notifications { get; set; } = new List<IncidentNotification>();

    // Review workflow
    public IncidentReportReview? Review { get; set; }
    public ICollection<IncidentReportStatusHistory> StatusHistory { get; set; } = new List<IncidentReportStatusHistory>();
}
