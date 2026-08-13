using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // POST /api/dashboard/summary  { facilityUnit?, startDate?, endDate?, medicationName?,
    // errorCategoryId?, stageOfProcessId?, patientOutcomeId? } — every filter is optional
    // and combines (AND) with the others. facilityUnit omitted or "All Units" means no unit
    // filter. startDate/endDate are each independently optional — same per-parameter fallback
    // as SystemMonitorControl's sp_GetComputerDailySummaryPagedWithStats: whichever one is
    // omitted defaults to the matching end of the previous complete calendar week (last
    // Monday..Sunday). The whole response (KPIs, breakdown, stage/severity/drug/error-type
    // buckets, trend) is scoped to these filters, not just the trend chart.
    // Nurse sees only their own reports, same visibility rule as GET /incident-reports.
    [HttpPost("summary")]
    public async Task<IActionResult> GetSummary([FromBody] DashboardSummaryRequest request, CancellationToken cancellationToken)
    {
        var summary = await _dashboardService.GetSummaryAsync(request, cancellationToken);
        return Ok(summary);
    }
}
