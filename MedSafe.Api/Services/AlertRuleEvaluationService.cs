using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;

namespace MedSafeAPI.Services;

// Runs automatically after an incident report is successfully committed — no
// dedicated frontend API. Loads every enabled dynamic Alert Rule, evaluates its
// conditions (ALL/ANY match mode) against the just-submitted report, and creates
// one IncidentNotification per configured recipient for every rule that matches.
public class AlertRuleEvaluationService : IAlertRuleEvaluationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AlertRuleEvaluationService> _logger;

    public AlertRuleEvaluationService(AppDbContext db, ILogger<AlertRuleEvaluationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EvaluateIncidentAsync(int incidentReportId, CancellationToken cancellationToken)
    {
        var report = await _db.IncidentReports
            .AsNoTracking()
            .Include(x => x.ContributingFactors)
            .Include(x => x.SeriousnessCriteria)
            .FirstOrDefaultAsync(x => x.Id == incidentReportId, cancellationToken);

        if (report == null)
        {
            _logger.LogWarning("Alert evaluation skipped because incident {IncidentId} was not found.", incidentReportId);
            return;
        }

        var emailMethodId = await _db.NotificationMethods
            .Where(x => x.Code == "EMAIL" && x.IsActive)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        // MatchModeId != null skips legacy rules that don't use the new dynamic
        // condition-builder structure yet.
        var rules = await _db.AlertRules
            .Where(x => x.Enabled && !x.IsDeleted && x.MatchModeId != null)
            .Include(x => x.MatchMode)
            .Include(x => x.Conditions).ThenInclude(x => x.ConditionField)
            .Include(x => x.Conditions).ThenInclude(x => x.Operator)
            .Include(x => x.Conditions).ThenInclude(x => x.Values)
            .Include(x => x.Recipients).ThenInclude(x => x.RecipientUser)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (rule.Conditions.Count == 0)
                continue;

            var results = rule.Conditions.Select(condition => EvaluateCondition(report, condition)).ToList();
            var matched = rule.MatchMode?.Code == "ANY" ? results.Any(x => x) : results.All(x => x);

            if (!matched)
                continue;

            await CreateNotificationsAsync(report, rule, emailMethodId, cancellationToken);
            rule.LastTriggered = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateNotificationsAsync(IncidentReport report, AlertRule rule, int? emailMethodId, CancellationToken cancellationToken)
    {
        var recipients = rule.Recipients
            .Where(x => x.IsActive)
            .GroupBy(x => x.RecipientUserId)
            .Select(x => x.First())
            .ToList();

        if (recipients.Count == 0)
            return;

        var existingRecipientIds = await _db.IncidentNotifications
            .Where(x => x.IncidentReportId == report.Id && x.AlertRuleId == rule.Id && x.RecipientUserId != null)
            .Select(x => x.RecipientUserId!.Value)
            .ToListAsync(cancellationToken);
        var existingSet = existingRecipientIds.ToHashSet();

        foreach (var recipient in recipients)
        {
            if (existingSet.Contains(recipient.RecipientUserId))
                continue;

            var now = DateTime.UtcNow;
            _db.IncidentNotifications.Add(new IncidentNotification
            {
                IncidentReportId = report.Id,
                AlertRuleId = rule.Id,
                RecipientUserId = recipient.RecipientUserId,
                NotificationTypeId = recipient.RecipientTypeId,
                UrgencyId = rule.UrgencyId,
                NotificationMethodId = emailMethodId,
                Status = "PENDING",
                PersonName = recipient.RecipientUser.Name,
                Title = rule.NotificationTitle,
                Message = rule.NotificationMessage,
                IsAutomatic = true,
                IsRead = false,
                ReadAt = null,
                NotifiedAt = now,
                // Current schema requires CreatedBy — use the report submitter.
                CreatedBy = report.SubmittedByUserId,
                CreatedDate = now
            });
        }
    }

    private static bool EvaluateCondition(IncidentReport report, AlertRuleCondition condition)
    {
        var fieldCode = condition.ConditionField.Code;
        var operatorCode = condition.Operator.Code;

        return fieldCode switch
        {
            "REPORT_TYPE" => EvaluateText(report.ReportType, operatorCode, condition),
            "HARM_LEVEL" => EvaluateHarmLevel(report.HarmLevelCode, operatorCode, condition),
            "ERROR_CATEGORY" => EvaluateLookup(report.ErrorCategoryId, operatorCode, condition),
            "STAGE_OF_PROCESS" => EvaluateLookup(report.StageOfProcessId, operatorCode, condition),
            "PATIENT_OUTCOME" => EvaluateLookup(report.PatientOutcomeId, operatorCode, condition),
            "SERIOUSNESS_CRITERIA" => EvaluateMultiLookup(report.SeriousnessCriteria.Select(x => x.SeriousnessCriterionId), operatorCode, condition),
            "CONTRIBUTING_FACTOR" => EvaluateMultiLookup(report.ContributingFactors.Select(x => x.ContributingFactorId), operatorCode, condition),
            "INCIDENT_LOCATION" => EvaluateText(report.IncidentLocation, operatorCode, condition),
            "SUSPECTED_CAUSALITY" => EvaluateText(report.SuspectedCausality, operatorCode, condition),
            _ => false
        };
    }

    // Handles REPORT_TYPE, INCIDENT_LOCATION, SUSPECTED_CAUSALITY.
    private static bool EvaluateText(string? actualValue, string operatorCode, AlertRuleCondition condition)
    {
        if (string.IsNullOrWhiteSpace(actualValue))
            return false;

        var expectedValues = condition.Values
            .Where(x => !string.IsNullOrWhiteSpace(x.TextValue))
            .Select(x => x.TextValue!.Trim())
            .ToList();

        bool IsMatch(string value) => string.Equals(actualValue.Trim(), value, StringComparison.OrdinalIgnoreCase);

        return operatorCode switch
        {
            "EQUALS" => expectedValues.Any(IsMatch),
            "NOT_EQUALS" => expectedValues.All(x => !IsMatch(x)),
            "IN" => expectedValues.Any(IsMatch),
            "NOT_IN" => expectedValues.All(x => !IsMatch(x)),
            _ => false
        };
    }

    // Handles ERROR_CATEGORY, STAGE_OF_PROCESS, PATIENT_OUTCOME.
    private static bool EvaluateLookup(int? actualValue, string operatorCode, AlertRuleCondition condition)
    {
        if (!actualValue.HasValue)
            return false;

        var expectedValues = condition.Values
            .Where(x => x.LookupValueId.HasValue)
            .Select(x => x.LookupValueId!.Value)
            .ToHashSet();

        return operatorCode switch
        {
            "EQUALS" => expectedValues.Contains(actualValue.Value),
            "NOT_EQUALS" => !expectedValues.Contains(actualValue.Value),
            "IN" => expectedValues.Contains(actualValue.Value),
            "NOT_IN" => !expectedValues.Contains(actualValue.Value),
            _ => false
        };
    }

    private static int GetHarmRank(string? code) => code?.Trim().ToUpperInvariant() switch
    {
        "A" => 1,
        "B" => 2,
        "C" => 3,
        "D" => 4,
        "E" => 5,
        "F" => 6,
        "G" => 7,
        "H" => 8,
        "I" => 9,
        _ => 0
    };

    // A < B < C < D < E < F < G < H < I — AT_LEAST/AT_MOST compare rank, not text.
    private static bool EvaluateHarmLevel(string actualValue, string operatorCode, AlertRuleCondition condition)
    {
        var expectedValues = condition.Values
            .Where(x => !string.IsNullOrWhiteSpace(x.TextValue))
            .Select(x => x.TextValue!.Trim().ToUpperInvariant())
            .ToList();

        if (expectedValues.Count == 0)
            return false;

        var actualRank = GetHarmRank(actualValue);
        if (actualRank == 0)
            return false;

        var actualUpper = actualValue.Trim().ToUpperInvariant();

        return operatorCode switch
        {
            "EQUALS" => expectedValues.Contains(actualUpper),
            "NOT_EQUALS" => !expectedValues.Contains(actualUpper),
            "IN" => expectedValues.Contains(actualUpper),
            "NOT_IN" => !expectedValues.Contains(actualUpper),
            "AT_LEAST" => actualRank >= GetHarmRank(expectedValues.First()),
            "AT_MOST" => actualRank <= GetHarmRank(expectedValues.First()),
            _ => false
        };
    }

    // Handles SERIOUSNESS_CRITERIA, CONTRIBUTING_FACTOR (multi-select fields).
    private static bool EvaluateMultiLookup(IEnumerable<int> actualValues, string operatorCode, AlertRuleCondition condition)
    {
        var actual = actualValues.ToHashSet();
        var expected = condition.Values
            .Where(x => x.LookupValueId.HasValue)
            .Select(x => x.LookupValueId!.Value)
            .ToHashSet();

        if (expected.Count == 0)
            return false;

        return operatorCode switch
        {
            "CONTAINS_ANY" => expected.Any(actual.Contains),
            "CONTAINS_ALL" => expected.All(actual.Contains),
            _ => false
        };
    }
}
