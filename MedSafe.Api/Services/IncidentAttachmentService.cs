using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;

namespace MedSafeAPI.Services;

public class IncidentAttachmentService : IIncidentAttachmentService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly string _storageRoot;

    public IncidentAttachmentService(AppDbContext db, ICurrentUserService currentUser, IConfiguration configuration, IWebHostEnvironment env)
    {
        _db = db;
        _currentUser = currentUser;

        var configuredRoot = configuration["FileStorage:RootPath"] ?? "App_Data\\IncidentAttachments";
        _storageRoot = Path.IsPathRooted(configuredRoot) ? configuredRoot : Path.Combine(env.ContentRootPath, configuredRoot);
    }

    public async Task<IncidentReportAttachmentDto> UploadAsync(int incidentReportId, IFormFile file, string? category, string? description, CancellationToken cancellationToken)
    {
        var incidentExists = await _db.IncidentReports.AnyAsync(x => x.Id == incidentReportId, cancellationToken);
        if (!incidentExists)
            throw new KeyNotFoundException("Incident report not found.");

        if (file.Length == 0)
            throw new ValidationException("File is empty.");

        if (file.Length > MaxFileSizeBytes)
            throw new ValidationException("Maximum file size is 10 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ValidationException("Invalid attachment type. Allowed: PDF, JPG, JPEG, PNG.");

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativeFolder = Path.Combine("incidents", incidentReportId.ToString(), "attachments");
        var folder = Path.Combine(_storageRoot, relativeFolder);
        Directory.CreateDirectory(folder);

        var fullPath = Path.Combine(folder, storedFileName);

        await using (var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
        }

        var sha256Hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(fullPath, cancellationToken)));

        var storageKey = Path.Combine(relativeFolder, storedFileName).Replace('\\', '/');

        var attachment = new IncidentReportAttachment
        {
            IncidentReportId = incidentReportId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StorageKey = storageKey,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FileSizeBytes = file.Length,
            Sha256Hash = sha256Hash,
            UploadedByUserId = _currentUser.UserId,
            UploadedAt = DateTime.UtcNow,
            IsDeleted = false,
            Category = category?.Trim(),
            Description = description?.Trim()
        };

        _db.IncidentReportAttachments.Add(attachment);
        await _db.SaveChangesAsync(cancellationToken);

        return new IncidentReportAttachmentDto
        {
            Id = attachment.Id,
            OriginalFileName = attachment.OriginalFileName,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            UploadedAt = attachment.UploadedAt,
            UploadedByUserId = attachment.UploadedByUserId,
            Category = attachment.Category,
            Description = attachment.Description
        };
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(int incidentReportId, int attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await _db.IncidentReportAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.IncidentReportId == incidentReportId && !a.IsDeleted, cancellationToken);

        if (attachment == null)
            throw new KeyNotFoundException("Attachment not found.");

        var fullPath = Path.Combine(_storageRoot, attachment.StorageKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new KeyNotFoundException("Attachment file is missing from storage.");

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        return (stream, attachment.ContentType, attachment.OriginalFileName);
    }
}
