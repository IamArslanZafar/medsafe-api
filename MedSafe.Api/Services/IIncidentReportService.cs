using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public interface IIncidentReportService
{
    Task<SubmitIncidentReportResponse> SubmitAsync(SubmitIncidentReportRequest request, CancellationToken cancellationToken);
    Task<IncidentReportDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<IncidentReportDto>> GetListAsync(CancellationToken cancellationToken);
    Task<IncidentReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
