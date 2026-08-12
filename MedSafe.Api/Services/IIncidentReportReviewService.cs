using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public interface IIncidentReportReviewService
{
    Task<StartReviewResponse> StartReviewAsync(int incidentReportId, CancellationToken cancellationToken);
    Task<SignOffReviewResponse> SignOffReviewAsync(int incidentReportId, SignOffReviewRequest request, CancellationToken cancellationToken);
    Task<IncidentReportReviewDto?> GetReviewAsync(int incidentReportId, CancellationToken cancellationToken);
    Task<List<ActionOwnerOptionDto>> GetActionOwnersAsync(CancellationToken cancellationToken);
}
