using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class CurrentMedicationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    // Set only when this entry was first created from the Incident Report wizard's
    // Add Medication modal — lets the frontend auto-fill those fields when this
    // medication is picked again on a later report. Null for entries added from
    // Configurations (or older ones from before this feature).
    public decimal? DoseValue { get; set; }
    public int? DoseUnitId { get; set; }
    public int? RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
}

public class CurrentMedicationUpsertDto
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
    // Optional — set inline from the Add Medication modal on create, or
    // directly by an Admin from the Configurations edit form. Update always
    // overwrites with these (an Admin's deliberate correction); the automatic
    // wizard backfill instead goes through SetDoseDefaults below, which never
    // overwrites a value already on file.
    public decimal? DoseValue { get; set; }
    public int? DoseUnitId { get; set; }
    public int? RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
}

public class CurrentMedicationBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}

public class SetCurrentMedicationDoseDefaultsDto
{
    public decimal? DoseValue { get; set; }
    public int? DoseUnitId { get; set; }
    public int? RouteId { get; set; }
    public int? FrequencyId { get; set; }
    public int? FormulationId { get; set; }
}
