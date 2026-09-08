namespace MedSafeAPI.DTOs;

// Same field list as ClinicalPharmacyIntervention.cs / interventionData.js's
// INITIAL_FORM_DATA — every field optional at the DTO level since the
// frontend lets a Draft be saved with only some pages filled in.
public sealed class ClinicalPharmacyInterventionDto
{
    public int Id { get; set; }
    public string? InterventionCode { get; set; }
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
    public string? Medication { get; set; }
    public string? Strength { get; set; }
    public string? Route { get; set; }
    public string? Frequency { get; set; }
    public string? CurrentDose { get; set; }
    public string? Indication { get; set; }
    public string? StartDate { get; set; }
    public string? LastDose { get; set; }
    public string? DrugClass { get; set; }
    public string? InterventionType { get; set; }
    public string? InterventionSubtype { get; set; }
    public string? IdentifiedProblem { get; set; }
    public string? RecommendedAction { get; set; }
    public string? RecommendedDose { get; set; }
    public string? RecommendedFrequency { get; set; }
    public string? RecommendedMonitoring { get; set; }
    public string? ClinicalRationale { get; set; }
    public string? Importance { get; set; }
    public string? TimeSpent { get; set; }
    public decimal? EstimatedSaving { get; set; }
    public string Currency { get; set; } = "SAR";
    public string? MedError { get; set; }
    public string? Merp { get; set; }
    public string? Adr { get; set; }
    public string? PrescriberName { get; set; }
    public string? PrescriberDepartment { get; set; }
    public string? ContactMethod { get; set; }
    public string? ContactDateTime { get; set; }
    public string? PrescriberResponse { get; set; }
    public string? ResponseDetails { get; set; }
    public string? Outcome { get; set; }
    public string? FollowupRequired { get; set; }
    public string? FollowupDate { get; set; }
    public string? AdditionalNotes { get; set; }
    public string DraftStatus { get; set; } = "Draft";
    public string AttachmentIdsJson { get; set; } = "[]";
    public int? SubmittedByUserId { get; set; }
    public string? SubmittedByRole { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ClinicalPharmacyInterventionCreateDto
{
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
    public string? Medication { get; set; }
    public string? Strength { get; set; }
    public string? Route { get; set; }
    public string? Frequency { get; set; }
    public string? CurrentDose { get; set; }
    public string? Indication { get; set; }
    public string? StartDate { get; set; }
    public string? LastDose { get; set; }
    public string? DrugClass { get; set; }
    public string? InterventionType { get; set; }
    public string? InterventionSubtype { get; set; }
    public string? IdentifiedProblem { get; set; }
    public string? RecommendedAction { get; set; }
    public string? RecommendedDose { get; set; }
    public string? RecommendedFrequency { get; set; }
    public string? RecommendedMonitoring { get; set; }
    public string? ClinicalRationale { get; set; }
    public string? Importance { get; set; }
    public string? TimeSpent { get; set; }
    public decimal? EstimatedSaving { get; set; }
    public string Currency { get; set; } = "SAR";
    public string? MedError { get; set; }
    public string? Merp { get; set; }
    public string? Adr { get; set; }
    public string? PrescriberName { get; set; }
    public string? PrescriberDepartment { get; set; }
    public string? ContactMethod { get; set; }
    public string? ContactDateTime { get; set; }
    public string? PrescriberResponse { get; set; }
    public string? ResponseDetails { get; set; }
    public string? Outcome { get; set; }
    public string? FollowupRequired { get; set; }
    public string? FollowupDate { get; set; }
    public string? AdditionalNotes { get; set; }
    // "Draft" (Save Draft) or "Submitted" (final Submit).
    public string DraftStatus { get; set; } = "Draft";
    public string AttachmentIdsJson { get; set; } = "[]";
}
