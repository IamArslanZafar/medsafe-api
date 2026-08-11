using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class NotificationStatusDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}

public class NotificationStatusUpsertDto
{
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
}

public class NotificationStatusBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}
