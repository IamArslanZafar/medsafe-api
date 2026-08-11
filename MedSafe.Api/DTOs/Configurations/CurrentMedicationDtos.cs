using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class CurrentMedicationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class CurrentMedicationUpsertDto
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
}

public class CurrentMedicationBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}
