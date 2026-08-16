using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public class DashboardService : IDashboardService
{
    // Mirrors the frontend's fixed 5-bucket process-stage taxonomy (see PROCESS_STAGES
    // in the frontend's taxonomy.js) — the lookup table's Name is free text, so bucket
    // by substring match the same way the frontend does.
    private static readonly string[] StageKeys = ["Prescribing", "Transcribing", "Dispensing", "Administration", "Monitoring"];
    private static readonly string[] SeverityCodes = ["A", "B", "C", "D", "E", "F", "G", "H", "I"];
    private static readonly string[] HarmCodes = ["E", "F", "G", "H", "I"];

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

        var stageNames = await _db.StageOfProcesses.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);
        var errorCategoryNames = await _db.ErrorCategories.AsNoTracking()
            .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);

        var total = reports.Count;
        var harmCount = reports.Count(r => HarmCodes.Contains(r.HarmLevelCode));
        var nearMissCount = reports.Count(r => r.ReportType == "Near Miss");
        var adrCount = reports.Count(r => r.ReportType == "ADR");
        var errorCount = reports.Count(r => r.ReportType == "Medication Error");
        var pendingReview = reports.Count(r => r.ReportStatus == "Pending" || r.ReportStatus == "UnderReview");
        var closedCount = reports.Count(r => r.ReportStatus == "Closed");

        var stageLookup = StageKeys.ToDictionary(k => k, _ => 0, StringComparer.OrdinalIgnoreCase);
        foreach (var r in reports)
        {
            if (r.StageOfProcessId is null || !stageNames.TryGetValue(r.StageOfProcessId.Value, out var name))
                continue;
            var matched = StageKeys.FirstOrDefault(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (matched != null) stageLookup[matched]++;
        }

        var severityLookup = SeverityCodes.ToDictionary(c => c, _ => 0);
        foreach (var r in reports)
            if (severityLookup.ContainsKey(r.HarmLevelCode))
                severityLookup[r.HarmLevelCode]++;

        var topMedications = reports
            .SelectMany(r => r.Medications)
            .Where(m => !string.IsNullOrWhiteSpace(m.MedicationName))
            .GroupBy(m => m.MedicationName)
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var errorTypes = reports
            .Where(r => r.ErrorCategoryId.HasValue && errorCategoryNames.ContainsKey(r.ErrorCategoryId.Value))
            .GroupBy(r => errorCategoryNames[r.ErrorCategoryId!.Value])
            .Select(g => new DashboardNamedCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToList();

        var alertRuleSummary = await _alertRuleService.GetSummaryAsync(cancellationToken);

        var dayCount = (actualEndDate - actualStartDate).Days + 1;
        var days = Enumerable.Range(0, dayCount).Select(i => actualStartDate.AddDays(i)).ToList();
        var trend = new DashboardTrendDto
        {
            Labels = days.Select(d => d.ToString("dd MMM")).ToList(),
            MedicationErrors = days.Select(d => reports.Count(r => r.ReportType == "Medication Error" && r.SubmittedAt.Date == d)).ToList(),
            NearMisses = days.Select(d => reports.Count(r => r.ReportType == "Near Miss" && r.SubmittedAt.Date == d)).ToList(),
            Adr = days.Select(d => reports.Count(r => r.ReportType == "ADR" && r.SubmittedAt.Date == d)).ToList(),
            HarmEvents = days.Select(d => reports.Count(r => HarmCodes.Contains(r.HarmLevelCode) && r.SubmittedAt.Date == d)).ToList(),
        };

        return new DashboardSummaryDto
        {
            TotalReports = total,
            NearMissRatePct = total == 0 ? 0 : (int)Math.Round(nearMissCount * 100.0 / total),
            HarmEvents = harmCount,
            AdrReactions = adrCount,
            PendingReview = pendingReview,
            Breakdown = new DashboardBreakdownDto
            {
                MedicationErrors = errorCount,
                NearMissReports = nearMissCount,
                AdrReactions = adrCount,
                ClosedResolved = closedCount,
                PendingReview = pendingReview,
            },
            Trend = trend,
            StageOfProcess = StageKeys.Select(k => new DashboardNamedCountDto { Name = k, Count = stageLookup[k] }).ToList(),
            Severity = SeverityCodes.Select(c => new DashboardNamedCountDto { Name = c, Count = severityLookup[c] }).ToList(),
            TopMedications = topMedications,
            ErrorTypesByNature = errorTypes,
            FieldUsage = alertRuleSummary.FieldUsage,
            TotalAlertRules = alertRuleSummary.TotalRules,
            ActiveAlertRules = alertRuleSummary.ActiveRules,
            CriticalAlertRules = alertRuleSummary.CriticalRules,
            AlertsTriggeredLast24Hours = alertRuleSummary.AlertsTriggeredLast24Hours,
        };
    }
}
