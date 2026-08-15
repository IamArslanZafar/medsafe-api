using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public interface IAlertRuleService
{
    Task<CreateAlertRuleResponse> CreateAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken);
    Task<List<AlertRuleListItemDto>> GetAllAsync(string? search, bool? isEnabled, int? urgencyId, CancellationToken cancellationToken);
    Task<AlertRuleDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<UpdateAlertRuleResponse> UpdateAsync(int id, UpdateAlertRuleRequest request, CancellationToken cancellationToken);
    Task<AlertRuleSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
    // Shared with DashboardService so both summary endpoints report the exact
    // same per-field rule counts from one query implementation.
    Task<List<AlertRuleFieldUsageDto>> GetFieldUsageAsync(CancellationToken cancellationToken);
}
