namespace MedSafe.Models;

// Name holds the full medication + dose combination shown in the picker, e.g. "Warfarin 5mg".
public class CurrentMedication
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    // Populated when this medication is first added via the Incident Report
    // wizard's Add Medication modal, so picking it again on a later report can
    // auto-fill these fields. Never overwritten afterward — an existing entry
    // keeps its original values even if a later report uses different ones for
    // the same drug.
    public decimal? DoseValue { get; set; }
    public int? DoseUnitId { get; set; }
    public int? RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
