using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class RouteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class RouteUpsertDto
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
}

public class RouteBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}
