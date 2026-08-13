using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class PermissionNodeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PermissionTag { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public List<PermissionNodeDto> Children { get; set; } = new();
}

public class PermissionModuleDto
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public List<PermissionNodeDto> Permissions { get; set; } = new();
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PermissionCount { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RoleDetailDto : RoleDto
{
    public List<int> PermissionIds { get; set; } = new();
}

public class RoleUpsertDto
{
    [Required, MinLength(2)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<int> PermissionIds { get; set; } = new();
}
