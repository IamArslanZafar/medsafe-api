using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class ReportedIncidentSeverityDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class ReportedIncidentSeverityUpsertDto
{
    public string? Code { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
}

public class ReportedIncidentSeverityBulkDeleteDto
{
    [Required] public List<int> Ids { get; set; } = new();
}
