using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class ErrorCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class ErrorCategoryUpsertDto
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
}

public class ErrorCategoryBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}
