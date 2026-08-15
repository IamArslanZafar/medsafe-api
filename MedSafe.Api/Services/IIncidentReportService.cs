using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public interface IIncidentReportService
{
    Task<SubmitIncidentReportResponse> SubmitAsync(SubmitIncidentReportRequest request, CancellationToken cancellationToken);
    Task<IncidentReportDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<IncidentReportDto>> GetListAsync(IncidentReportListRequest request, CancellationToken cancellationToken);
    Task<IncidentReportSummaryDto> GetSummaryAsync(IncidentReportListRequest request, CancellationToken cancellationToken);

    // Admin, the submitter, anyone the report notified, and the assigned reviewer can
    // all view/act on a report — notifications would otherwise point recipients at a
    // report their role-based access then blocks with a 403.
    Task<bool> CanCurrentUserAccessAsync(int incidentReportId, CancellationToken cancellationToken);
}
