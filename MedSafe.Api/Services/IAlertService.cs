namespace MedSafeAPI.Services;

public interface IAlertService
{
    Task EvaluateIncidentAsync(int incidentReportId, CancellationToken cancellationToken);
}
