namespace MedSafeAPI.Services;

public interface IAlertRuleEvaluationService
{
    Task EvaluateIncidentAsync(int incidentReportId, CancellationToken cancellationToken);
}
