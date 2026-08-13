namespace MedSafe.Models;

public class SystemModule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
