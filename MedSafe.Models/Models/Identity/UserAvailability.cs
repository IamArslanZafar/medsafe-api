namespace MedSafe.Models;

// One row per working day — replaces the old free-text Shift string with a
// proper per-day schedule. DayOfWeek: 1=Monday .. 7=Sunday. EndTime < StartTime
// is a valid overnight shift (e.g. 20:00 -> 08:00), not an error.
public class UserAvailability
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public User User { get; set; } = null!;
}
