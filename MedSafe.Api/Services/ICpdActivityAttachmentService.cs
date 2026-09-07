using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public interface ICpdActivityAttachmentService
{
    Task<AttachmentUploadDto> UploadAsync(IFormFile file, CancellationToken cancellationToken);
    Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(int attachmentId, CancellationToken cancellationToken);
}
