namespace MedSafe.Models;

public class DropdownDefinition
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<DropdownValue> Values { get; set; } = new();
}
