namespace MedSafe.Models;

// ADR only — "Outpatient Medications" / "Inpatient Medications" free-text blocks,
// distinct from the structured Current Medication ids on IncidentReport.
public class IncidentReportConcomitantMedication
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }

    public string CareSettingCode { get; set; } = null!; // INPATIENT | OUTPATIENT
    public string MedicationText { get; set; } = null!;

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
}
