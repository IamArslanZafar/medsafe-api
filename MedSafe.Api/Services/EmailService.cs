using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace MedSafeAPI.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(SendEmailRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
            throw new InvalidOperationException("SMTP is not configured — set the Email:Host and Email:FromAddress settings.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(request.ToName, request.ToEmail));
        message.Subject = request.Subject;

        var builder = new BodyBuilder { HtmlBody = request.HtmlBody };
        foreach (var attachment in request.Attachments)
            builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Email sent to {ToEmail} — subject: {Subject}", request.ToEmail, request.Subject);
    }
}
