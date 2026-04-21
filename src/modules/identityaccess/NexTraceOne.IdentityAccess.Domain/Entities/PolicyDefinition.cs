using System.Text.Json;

using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que representa uma definição de política configurável no Policy Studio.
/// Permite que administradores de plataforma definam políticas sem código usando
/// um DSL JSON estruturado (sem dependência de OPA/Rego).
///
/// As regras são avaliadas com semântica AND — todas têm de ser satisfeitas para Allow.
/// Em caso de falha, a acção configurada (Block/Warn/Allow) é retornada.
/// Fail-open em caso de erro de parse (comportamento configurável via config).
/// </summary>
public sealed class PolicyDefinition : AuditableEntity<PolicyDefinitionId>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private PolicyDefinition() { }

    /// <summary>Tenant proprietário desta definição de política.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Nome descritivo da política.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição opcional da política.</summary>
    public string? Description { get; private set; }

    /// <summary>Tipo de política (gate de promoção, controlo de acesso, etc.).</summary>
    public PolicyDefinitionType PolicyType { get; private set; }

    /// <summary>Indica se a política está activa e deve ser avaliada.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Versão da política — incrementada em cada UpdateRules.</summary>
    public int Version { get; private set; } = 1;

    /// <summary>Array JSON de regras: [{Field, Operator, Value}].</summary>
    public string RulesJson { get; private set; } = "[]";

    /// <summary>Objecto JSON com a acção resultante: {action: "Block"|"Warn"|"Allow", message: "..."}.</summary>
    public string ActionJson { get; private set; } = """{"action":"Block","message":"Policy evaluation failed."}""";

    /// <summary>Serviços aos quais esta política se aplica — nomes separados por vírgula ou "*" para todos.</summary>
    public string AppliesTo { get; private set; } = "*";

    /// <summary>Filtro de ambiente — null para todos os ambientes.</summary>
    public string? EnvironmentFilter { get; private set; }

    /// <summary>Id do utilizador que criou a política (auditoria).</summary>
    public string? CreatedByUserId { get; private set; }

    /// <summary>Cria uma nova definição de política.</summary>
    public static PolicyDefinition Create(
        string tenantId,
        string name,
        string? description,
        PolicyDefinitionType policyType,
        string rulesJson,
        string actionJson,
        string appliesTo,
        string? environmentFilter,
        string? createdByUserId,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(rulesJson);
        Guard.Against.NullOrWhiteSpace(actionJson);

        return new PolicyDefinition
        {
            Id = PolicyDefinitionId.New(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            PolicyType = policyType,
            IsEnabled = true,
            Version = 1,
            RulesJson = rulesJson,
            ActionJson = actionJson,
            AppliesTo = string.IsNullOrWhiteSpace(appliesTo) ? "*" : appliesTo,
            EnvironmentFilter = environmentFilter,
            CreatedByUserId = createdByUserId
        };
    }

    /// <summary>Activa a política.</summary>
    public void Enable() => IsEnabled = true;

    /// <summary>Desactiva a política.</summary>
    public void Disable() => IsEnabled = false;

    /// <summary>Actualiza as regras e a acção, incrementando a versão.</summary>
    public void UpdateRules(string rulesJson, string actionJson, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(rulesJson);
        Guard.Against.NullOrWhiteSpace(actionJson);

        RulesJson = rulesJson;
        ActionJson = actionJson;
        Version++;
    }

    /// <summary>
    /// Avalia o contexto JSON contra as regras da política.
    /// Semântica AND — todos os predicados têm de ser satisfeitos.
    /// Fail-open em caso de erro de parse: retorna Allow com aviso.
    /// </summary>
    public PolicyEvaluationResult Evaluate(string contextJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contextJson))
                return new PolicyEvaluationResult(true, "Allow", "Empty context — fail-open applied.", null);

            var context = JsonSerializer.Deserialize<Dictionary<string, string>>(contextJson, JsonOptions)
                          ?? [];

            var rules = JsonSerializer.Deserialize<List<PolicyRule>>(RulesJson, JsonOptions)
                        ?? [];

            var action = JsonSerializer.Deserialize<PolicyAction>(ActionJson, JsonOptions)
                         ?? new PolicyAction("Block", "Policy evaluation failed.");

            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Field) || string.IsNullOrWhiteSpace(rule.Operator))
                    continue;

                if (!EvaluateRule(rule, context))
                {
                    var passed = string.Equals(action.Action, "Allow", StringComparison.OrdinalIgnoreCase);
                    return new PolicyEvaluationResult(passed, action.Action, action.Message, $"{rule.Field} {rule.Operator} {rule.Value}");
                }
            }

            return new PolicyEvaluationResult(true, "Allow", null, null);
        }
        catch (JsonException ex)
        {
            return new PolicyEvaluationResult(true, "Allow", $"Policy JSON parse error ({ex.GetType().Name}) — fail-open applied.", null);
        }
        catch
        {
            return new PolicyEvaluationResult(true, "Allow", "Policy evaluation error — fail-open applied.", null);
        }
    }

    private static bool EvaluateRule(PolicyRule rule, Dictionary<string, string> context)
    {
        if (!context.TryGetValue(rule.Field, out var contextValue))
            contextValue = string.Empty;

        return rule.Operator switch
        {
            "Equals" => string.Equals(contextValue, rule.Value, StringComparison.OrdinalIgnoreCase),
            "NotEquals" => !string.Equals(contextValue, rule.Value, StringComparison.OrdinalIgnoreCase),
            "Contains" => contextValue.Contains(rule.Value, StringComparison.OrdinalIgnoreCase),
            "NotContains" => !contextValue.Contains(rule.Value, StringComparison.OrdinalIgnoreCase),
            "Matches" => contextValue.Equals(rule.Value, StringComparison.OrdinalIgnoreCase),
            "GreaterThan" => decimal.TryParse(contextValue, out var cv) && decimal.TryParse(rule.Value, out var rv) && cv > rv,
            "LessThan" => decimal.TryParse(contextValue, out var cv2) && decimal.TryParse(rule.Value, out var rv2) && cv2 < rv2,
            _ => true
        };
    }

    private sealed record PolicyRule(string Field, string Operator, string Value);
    private sealed record PolicyAction(string Action, string? Message);
}

/// <summary>Identificador fortemente tipado de PolicyDefinition.</summary>
public sealed record PolicyDefinitionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PolicyDefinitionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PolicyDefinitionId From(Guid id) => new(id);
}
