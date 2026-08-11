namespace MedSafe.Models;

public class DropdownValue
{
    public string Id { get; set; } = string.Empty;

    public int DropdownDefinitionId { get; set; }
    public DropdownDefinition? DropdownDefinition { get; set; }

    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
