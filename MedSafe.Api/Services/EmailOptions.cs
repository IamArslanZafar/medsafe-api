namespace MedSafeAPI.Services;

// Bound from the "Email" config section. Left blank in appsettings.json until
// real SMTP credentials are available — EmailService treats a blank Host as
// "not configured yet" and skips sending rather than throwing.
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "MedSafe";
    public bool UseSsl { get; set; } = true;
}
