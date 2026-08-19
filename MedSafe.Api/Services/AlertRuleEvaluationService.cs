using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;

namespace MedSafeAPI.Services;

// Runs automatically after an incident report is successfully committed — no
// dedicated frontend API. Loads every enabled dynamic Alert Rule, evaluates its
// conditions (ALL/ANY match mode) against the just-submitted report, and — for
// every rule that matches — records one AlertTriggerHistory (via
// IAlertTriggerService) plus one IncidentNotification per configured recipient.
public class AlertRuleEvaluationService : IAlertRuleEvaluationService
{
    private readonly AppDbContext _db;
    private readonly IAlertTriggerService _alertTriggerService;
    private readonly ILogger<AlertRuleEvaluationService> _logger;

    public AlertRuleEvaluationService(AppDbContext db, IAlertTriggerService alertTriggerService, ILogger<AlertRuleEvaluationService> logger)
    {
        _db = db;
        _alertTriggerService = alertTriggerService;
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

            var results = new List<ConditionEvaluationResult>();
            foreach (var condition in rule.Conditions)
                results.Add(await EvaluateConditionAsync(report, condition, _db, cancellationToken));

            var matched = rule.MatchMode?.Code == "ANY" ? results.Any(x => x.Matched) : results.All(x => x.Matched);
            if (!matched)
                continue;

            var recipients = rule.Recipients
                .Where(x => x.IsActive)
                .GroupBy(x => x.RecipientUserId)
                .Select(x => new AlertTriggerRecipientCommand { UserId = x.Key, RecipientTypeId = x.First().RecipientTypeId })
                .ToList();

            if (recipients.Count == 0)
                continue;

            await _alertTriggerService.CreateAsync(new CreateAlertTriggerCommand
            {
                AlertRuleId = rule.Id,
                IncidentReportId = report.Id,
                UrgencyId = rule.UrgencyId,
                TriggerSource = "REPORT_SUBMISSION",
                DedupeKey = $"REPORT_SUBMISSION:{rule.Id}:{report.Id}",
                ConditionSummary = BuildConditionSummary(results, rule.MatchMode?.Code),
                MatchedConditionSnapshot = JsonSerializer.Serialize(results),
                Title = rule.NotificationTitle ?? "Medication Safety Alert",
                Message = rule.NotificationMessage ?? string.Empty,
                CreatedByUserId = report.SubmittedByUserId,
                Recipients = recipients
            }, cancellationToken);
        }
    }

    private static string BuildConditionSummary(List<ConditionEvaluationResult> results, string? matchModeCode)
    {
        var parts = results.Select(r => $"{r.FieldName} {FriendlyOperator(r.OperatorCode)} {r.ExpectedValue}");
        var joiner = matchModeCode == "ANY" ? " OR " : " AND ";
        return string.Join(joiner, parts);
    }

    private static string FriendlyOperator(string operatorCode) => operatorCode switch
    {
        "EQUALS" => "=",
        "NOT_EQUALS" => "!=",
        "IN" => "IN",
        "NOT_IN" => "NOT IN",
        "AT_LEAST" => ">=",
        "AT_MOST" => "<=",
        "CONTAINS_ANY" => "CONTAINS ANY OF",
        "CONTAINS_ALL" => "CONTAINS ALL OF",
        _ => operatorCode
    };

    private static async Task<ConditionEvaluationResult> EvaluateConditionAsync(IncidentReport report, AlertRuleCondition condition, AppDbContext db, CancellationToken cancellationToken)
    {
        var fieldCode = condition.ConditionField.Code;
        var operatorCode = condition.Operator.Code;
        var result = new ConditionEvaluationResult
        {
            FieldCode = fieldCode,
            FieldName = condition.ConditionField.Name,
            OperatorCode = operatorCode,
        };

        switch (fieldCode)
        {
            case "REPORT_TYPE":
                result.ActualValue = report.ReportType;
                result.ExpectedValue = JoinText(condition);
                result.Matched = EvaluateText(report.ReportType, operatorCode, condition);
                break;
            case "HARM_LEVEL":
                result.ActualValue = report.HarmLevelCode;
                result.ExpectedValue = JoinText(condition);
                result.Matched = EvaluateHarmLevel(report.HarmLevelCode, operatorCode, condition);
                break;
            case "ERROR_CATEGORY":
                result.ActualValue = await ResolveActualLabelAsync(db, fieldCode, report.ErrorCategoryId, cancellationToken);
                result.ExpectedValue = await JoinLookupLabelsAsync(db, fieldCode, condition, cancellationToken);
                result.Matched = EvaluateLookup(report.ErrorCategoryId, operatorCode, condition);
                break;
            case "STAGE_OF_PROCESS":
                result.ActualValue = await ResolveActualLabelAsync(db, fieldCode, report.StageOfProcessId, cancellationToken);
                result.ExpectedValue = await JoinLookupLabelsAsync(db, fieldCode, condition, cancellationToken);
                result.Matched = EvaluateLookup(report.StageOfProcessId, operatorCode, condition);
                break;
            case "PATIENT_OUTCOME":
                result.ActualValue = await ResolveActualLabelAsync(db, fieldCode, report.PatientOutcomeId, cancellationToken);
                result.ExpectedValue = await JoinLookupLabelsAsync(db, fieldCode, condition, cancellationToken);
                result.Matched = EvaluateLookup(report.PatientOutcomeId, operatorCode, condition);
                break;
            case "SERIOUSNESS_CRITERIA":
                result.ActualValue = await JoinLookupLabelsAsync(db, fieldCode, report.SeriousnessCriteria.Select(x => x.SeriousnessCriterionId), cancellationToken);
                result.ExpectedValue = await JoinLookupLabelsAsync(db, fieldCode, condition, cancellationToken);
                result.Matched = EvaluateMultiLookup(report.SeriousnessCriteria.Select(x => x.SeriousnessCriterionId), operatorCode, condition);
                break;
            case "CONTRIBUTING_FACTOR":
                result.ActualValue = await JoinLookupLabelsAsync(db, fieldCode, report.ContributingFactors.Select(x => x.ContributingFactorId), cancellationToken);
                result.ExpectedValue = await JoinLookupLabelsAsync(db, fieldCode, condition, cancellationToken);
                result.Matched = EvaluateMultiLookup(report.ContributingFactors.Select(x => x.ContributingFactorId), operatorCode, condition);
                break;
            case "INCIDENT_LOCATION":
                result.ActualValue = report.IncidentLocation;
                result.ExpectedValue = JoinText(condition);
                result.Matched = EvaluateText(report.IncidentLocation, operatorCode, condition);
                break;
            case "SUSPECTED_CAUSALITY":
                result.ActualValue = report.SuspectedCausality;
                result.ExpectedValue = JoinText(condition);
                result.Matched = EvaluateText(report.SuspectedCausality, operatorCode, condition);
                break;
            default:
                result.ExpectedValue = JoinText(condition);
                result.Matched = false;
                break;
        }

        return result;
    }

    private static string JoinText(AlertRuleCondition condition) =>
        string.Join(", ", condition.Values.Select(v => v.TextValue ?? v.LookupValueId?.ToString() ?? string.Empty));

    private static async Task<string?> ResolveActualLabelAsync(AppDbContext db, string fieldCode, int? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return null;
        return await AlertConditionLabelResolver.ResolveNameAsync(db, fieldCode, id.Value, cancellationToken) ?? id.Value.ToString();
    }

    private static async Task<string> JoinLookupLabelsAsync(AppDbContext db, string fieldCode, AlertRuleCondition condition, CancellationToken cancellationToken) =>
        await JoinLookupLabelsAsync(db, fieldCode, condition.Values.Where(v => v.LookupValueId.HasValue).Select(v => v.LookupValueId!.Value), cancellationToken);

    private static async Task<string> JoinLookupLabelsAsync(AppDbContext db, string fieldCode, IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var labels = new List<string>();
        foreach (var id in ids)
            labels.Add(await AlertConditionLabelResolver.ResolveNameAsync(db, fieldCode, id, cancellationToken) ?? id.ToString());
        return string.Join(", ", labels);
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
