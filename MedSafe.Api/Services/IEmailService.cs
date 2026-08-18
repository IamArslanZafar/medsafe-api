namespace MedSafeAPI.Services;

public sealed class EmailAttachment
{
    public string FileName { get; init; } = null!;
    public string ContentType { get; init; } = "application/pdf";
    public byte[] Content { get; init; } = [];
}

public sealed class SendEmailRequest
{
    public string ToEmail { get; init; } = null!;
    public string ToName { get; init; } = null!;
    public string Subject { get; init; } = null!;
    public string HtmlBody { get; init; } = null!;
    public List<EmailAttachment> Attachments { get; init; } = [];
}

// Knows only how to deliver a prepared email over SMTP — nothing about alert
// rules, incident reports, or notification records. Keeps SMTP concerns out
// of the domain services that decide *what* to send.
public interface IEmailService
{
    Task SendAsync(SendEmailRequest request, CancellationToken cancellationToken);
}
