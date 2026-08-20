using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class SectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UnitDepartmentId { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class SectionUpsertDto
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public int UnitDepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
}

public class SectionBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}
