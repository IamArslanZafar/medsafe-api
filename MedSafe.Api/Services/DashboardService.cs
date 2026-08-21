using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public class DashboardService : IDashboardService
{
    private static readonly string[] HarmCodes = ["E", "F", "G", "H", "I"];
    private static readonly string[] NccMerpCodes = ["A", "B", "C", "D", "E", "F", "G", "H", "I"];

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlertRuleService _alertRuleService;

    public DashboardService(AppDbContext db, ICurrentUserService currentUser, IAlertRuleService alertRuleService)
    {
        _db = db;
        _currentUser = currentUser;
        _alertRuleService = alertRuleService;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(DashboardSummaryRequest request, CancellationToken cancellationToken)
    {
        // Previous complete calendar week (last Monday..Sunday, not the current week) —
        // same fallback SystemMonitorControl's sp_GetComputerDailySummaryPagedWithStats
        // uses, applied per-parameter so passing only one of the two still works.
        var today = DateTime.UtcNow.Date;
        var daysSinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var currentWeekMonday = today.AddDays(-daysSinceMonday);
        var lastWeekMonday = currentWeekMonday.AddDays(-7);
        var lastWeekSunday = currentWeekMonday.AddDays(-1);

        var actualStartDate = (request.StartDate ?? lastWeekMonday).Date;
        var actualEndDate = (request.EndDate ?? lastWeekSunday).Date;

        // Filtered by SubmittedAt (when it was logged into the system), not
        // IncidentOccurredAt (when the incident clinically happened) — a report
        // submitted today with an earlier incident date should still count as
        // "today" on the dashboard, matching the "All incidents logged" KPI wording.
        var query = _db.IncidentReports
            .AsNoTracking()
            .Include(r => r.Medications)
            .Where(r => r.SubmittedAt.Date >= actualStartDate && r.SubmittedAt.Date <= actualEndDate)
            .AsQueryable();

        // Only Admin sees every report; every other role is scoped to their own.
        if (_currentUser.Role != "Admin")
            query = query.Where(r => r.SubmittedByUserId == _currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(request.FacilityUnit) && request.FacilityUnit != "All Units")
            query = query.Where(r => r.IncidentLocation == request.FacilityUnit);

        if (!string.IsNullOrWhiteSpace(request.ReportType))
            query = query.Where(r => r.ReportType == request.ReportType);

        if (!string.IsNullOrWhiteSpace(request.MedicationName))
            query = query.Where(r => r.Medications.Any(m => m.MedicationName == request.MedicationName));

        if (request.ErrorCategoryId.HasValue)
            query = query.Where(r => r.ErrorCategoryId == request.ErrorCategoryId);

        if (request.StageOfProcessId.HasValue)
            query = query.Where(r => r.StageOfProcessId == request.StageOfProcessId);

        if (request.PatientOutcomeId.HasValue)
            query = query.Where(r => r.PatientOutcomeId == request.PatientOutcomeId);

        if (!string.IsNullOrWhiteSpace(request.SuspectedCausality))
            query = query.Where(r => r.SuspectedCausality == request.SuspectedCausality);

        if (request.ContributingFactorId.HasValue)
            query = query.Where(r => r.ContributingFactors.Any(cf => cf.ContributingFactorId == request.ContributingFactorId));

        if (request.SeriousnessCriterionId.HasValue)
            query = query.Where(r => r.SeriousnessCriteria.Any(sc => sc.SeriousnessCriterionId == request.SeriousnessCriterionId));

        var reports = await query.ToListAsync(cancellationToken);

        var patientOutcomeNames = await _db.PatientOutcomes.AsNoTracking()
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
        var contributingFactorNames = await _db.ContributingFactors.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
        var errorCategoryNames = await _db.ErrorCategories.AsNoTracking()
            .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);
        var stageNames = await _db.StageOfProcesses.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);
        var reportedSeverityNames = await _db.ReportedIncidentSeverities.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);
        var adrSeverityNames = await _db.AdrSeverities.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);
        var seriousnessCriterionNames = await _db.SeriousnessCriteria.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);
        var reportingSourceNames = await _db.ReportingSources.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var total = reports.Count;
        var harmCount = reports.Count(r => HarmCodes.Contains(r.HarmLevelCode));
        var nearMissCount = reports.Count(r => r.ReportType == "Near Miss");
        var adrCount = reports.Count(r => r.ReportType == "ADR");
        var errorCount = reports.Count(r => r.ReportType == "Medication Error");
        var pendingOnlyCount = reports.Count(r => r.ReportStatus == "Pending");
        var underReviewCount = reports.Count(r => r.ReportStatus == "UnderReview");
        var closedCount = reports.Count(r => r.ReportStatus == "Closed");
        var overdueCount = reports.Count(r =>
            (r.ReportStatus == "Pending" || r.ReportStatus == "UnderReview")
            && (DateTime.UtcNow - r.SubmittedAt).TotalHours > 48);

        var topMedications = reports
            .SelectMany(r => r.Medications)
            .Where(m => !string.IsNullOrWhiteSpace(m.MedicationName))
            .GroupBy(m => m.MedicationName)
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var meReports = reports.Where(r => r.ReportType == "Medication Error").ToList();
        var adrReports = reports.Where(r => r.ReportType == "ADR").ToList();

        var topMeMedications = meReports
            .SelectMany(r => r.Medications)
            .Where(m => !string.IsNullOrWhiteSpace(m.MedicationName))
            .GroupBy(m => m.MedicationName)
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToList();

        var topAdrMedications = adrReports
            .SelectMany(r => r.Medications)
            .Where(m => !string.IsNullOrWhiteSpace(m.MedicationName))
            .GroupBy(m => m.MedicationName)
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToList();

        var topLocations = reports
            .Where(r => !string.IsNullOrWhiteSpace(r.IncidentLocation))
            .GroupBy(r => r.IncidentLocation)
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        var patientOutcomeOverall = reports
            .Where(r => patientOutcomeNames.ContainsKey(r.PatientOutcomeId))
            .GroupBy(r => patientOutcomeNames[r.PatientOutcomeId])
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToList();

        var reportIds = reports.Select(r => r.Id).ToHashSet();
        var contributingFactorsRaw = await _db.IncidentReportContributingFactors
            .AsNoTracking()
            .Where(cf => reportIds.Contains(cf.IncidentReportId))
            .GroupBy(cf => cf.ContributingFactorId)
            .Select(g => new { FactorId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var contributingFactorsOverall = contributingFactorsRaw
            .Where(x => contributingFactorNames.ContainsKey(x.FactorId))
            .Select(x => new DashboardNamedCountDto { Name = contributingFactorNames[x.FactorId], Count = x.Count })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToList();

        var reviewStatus = new List<DashboardNamedCountDto>
        {
            new() { Name = "Pending", Count = pendingOnlyCount },
            new() { Name = "Under Review", Count = underReviewCount },
            new() { Name = "Closed", Count = closedCount },
            new() { Name = "Overdue >48h", Count = overdueCount },
        };

        // ── Medication Error Dashboard-only breakdowns ──
        var errorCategoryBreakdown = meReports
            .Where(r => r.ErrorCategoryId.HasValue && errorCategoryNames.ContainsKey(r.ErrorCategoryId.Value))
            .GroupBy(r => errorCategoryNames[r.ErrorCategoryId!.Value])
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(6).ToList();

        var stageOfProcessBreakdown = meReports
            .Where(r => r.StageOfProcessId.HasValue && stageNames.ContainsKey(r.StageOfProcessId.Value))
            .GroupBy(r => stageNames[r.StageOfProcessId!.Value])
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(6).ToList();

        var nccMerpBreakdown = NccMerpCodes
            .Select(c => new DashboardNamedCountDto { Name = c, Count = meReports.Count(r => r.HarmLevelCode == c) })
            .ToList();

        var reportedSeverityBreakdown = meReports
            .Where(r => r.ReportedIncidentSeverityId.HasValue && reportedSeverityNames.ContainsKey(r.ReportedIncidentSeverityId.Value))
            .GroupBy(r => reportedSeverityNames[r.ReportedIncidentSeverityId!.Value])
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(6).ToList();

        // ── ADR Dashboard-only breakdowns ──
        var adrSeverityBreakdown = adrReports
            .Where(r => r.AdrSeverityId.HasValue && adrSeverityNames.ContainsKey(r.AdrSeverityId.Value))
            .GroupBy(r => adrSeverityNames[r.AdrSeverityId!.Value])
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(6).ToList();

        var whoUmcCausalityBreakdown = adrReports
            .Where(r => !string.IsNullOrWhiteSpace(r.SuspectedCausality))
            .GroupBy(r => r.SuspectedCausality!)
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();

        var reportingSourceBreakdown = adrReports
            .Where(r => r.ReportingSourceId.HasValue && reportingSourceNames.ContainsKey(r.ReportingSourceId.Value))
            .GroupBy(r => reportingSourceNames[r.ReportingSourceId!.Value])
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(6).ToList();

        var adrReportIds = adrReports.Select(r => r.Id).ToHashSet();
        var seriousnessRaw = await _db.IncidentReportSeriousnessCriteria
            .AsNoTracking()
            .Where(sc => adrReportIds.Contains(sc.IncidentReportId))
            .GroupBy(sc => sc.SeriousnessCriterionId)
            .Select(g => new { CriterionId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var seriousnessCriteriaBreakdown = seriousnessRaw
            .Where(x => seriousnessCriterionNames.ContainsKey(x.CriterionId))
            .Select(x => new DashboardNamedCountDto { Name = seriousnessCriterionNames[x.CriterionId], Count = x.Count })
            .OrderByDescending(x => x.Count).Take(6).ToList();

        // A "serious" ADR is any ADR report with at least one linked Seriousness
        // Criterion (hospitalization, life-threatening, etc. — ICH definition).
        var seriousAdrCount = await _db.IncidentReportSeriousnessCriteria
            .AsNoTracking()
            .Where(sc => adrReportIds.Contains(sc.IncidentReportId))
            .Select(sc => sc.IncidentReportId)
            .Distinct()
            .CountAsync(cancellationToken);

        var alertRuleSummary = await _alertRuleService.GetSummaryAsync(cancellationToken);

        // A single-day range (Today/Yesterday) bucketed by day collapses to one
        // data point — not a usable trend line. Bucket by hour instead whenever
        // the range is exactly one day; multi-day ranges stay day-bucketed.
        DashboardTrendDto trend;
        if (actualStartDate == actualEndDate)
        {
            var hours = Enumerable.Range(0, 24).ToList();
            trend = new DashboardTrendDto
            {
                Labels = hours.Select(h => new DateTime(1, 1, 1, h, 0, 0).ToString("h tt")).ToList(),
                MedicationErrors = hours.Select(h => reports.Count(r => r.ReportType == "Medication Error" && r.SubmittedAt.Date == actualStartDate && r.SubmittedAt.Hour == h)).ToList(),
                Adr = hours.Select(h => reports.Count(r => r.ReportType == "ADR" && r.SubmittedAt.Date == actualStartDate && r.SubmittedAt.Hour == h)).ToList(),
                HarmEvents = hours.Select(h => reports.Count(r => HarmCodes.Contains(r.HarmLevelCode) && r.SubmittedAt.Date == actualStartDate && r.SubmittedAt.Hour == h)).ToList(),
            };
        }
        else
        {
            var dayCount = (actualEndDate - actualStartDate).Days + 1;
            var days = Enumerable.Range(0, dayCount).Select(i => actualStartDate.AddDays(i)).ToList();
            trend = new DashboardTrendDto
            {
                Labels = days.Select(d => d.ToString("dd MMM")).ToList(),
                MedicationErrors = days.Select(d => reports.Count(r => r.ReportType == "Medication Error" && r.SubmittedAt.Date == d)).ToList(),
                Adr = days.Select(d => reports.Count(r => r.ReportType == "ADR" && r.SubmittedAt.Date == d)).ToList(),
                HarmEvents = days.Select(d => reports.Count(r => HarmCodes.Contains(r.HarmLevelCode) && r.SubmittedAt.Date == d)).ToList(),
            };
        }

        return new DashboardSummaryDto
        {
            TotalReports = total,
            NearMissRatePct = total == 0 ? 0 : (int)Math.Round(nearMissCount * 100.0 / total),
            HarmEvents = harmCount,
            AdrReactions = adrCount,
            PendingReview = pendingOnlyCount,
            UnderReviewCount = underReviewCount,
            OverdueCount = overdueCount,
            Breakdown = new DashboardBreakdownDto
            {
                MedicationErrors = errorCount,
                NearMissReports = nearMissCount,
                AdrReactions = adrCount,
                ClosedResolved = closedCount,
                PendingReview = pendingOnlyCount + underReviewCount,
            },
            Trend = trend,
            ReviewStatus = reviewStatus,
            TopMedications = topMedications,
            TopMeMedications = topMeMedications,
            TopAdrMedications = topAdrMedications,
            TopLocations = topLocations,
            PatientOutcomeOverall = patientOutcomeOverall,
            ContributingFactorsOverall = contributingFactorsOverall,
            ErrorCategoryBreakdown = errorCategoryBreakdown,
            StageOfProcessBreakdown = stageOfProcessBreakdown,
            NccMerpBreakdown = nccMerpBreakdown,
            ReportedSeverityBreakdown = reportedSeverityBreakdown,
            AdrSeverityBreakdown = adrSeverityBreakdown,
            WhoUmcCausalityBreakdown = whoUmcCausalityBreakdown,
            SeriousnessCriteriaBreakdown = seriousnessCriteriaBreakdown,
            ReportingSourceBreakdown = reportingSourceBreakdown,
            SeriousAdrCount = seriousAdrCount,
            FieldUsage = alertRuleSummary.FieldUsage,
            TotalAlertRules = alertRuleSummary.TotalRules,
            ActiveAlertRules = alertRuleSummary.ActiveRules,
            CriticalAlertRules = alertRuleSummary.CriticalRules,
            AlertsTriggeredLast24Hours = alertRuleSummary.AlertsTriggeredLast24Hours,
        };
    }
}
