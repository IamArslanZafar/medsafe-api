namespace MedSafe.Models;

// One row per user — just the ORCID field from ResearchPublications.jsx's
// separate `orcid` useState. Deliberately its own tiny table (not a column
// added to the Users identity table) since it's specific to this module.
public class ResearcherProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Orcid { get; set; } = string.Empty;
}
