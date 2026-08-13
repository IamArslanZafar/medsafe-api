using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(string? facilityUnit, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken);
}
