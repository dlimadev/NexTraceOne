using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Regra de automação If-Then para o tenant.
/// Avaliada em event handlers existentes quando o trigger for activado.
///
/// Triggers suportados: on_change_created, on_incident_opened, on_contract_published, on_approval_expired.
/// Acções suportadas: send_notification, assign_reviewer, add_tag, require_evidence, create_incident.
/// </summary>
public sealed class AutomationRule : AuditableEntity<AutomationRuleId>
{
    private const int MaxNameLength = 100;
    private static readonly string[] ValidTriggers =
        ["on_change_created", "on_incident_opened", "on_contract_published", "on_approval_expired"];

    private AutomationRule() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Trigger { get; private set; } = string.Empty;
    public string ConditionsJson { get; private set; } = "[]";
    public string ActionsJson { get; private set; } = "[]";
    public bool IsEnabled { get; private set; } = true;
    public string RuleCreatedBy { get; private set; } = string.Empty;

    public static AutomationRule Create(
        string tenantId,
        string name,
        string trigger,
        string conditionsJson,
        string actionsJson,
        string createdBy,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(trigger);
        Guard.Against.NullOrWhiteSpace(createdBy);

        if (!ValidTriggers.Contains(trigger, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Trigger must be one of: {string.Join(", ", ValidTriggers)}", nameof(trigger));

        var rule = new AutomationRule
        {
            Id = new AutomationRuleId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name,
            Trigger = trigger.Trim().ToLower(),
            ConditionsJson = string.IsNullOrWhiteSpace(conditionsJson) ? "[]" : conditionsJson,
            ActionsJson = string.IsNullOrWhiteSpace(actionsJson) ? "[]" : actionsJson,
            IsEnabled = true,
            RuleCreatedBy = createdBy,
        };
        rule.SetCreated(createdAt, string.Empty);
        rule.SetUpdated(createdAt, string.Empty);
        return rule;
    }

    public void Toggle(bool enabled, DateTimeOffset updatedAt)
    {
        IsEnabled = enabled;
        SetUpdated(updatedAt, string.Empty);
    }

    public void UpdateDetails(string name, string conditionsJson, string actionsJson, DateTimeOffset updatedAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Name = name;
        ConditionsJson = string.IsNullOrWhiteSpace(conditionsJson) ? "[]" : conditionsJson;
        ActionsJson = string.IsNullOrWhiteSpace(actionsJson) ? "[]" : actionsJson;
        SetUpdated(updatedAt, string.Empty);
    }
}
