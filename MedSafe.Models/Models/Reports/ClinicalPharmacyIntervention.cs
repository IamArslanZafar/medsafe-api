namespace MedSafe.Models;

// Clinical Pharmacy Intervention module — ported field-for-field from
// interventionData.js's INITIAL_FORM_DATA (34 fields), the source of truth
// for ClinicalPharmacyInterventionForm.jsx's multi-page form. Every field is
// free-text/nullable to match the frontend, which stores everything as
// plain strings regardless of apparent type (dates, numbers).
public class ClinicalPharmacyIntervention
{
    public int Id { get; set; }

    // Patient
    public string? Mrn { get; set; }
    public string? PatientName { get; set; }
    public string? Dob { get; set; }
    public string? Sex { get; set; }
    public string? VisitType { get; set; }
    public string? Facility { get; set; }
    public string? DepartmentUnit { get; set; }
    public string? Diagnosis { get; set; }
    public string? ReportingFacility { get; set; }
    public string? ServiceType { get; set; }

    // Medication
    public string? Medication { get; set; }
    public string? Strength { get; set; }
    public string? Route { get; set; }
    public string? Frequency { get; set; }
    public string? CurrentDose { get; set; }
    public string? Indication { get; set; }
    public string? StartDate { get; set; }
    public string? LastDose { get; set; }
    public string? DrugClass { get; set; }

    // Intervention
    public string? InterventionType { get; set; }
    public string? InterventionSubtype { get; set; }
    public string? IdentifiedProblem { get; set; }
    public string? RecommendedAction { get; set; }
    public string? RecommendedDose { get; set; }
    public string? RecommendedFrequency { get; set; }
    public string? RecommendedMonitoring { get; set; }
    public string? ClinicalRationale { get; set; }

    // Impact
    public string? Importance { get; set; }
    public string? TimeSpent { get; set; }
    public decimal? EstimatedSaving { get; set; }
    public string Currency { get; set; } = "SAR";
    public string? MedError { get; set; }
    public string? Merp { get; set; }
    public string? Adr { get; set; }

    // Prescriber
    public string? PrescriberName { get; set; }
    public string? PrescriberDepartment { get; set; }
    public string? ContactMethod { get; set; }
    public string? ContactDateTime { get; set; }
    public string? PrescriberResponse { get; set; }
    public string? ResponseDetails { get; set; }

    // Outcome
    public string? Outcome { get; set; }
    public string? FollowupRequired { get; set; }
    public string? FollowupDate { get; set; }

    // Other
    public string? AdditionalNotes { get; set; }
    // "Draft" | "Submitted" — matches ClinicalPharmacyInterventionForm.jsx's draftStatus.
    public string DraftStatus { get; set; } = "Draft";
    // JSON array of attachment ids — the form supports multiple files.
    public string AttachmentIdsJson { get; set; } = "[]";
    public int? SubmittedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
