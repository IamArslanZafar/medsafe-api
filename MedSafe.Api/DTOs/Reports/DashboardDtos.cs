namespace MedSafeAPI.DTOs;

public sealed class DashboardSummaryRequest
{
    // Omit or pass "All Units" for no unit filter.
    public string? FacilityUnit { get; set; }

    // Each independently optional — whichever is omitted defaults to the matching
    // end of the previous complete calendar week (last Monday..Sunday).
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // All optional — same classification fields used on the Submit form. Each
    // narrows the same report set the KPIs/breakdown/trend/etc. are computed from.
    // Raw value — "Medication Error" | "Near Miss" | "ADR" (matches IncidentReport.ReportType).
    public string? ReportType { get; set; }
    public string? MedicationName { get; set; }
    public int? ErrorCategoryId { get; set; }
    public int? StageOfProcessId { get; set; }
    public int? PatientOutcomeId { get; set; }
    // WHO-UMC causality scale value (e.g. "probable") — stored as free text on
    // IncidentReport, not a lookup table FK.
    public string? SuspectedCausality { get; set; }
    public int? ContributingFactorId { get; set; }
    public int? SeriousnessCriterionId { get; set; }
}

public sealed class DashboardSummaryDto
{
    public int TotalReports { get; set; }
    public int NearMissRatePct { get; set; }
    public int HarmEvents { get; set; }
    public int AdrReactions { get; set; }
    // Only ReportStatus == "Pending" — UnderReview is now its own separate count.
    public int PendingReview { get; set; }
    public int UnderReviewCount { get; set; }
    // Pending/UnderReview reports still unreviewed 48h+ after submission — same
    // threshold as the automatic reviewer reminder (EmailNotificationWorker-adjacent
    // business rule, documented on the Training & Support "Reporting SOP" tab).
    public int OverdueCount { get; set; }

    public DashboardBreakdownDto Breakdown { get; set; } = new();
    public DashboardTrendDto Trend { get; set; } = new();
    // Pending / Under Review / Closed / Overdue >48h, in that order.
    public List<DashboardNamedCountDto> ReviewStatus { get; set; } = new();
    // Overall — spans both report types.
    public List<DashboardNamedCountDto> TopMedications { get; set; } = new();
    public List<DashboardNamedCountDto> TopLocations { get; set; } = new();
    public List<DashboardNamedCountDto> PatientOutcomeOverall { get; set; } = new();
    public List<DashboardNamedCountDto> ContributingFactorsOverall { get; set; } = new();
    // Medication Error / ADR-only top medications, for the two report-type-specific highlight cards.
    public List<DashboardNamedCountDto> TopMeMedications { get; set; } = new();
    public List<DashboardNamedCountDto> TopAdrMedications { get; set; } = new();

    // Medication Error Dashboard-only (see AssignPermissionsModal's "Submit Medication
    // Error Reports"-only view) — computed from this request's Medication Error reports only.
    public List<DashboardNamedCountDto> ErrorCategoryBreakdown { get; set; } = new();
    public List<DashboardNamedCountDto> StageOfProcessBreakdown { get; set; } = new();
    public List<DashboardNamedCountDto> NccMerpBreakdown { get; set; } = new();
    public List<DashboardNamedCountDto> ReportedSeverityBreakdown { get; set; } = new();

    // ADR Dashboard-only ("Submit ADR Reports"-only view) — computed from this
    // request's ADR reports only.
    public List<DashboardNamedCountDto> AdrSeverityBreakdown { get; set; } = new();
    public List<DashboardNamedCountDto> WhoUmcCausalityBreakdown { get; set; } = new();
    public List<DashboardNamedCountDto> SeriousnessCriteriaBreakdown { get; set; } = new();
    public List<DashboardNamedCountDto> ReportingSourceBreakdown { get; set; } = new();
    // An ADR report with at least one linked Seriousness Criterion (ICH definition).
    public int SeriousAdrCount { get; set; }
    // Alert Rule condition-field usage — how many configured (non-deleted) rules
    // use each supported field at least once. Rule configuration data, not scoped
    // to this request's date/unit/report filters (see AlertRuleFieldUsageDto).
    public List<AlertRuleFieldUsageDto> FieldUsage { get; set; } = new();
    // Alert Rule status counts — same rule-configuration data as FieldUsage above
    // (not scoped to this request's date/unit/report filters). Powers the "Report
    // Breakdown" card's Alert Rules view on the dashboard.
    public int TotalAlertRules { get; set; }
    public int ActiveAlertRules { get; set; }
    public int CriticalAlertRules { get; set; }
    public int AlertsTriggeredLast24Hours { get; set; }
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
    public List<int> Adr { get; set; } = new();
    // Severity E-I on that day, regardless of report type — same definition as the top-level HarmEvents KPI.
    public List<int> HarmEvents { get; set; } = new();
}

public sealed class DashboardNamedCountDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
