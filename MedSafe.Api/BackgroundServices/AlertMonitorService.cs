using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;

namespace MedSafeAPI.BackgroundServices;

public class AlertMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertMonitorService> _logger;

    public AlertMonitorService(IServiceScopeFactory scopeFactory, ILogger<AlertMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAlertsAsync();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CheckAlertsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // RULE-004: Same error category >= 3 times in 7 days
        var since = DateTime.UtcNow.AddDays(-7);
        var repeated = await db.IncidentReports
            .Where(r => r.SubmittedAt >= since && r.ErrorCategoryId != null)
            .GroupBy(r => r.ErrorCategoryId)
            .Where(g => g.Count() >= 3)
            .Select(g => g.Key)
            .ToListAsync();

        if (repeated.Any())
            _logger.LogWarning("RULE-004 triggered: High-frequency error categories: {Categories}", string.Join(", ", repeated));

        // RULE-005: Reports awaiting review > 48 hrs
        var stale = await db.IncidentReports
            .Where(r => r.ReportStatus == "Submitted" && r.SubmittedAt < DateTime.UtcNow.AddHours(-48))
            .CountAsync();

        if (stale > 0)
            _logger.LogWarning("RULE-005 triggered: {Count} reports pending review > 48 hours", stale);
    }
}
