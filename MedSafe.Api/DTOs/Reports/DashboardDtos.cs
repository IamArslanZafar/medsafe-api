namespace MedSafeAPI.DTOs;

public sealed class DashboardSummaryRequest
{
    // Omit or pass "All Units" for no unit filter.
    public string? FacilityUnit { get; set; }

    // Each independently optional — whichever is omitted defaults to the matching
    // end of the previous complete calendar week (last Monday..Sunday).
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class DashboardSummaryDto
{
    public int TotalReports { get; set; }
    public int NearMissRatePct { get; set; }
    public int HarmEvents { get; set; }
    public int AdrReactions { get; set; }
    public int PendingReview { get; set; }

    public DashboardBreakdownDto Breakdown { get; set; } = new();
    public DashboardTrendDto Trend { get; set; } = new();
    public List<DashboardNamedCountDto> StageOfProcess { get; set; } = new();
    public List<DashboardNamedCountDto> Severity { get; set; } = new();
    public List<DashboardNamedCountDto> TopMedications { get; set; } = new();
    public List<DashboardNamedCountDto> ErrorTypesByNature { get; set; } = new();
}

public sealed class DashboardBreakdownDto
{
    public int MedicationErrors { get; set; }
    public int NearMissReports { get; set; }
    public int AdrReactions { get; set; }
    public int ClosedResolved { get; set; }
    public int PendingReview { get; set; }
}

public sealed class DashboardTrendDto
{
    // One point per day across the request's date range (see DashboardController.GetSummary),
    // oldest first — e.g. "03 Aug". Defaults to the previous complete calendar week (7 points)
    // when no startDate/endDate is passed.
    public List<string> Labels { get; set; } = new();
    public List<int> MedicationErrors { get; set; } = new();
    public List<int> NearMisses { get; set; } = new();
    public List<int> Adr { get; set; } = new();
    // Severity E-I on that day, regardless of report type — same definition as the top-level HarmEvents KPI.
    public List<int> HarmEvents { get; set; } = new();
}

public sealed class DashboardNamedCountDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
