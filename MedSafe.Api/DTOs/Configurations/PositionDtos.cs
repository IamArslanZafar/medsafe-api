using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class PositionDto
{
    public int Id { get; set; }
    public int ProfessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class PositionUpsertDto
{
    [Required] public int ProfessionId { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
}

public class PositionBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}
