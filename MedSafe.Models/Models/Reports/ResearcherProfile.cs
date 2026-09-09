namespace MedSafe.Models;

// One row per user — the ORCID field from ResearchPublications.jsx's separate
// `orcid` useState. Deliberately its own tiny table (not a column added to the
// Users identity table) since it's specific to this module. Orcid can be set
// two ways: typed in manually (Verified stays false) or via the real ORCID
// OAuth "Connect" flow, which fills Name/Verified too since that value came
// straight back from ORCID's own token endpoint, not user-typed text.
//
// AccessToken/RefreshToken/TokenExpiresAt persist the OAuth connection itself
// (not just the identity it verified) so the app can keep calling ORCID's API
// on this user's behalf later without asking them to "Connect" again — see
// ResearchPublicationsController.RefreshOrcidTokenIfNeededAsync, which renews
// AccessToken via the refresh_token grant once TokenExpiresAt is close, and is
// only null for a profile that was set via the manual-entry path (never went
// through the OAuth exchange) or hasn't connected at all.
public class ResearcherProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Orcid { get; set; } = string.Empty;
    public string? Name { get; set; }
    public bool Verified { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}
