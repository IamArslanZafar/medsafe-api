namespace MedSafe.Models;

// One row per user — the ORCID field from ResearchPublications.jsx's separate
// `orcid` useState. Deliberately its own tiny table (not a column added to the
// Users identity table) since it's specific to this module. Orcid can be set
// two ways: typed in manually (Verified stays false) or via the real ORCID
// OAuth "Connect" flow, which fills Name/Verified too since that value came
// straight back from ORCID's own token endpoint, not user-typed text.
public class ResearcherProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Orcid { get; set; } = string.Empty;
    public string? Name { get; set; }
    public bool Verified { get; set; }
}
