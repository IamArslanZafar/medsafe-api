namespace MedSafe.Models;

// Name holds the full medication + dose combination shown in the picker, e.g. "Warfarin 5mg".
public class CurrentMedication
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
