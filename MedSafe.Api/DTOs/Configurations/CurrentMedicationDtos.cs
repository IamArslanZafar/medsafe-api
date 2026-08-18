using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class CurrentMedicationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    // A row's Name is not unique — the same drug/strength label can have several
    // rows, one per distinct Dose/Unit/Route/Frequency/Formulation combination
    // actually used. Null fields mean this particular row has no dose data on
    // file (e.g. added from Configurations without filling them in).
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
    // Optional — part of Create's find-or-create tuple (Name + these 5 fields).
    // Also settable directly by an Admin from the Configurations edit form.
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
