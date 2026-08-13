namespace MedSafe.Models;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // Machine-readable identifier for this permission node, e.g. "clinical_review.sign_off".
    public string PermissionTag { get; set; } = string.Empty;
    // Null = root node for its module (matches a module's top tab checkbox).
    public int? ParentId { get; set; }
    public int SystemModuleId { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
