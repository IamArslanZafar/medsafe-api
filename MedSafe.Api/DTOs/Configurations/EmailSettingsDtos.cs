using System.ComponentModel.DataAnnotations;

namespace MedSafeAPI.DTOs;

public class EmailSettingsDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    // The real password never round-trips to the client — this just tells the
    // form whether one is already on file, so it can show a placeholder instead.
    public bool PasswordSet { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }
}

public class EmailSettingsUpdateDto
{
    [Required] public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    // Leave null/blank to keep the currently-stored password unchanged.
    public string? Password { get; set; }
    [Required, EmailAddress] public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
}
