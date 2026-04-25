using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para TenantPipelineRule.
/// </summary>
public sealed record TenantPipelineRuleId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Regra de processamento configurada por tenant que é aplicada pelo TenantPipelineEngine
/// a cada sinal de telemetria ingerido (span, log ou métrica).
///
/// As regras são aplicadas em ordem ascendente de Priority dentro do mesmo RuleType.
/// Suporta acções de Masking, Filtering, Enrichment e Transform.
///
/// Owner: módulo Integrations (Pipeline).
/// </summary>
public sealed class TenantPipelineRule : Entity<TenantPipelineRuleId>
{
    /// <summary>Tenant proprietário da regra.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Nome descritivo da regra (ex: "Mask PII emails in logs").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Tipo de acção que a regra executa.</summary>
    public PipelineRuleType RuleType { get; private set; }

    /// <summary>Tipo de sinal ao qual a regra se aplica.</summary>
    public PipelineSignalType SignalType { get; private set; }

    /// <summary>
    /// Condição em JSON para avaliar se a regra deve ser aplicada.
    /// Formato: {"field": "$.level", "operator": "eq", "value": "debug"}
    /// </summary>
    public string ConditionJson { get; private set; } = "{}";

    /// <summary>
    /// Acção em JSON que define o que fazer quando a condição é satisfeita.
    /// Masking: {"field": "$.body.email", "replacement": "[REDACTED]"}
    /// Filtering: {"action": "discard"}
    /// Enrichment: {"attributes": {"key": "value"}}
    /// Transform: {"field": "$.level", "rename": "severity"}
    /// </summary>
    public string ActionJson { get; private set; } = "{}";

    /// <summary>Prioridade de execução (menor número = executado primeiro).</summary>
    public int Priority { get; private set; }

    /// <summary>Indica se a regra está activa.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Descrição opcional da regra.</summary>
    public string? Description { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    private TenantPipelineRule() { }

    /// <summary>Cria uma nova regra de pipeline para o tenant.</summary>
    public static TenantPipelineRule Create(
        string tenantId,
        string name,
        PipelineRuleType ruleType,
        PipelineSignalType signalType,
        string conditionJson,
        string actionJson,
        int priority,
        bool isEnabled,
        string? description,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 150, nameof(name));
        Guard.Against.NullOrWhiteSpace(conditionJson, nameof(conditionJson));
        Guard.Against.NullOrWhiteSpace(actionJson, nameof(actionJson));
        Guard.Against.NegativeOrZero(priority, nameof(priority));

        return new TenantPipelineRule
        {
            Id = new TenantPipelineRuleId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name.Trim(),
            RuleType = ruleType,
            SignalType = signalType,
            ConditionJson = conditionJson,
            ActionJson = actionJson,
            Priority = priority,
            IsEnabled = isEnabled,
            Description = description?.Trim(),
            CreatedAt = utcNow
        };
    }

    /// <summary>Actualiza a configuração da regra.</summary>
    public void Update(
        string name,
        PipelineRuleType ruleType,
        PipelineSignalType signalType,
        string conditionJson,
        string actionJson,
        int priority,
        bool isEnabled,
        string? description,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 150, nameof(name));
        Guard.Against.NullOrWhiteSpace(conditionJson, nameof(conditionJson));
        Guard.Against.NullOrWhiteSpace(actionJson, nameof(actionJson));
        Guard.Against.NegativeOrZero(priority, nameof(priority));

        Name = name.Trim();
        RuleType = ruleType;
        SignalType = signalType;
        ConditionJson = conditionJson;
        ActionJson = actionJson;
        Priority = priority;
        IsEnabled = isEnabled;
        Description = description?.Trim();
        UpdatedAt = utcNow;
    }

    /// <summary>Activa a regra.</summary>
    public void Enable(DateTimeOffset utcNow)
    {
        IsEnabled = true;
        UpdatedAt = utcNow;
    }

    /// <summary>Desactiva a regra.</summary>
    public void Disable(DateTimeOffset utcNow)
    {
        IsEnabled = false;
        UpdatedAt = utcNow;
    }
}
