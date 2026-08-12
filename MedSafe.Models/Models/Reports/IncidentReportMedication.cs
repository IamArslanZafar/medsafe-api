namespace MedSafe.Models;

public class IncidentReportMedication
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }

    public string MedicationName { get; set; } = null!;
    public decimal DoseValue { get; set; }
    public int DoseUnitId { get; set; }
    public int RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
    public DateTime MedicationGivenAt { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
}
