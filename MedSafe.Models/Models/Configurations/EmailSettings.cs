namespace MedSafe.Models;

// Single-row table (Id is always 1) holding the SMTP credentials EmailService
// sends through. Previously hardcoded in appsettings.json's "Email" section —
// moved here so an Admin can view/rotate them from the Email Settings page
// without a redeploy.
public class EmailSettings
{
    public int Id { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "QTCMRS";
    public bool UseSsl { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedByUserId { get; set; }
}
