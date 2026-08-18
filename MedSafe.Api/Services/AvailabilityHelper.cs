using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

// Shared by AuthController.Register (initial availability on user creation) and
// UsersController's dedicated availability endpoint (edit flow) so the day
// mapping and validation rules — 1..7, no duplicate days, overnight shifts
// allowed — only live in one place.
public static class AvailabilityHelper
{
    public static readonly string[] DayNames =
        ["", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    // Returns null on success, or a client-facing error message.
    public static string? Validate(List<AvailabilityRequestDto> requests)
    {
        var seenDays = new HashSet<int>();

        foreach (var r in requests)
        {
            if (r.DayOfWeek < 1 || r.DayOfWeek > 7)
                return $"Invalid day of week: {r.DayOfWeek}.";

            if (!seenDays.Add(r.DayOfWeek))
                return $"{DayNames[r.DayOfWeek]} was selected more than once.";

            if (!TimeOnly.TryParse(r.StartTime, out var start))
                return $"Invalid start time for {DayNames[r.DayOfWeek]}.";

            if (!TimeOnly.TryParse(r.EndTime, out var end))
                return $"Invalid end time for {DayNames[r.DayOfWeek]}.";

            // Overnight shifts (e.g. 20:00 -> 08:00) are valid — only an exact
            // match (zero-length shift) is rejected.
            if (start == end)
                return $"Start and end time cannot be equal for {DayNames[r.DayOfWeek]}.";
        }

        return null;
    }

    public static List<UserAvailability> BuildEntities(int userId, List<AvailabilityRequestDto> requests) =>
        requests.Select(r => new UserAvailability
        {
            UserId = userId,
            DayOfWeek = r.DayOfWeek,
            StartTime = TimeOnly.Parse(r.StartTime),
            EndTime = TimeOnly.Parse(r.EndTime)
        }).ToList();

    public static AvailabilityDto ToDto(UserAvailability a) => new()
    {
        Id = a.Id,
        DayOfWeek = a.DayOfWeek,
        DayName = DayNames[a.DayOfWeek],
        StartTime = a.StartTime.ToString("HH:mm"),
        EndTime = a.EndTime.ToString("HH:mm"),
        IsOvernight = a.EndTime < a.StartTime
    };
}
