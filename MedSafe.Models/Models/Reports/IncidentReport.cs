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
    public DateTime? PatientDateOfBirth { get; set; }
    public string? RelevantMedicalHistory { get; set; }
    public DateTime? AdmissionDate { get; set; }
    public string? CurrentDiagnosis { get; set; }

    // Step 3 - Incident & Harm
    // Legacy string snapshots — kept and dual-written alongside the FK ids below
    // because AlertRuleEvaluationService/DashboardService/Report Hub filters still
    // read these directly. Do not remove until those are migrated to the FK ids.
    public string ReportType { get; set; } = null!;
    public int? ErrorCategoryId { get; set; }
    public int? StageOfProcessId { get; set; }
    public string? AdrReactionDescription { get; set; }
    public string? SuspectedCausality { get; set; }
    public string? HarmLevelCode { get; set; }
    public DateTime IncidentOccurredAt { get; set; }
    public string IncidentLocation { get; set; } = null!;
    public string IncidentNarrative { get; set; } = null!;
    public int PatientOutcomeId { get; set; }

    // Lookup FK ids (new Medication Error / ADR schema)
    public int? ReportTypeId { get; set; }
    public int? HarmLevelId { get; set; }
    public int? SuspectedCausalityId { get; set; }
    public int? AdrSeverityId { get; set; }
    public int? IncidentUnitId { get; set; }
    public int? SectionId { get; set; }
    public int? VisitTypeId { get; set; }
    public int? ReportingSourceId { get; set; }
    public string? AdrAdditionalInformation { get; set; }

    // ADR only — onset/resolution of the reaction itself.
    public DateTime? ReactionStartAt { get; set; }
    public DateTime? ReactionStoppedAt { get; set; }

    // Common to both report types.
    public int? ReportedIncidentSeverityId { get; set; }
    public bool? IsResearchStudyRelated { get; set; }

    // Step 4
    public int? ProfessionId { get; set; }
    public int? PositionId { get; set; }
    public string? EnteredByTitle { get; set; }
    public string? ReporterPhoneNumber { get; set; }

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
    public ICollection<IncidentReportConcomitantMedication> ConcomitantMedications { get; set; } = new List<IncidentReportConcomitantMedication>();
    public ICollection<IncidentReportHealthcareProfessional> HealthcareProfessionals { get; set; } = new List<IncidentReportHealthcareProfessional>();
    public ICollection<IncidentReportWitness> Witnesses { get; set; } = new List<IncidentReportWitness>();
    public ICollection<IncidentReportOtherDepartment> OtherDepartments { get; set; } = new List<IncidentReportOtherDepartment>();
    public ICollection<IncidentReportReporter> Reporters { get; set; } = new List<IncidentReportReporter>();
    public ICollection<IncidentReportManualNotification> ManualNotifications { get; set; } = new List<IncidentReportManualNotification>();

    // Review workflow
    public IncidentReportReview? Review { get; set; }
    public ICollection<IncidentReportStatusHistory> StatusHistory { get; set; } = new List<IncidentReportStatusHistory>();
}
