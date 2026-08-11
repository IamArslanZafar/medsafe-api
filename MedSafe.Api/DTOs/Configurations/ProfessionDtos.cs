using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class ProfessionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class ProfessionUpsertDto
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
}

public class ProfessionBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}
