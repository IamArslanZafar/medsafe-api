namespace MedSafeAPI.DTOs;

public class AvailabilityRequestDto
{
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

public class AvailabilityDto
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsOvernight { get; set; }
}

public class UpdateUserAvailabilityDto
{
    public List<AvailabilityRequestDto> Availability { get; set; } = [];
}
