using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DashboardSummaryRequest request, CancellationToken cancellationToken);
}
