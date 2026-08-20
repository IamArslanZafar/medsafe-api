namespace MedSafe.Models;

public class IncidentReportMedication
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }

    public string MedicationName { get; set; } = null!;
    public string? GenericName { get; set; }
    public string? DrugClass { get; set; }

    // Nullable — Medication Error requires these (enforced in service validation);
    // an ADR suspected medication may not have every value available.
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

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
}
