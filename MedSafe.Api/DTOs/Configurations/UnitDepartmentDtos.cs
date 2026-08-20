using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class UnitDepartmentDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class UnitDepartmentUpsertDto
{
    public string? Code { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
}

public class UnitDepartmentBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}
