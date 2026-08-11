using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public interface IIncidentAttachmentService
{
    Task<IncidentReportAttachmentDto> UploadAsync(int incidentReportId, IFormFile file, CancellationToken cancellationToken);
    Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(int incidentReportId, int attachmentId, CancellationToken cancellationToken);
}
