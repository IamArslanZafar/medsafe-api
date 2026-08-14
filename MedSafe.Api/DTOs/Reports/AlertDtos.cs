namespace MedSafeAPI.DTOs;

// ── Alert Rule Builder — lookup DTOs ────────────────────────────────────

public sealed class AlertBuilderOperatorDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class AlertBuilderFieldDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsMultiValue { get; set; }
    public List<AlertBuilderOperatorDto> Operators { get; set; } = [];
}

public sealed class AlertBuilderValueDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed class AlertRuleMatchModeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

// ── Alert Rule Builder — create DTOs ────────────────────────────────────

public sealed class AlertRuleValueRequest
{
    public int? LookupValueId { get; set; }
    public string? TextValue { get; set; }
}

public sealed class CreateAlertRuleConditionRequest
{
    public int ConditionFieldId { get; set; }
    public int OperatorId { get; set; }
    public List<AlertRuleValueRequest> Values { get; set; } = [];
}

public sealed class CreateAlertRuleRecipientRequest
{
    public int RecipientTypeId { get; set; }
    public int RecipientUserId { get; set; }
}

public sealed class CreateAlertRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public int MatchModeId { get; set; }
    public int UrgencyId { get; set; }
    public string NotificationTitle { get; set; } = string.Empty;
    public string NotificationMessage { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<CreateAlertRuleConditionRequest> Conditions { get; set; } = [];
    public List<CreateAlertRuleRecipientRequest> Recipients { get; set; } = [];
}

public sealed class CreateAlertRuleResponse
{
    public int Id { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int ConditionCount { get; set; }
    public int RecipientCount { get; set; }
}

// ── Alert Rule Builder — list / detail / update / summary DTOs ─────────

public sealed class AlertRuleUrgencyDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class AlertRuleListItemDto
{
    public int Id { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AlertRuleMatchModeDto? MatchMode { get; set; }
    public AlertRuleUrgencyDto? Urgency { get; set; }
    public string? NotificationTitle { get; set; }
    public string? NotificationMessage { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public int ConditionCount { get; set; }
    public int RecipientCount { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AlertRuleConditionValueDetailDto
{
    public int Id { get; set; }
    public int? LookupValueId { get; set; }
    public string? TextValue { get; set; }
    // Resolved human-readable label — e.g. "Wrong Drug" for a lookupValueId, or
    // "Category E" for a HARM_LEVEL textValue — so the frontend doesn't need to
    // separately re-resolve ids when showing an existing rule.
    public string DisplayValue { get; set; } = string.Empty;
}

public sealed class AlertRuleConditionDetailDto
{
    public int Id { get; set; }
    public int ConditionFieldId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public int OperatorId { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public List<AlertRuleConditionValueDetailDto> Values { get; set; } = [];
}

public sealed class AlertRuleRecipientDetailDto
{
    public int Id { get; set; }
    public int RecipientTypeId { get; set; }
    public string RecipientTypeCode { get; set; } = string.Empty;
    public string RecipientTypeName { get; set; } = string.Empty;
    public int RecipientUserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Unit { get; set; }
}

public sealed class AlertRuleDetailDto
{
    public int Id { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AlertRuleMatchModeDto? MatchMode { get; set; }
    public AlertRuleUrgencyDto? Urgency { get; set; }
    public string? NotificationTitle { get; set; }
    public string? NotificationMessage { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public List<AlertRuleConditionDetailDto> Conditions { get; set; } = [];
    public List<AlertRuleRecipientDetailDto> Recipients { get; set; } = [];
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

// Same shape as CreateAlertRuleRequest — the frontend resends the complete
// current configuration and the backend replaces conditions/recipients wholesale.
public sealed class UpdateAlertRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public int MatchModeId { get; set; }
    public int UrgencyId { get; set; }
    public string NotificationTitle { get; set; } = string.Empty;
    public string NotificationMessage { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<CreateAlertRuleConditionRequest> Conditions { get; set; } = [];
    public List<CreateAlertRuleRecipientRequest> Recipients { get; set; } = [];
}

public sealed class UpdateAlertRuleResponse
{
    public int Id { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int ConditionCount { get; set; }
    public int RecipientCount { get; set; }
    public DateTime ModifiedAt { get; set; }
}

public sealed class ToggleAlertRuleResponse
{
    public int Id { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class TestAlertRuleRecipientDto
{
    public int RecipientUserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
}

public sealed class TestAlertRuleResponse
{
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public List<TestAlertRuleRecipientDto> Recipients { get; set; } = [];
    public string Message { get; set; } = string.Empty;
    public DateTime TestedAt { get; set; }
}

public sealed class AlertRuleSummaryDto
{
    public int TotalRules { get; set; }
    public int ActiveRules { get; set; }
    public int InactiveRules { get; set; }
    public int CriticalRules { get; set; }
    public int AlertsTriggeredLast24Hours { get; set; }
}
