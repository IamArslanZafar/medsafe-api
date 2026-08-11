using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class StageOfProcessDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class StageOfProcessUpsertDto
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
}

public class StageOfProcessBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}
