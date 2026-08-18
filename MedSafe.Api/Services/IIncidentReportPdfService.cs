namespace MedSafeAPI.Services;

public interface IIncidentReportPdfService
{
    // Returns the generated PDF as raw bytes — nothing is written to disk, so
    // the email pipeline can attach it directly without depending on a browser
    // or leaving temporary files behind.
    Task<byte[]> GenerateAsync(int incidentReportId, CancellationToken cancellationToken);
}
