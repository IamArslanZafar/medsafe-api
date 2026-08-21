using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace MedSafeAPI.Services;

public class EmailService : IEmailService
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmailService> _logger;

    public EmailService(AppDbContext db, ILogger<EmailService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(SendEmailRequest request, CancellationToken cancellationToken)
    {
        var settings = await _db.EmailSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings == null || string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.FromAddress))
            throw new InvalidOperationException("SMTP is not configured — set it up under Settings > Email Settings.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(new MailboxAddress(request.ToName, request.ToEmail));
        message.Subject = request.Subject;

        var builder = new BodyBuilder { HtmlBody = request.HtmlBody };
        foreach (var attachment in request.Attachments)
            builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(settings.Host, settings.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Username))
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Email sent to {ToEmail} — subject: {Subject}", request.ToEmail, request.Subject);
    }
}
