using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.AiGovernance.Domain.Errors;

namespace NexTraceOne.AiGovernance.Domain.Entities;

/// <summary>
/// Representa uma política de acesso que controla quem pode utilizar quais modelos de IA.
/// Políticas são avaliadas por escopo (utilizador, grupo, papel, persona ou equipa)
/// e determinam modelos permitidos/bloqueados, limites de tokens e restrições de ambiente.
///
/// Invariantes:
/// - Scope deve ser um dos valores válidos: user, group, role, persona, team.
/// - MaxTokensPerRequest deve ser positivo.
/// - Política inicia sempre ativa.
/// - AllowedModelIds e BlockedModelIds são listas de GUIDs separadas por vírgula.
/// </summary>
public sealed class AIAccessPolicy : AuditableEntity<AIAccessPolicyId>
{
    private AIAccessPolicy() { }

    /// <summary>Nome identificador da política de acesso.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada do propósito e escopo da política.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Tipo de escopo da política (ex: "user", "group", "role", "persona", "team").</summary>
    public string Scope { get; private set; } = string.Empty;

    /// <summary>Valor do escopo — identifica o utilizador, grupo, papel, persona ou equipa.</summary>
    public string ScopeValue { get; private set; } = string.Empty;

    /// <summary>Lista de IDs de modelos permitidos separados por vírgula. Vazio significa sem restrição.</summary>
    public string AllowedModelIds { get; private set; } = string.Empty;

    /// <summary>Lista de IDs de modelos bloqueados separados por vírgula.</summary>
    public string BlockedModelIds { get; private set; } = string.Empty;

    /// <summary>Indica se o acesso a IA externa é permitido por esta política.</summary>
    public bool AllowExternalAI { get; private set; }

    /// <summary>Indica se apenas modelos internos são permitidos.</summary>
    public bool InternalOnly { get; private set; }

    /// <summary>Limite máximo de tokens por requisição individual.</summary>
    public int MaxTokensPerRequest { get; private set; }

    /// <summary>Restrições de ambiente separadas por vírgula (ex: "production,staging").</summary>
    public string EnvironmentRestrictions { get; private set; } = string.Empty;

    /// <summary>Indica se a política está ativa e sendo avaliada.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Cria uma nova política de acesso de IA com validações de invariantes.
    /// A política inicia ativa e pronta para avaliação.
    /// </summary>
    public static AIAccessPolicy Create(
        string name,
        string description,
        string scope,
        string scopeValue,
        bool allowExternalAI,
        bool internalOnly,
        int maxTokensPerRequest,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NullOrWhiteSpace(scope);
        Guard.Against.NullOrWhiteSpace(scopeValue);
        Guard.Against.NegativeOrZero(maxTokensPerRequest);

        return new AIAccessPolicy
        {
            Id = AIAccessPolicyId.New(),
            Name = name,
            Description = description,
            Scope = scope,
            ScopeValue = scopeValue,
            AllowExternalAI = allowExternalAI,
            InternalOnly = internalOnly,
            MaxTokensPerRequest = maxTokensPerRequest,
            AllowedModelIds = string.Empty,
            BlockedModelIds = string.Empty,
            EnvironmentRestrictions = string.Empty,
            IsActive = true
        };
    }

    /// <summary>
    /// Atualiza os parâmetros da política de acesso.
    /// Permite ajustar descrição, permissões de IA externa, limites e restrições.
    /// </summary>
    public Result<Unit> Update(
        string description,
        bool allowExternalAI,
        bool internalOnly,
        int maxTokensPerRequest,
        string environmentRestrictions)
    {
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NegativeOrZero(maxTokensPerRequest);

        Description = description;
        AllowExternalAI = allowExternalAI;
        InternalOnly = internalOnly;
        MaxTokensPerRequest = maxTokensPerRequest;
        EnvironmentRestrictions = environmentRestrictions ?? string.Empty;
        return Unit.Value;
    }

    /// <summary>
    /// Define a lista de modelos permitidos (IDs separados por vírgula).
    /// Lista vazia significa sem restrição de modelos permitidos.
    /// </summary>
    public Result<Unit> SetAllowedModels(string ids)
    {
        AllowedModelIds = ids ?? string.Empty;
        return Unit.Value;
    }

    /// <summary>
    /// Define a lista de modelos bloqueados (IDs separados por vírgula).
    /// Modelos bloqueados são rejeitados independentemente de estarem na lista de permitidos.
    /// </summary>
    public Result<Unit> SetBlockedModels(string ids)
    {
        BlockedModelIds = ids ?? string.Empty;
        return Unit.Value;
    }

    /// <summary>
    /// Desativa a política, removendo-a da avaliação durante requisições de IA.
    /// Operação idempotente.
    /// </summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }

    /// <summary>
    /// Reativa a política, incluindo-a na avaliação durante requisições de IA.
    /// Operação idempotente.
    /// </summary>
    public Result<Unit> Activate()
    {
        IsActive = true;
        return Unit.Value;
    }

    /// <summary>
    /// Verifica se um modelo específico é permitido por esta política.
    /// Um modelo é permitido se: (1) não está na lista de bloqueados E
    /// (2) a lista de permitidos está vazia OU o modelo está na lista de permitidos.
    /// </summary>
    public bool IsModelAllowed(Guid modelId)
    {
        var modelIdStr = modelId.ToString();

        if (!string.IsNullOrWhiteSpace(BlockedModelIds))
        {
            var blocked = BlockedModelIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (blocked.Any(b => b.Equals(modelIdStr, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        if (string.IsNullOrWhiteSpace(AllowedModelIds))
            return true;

        var allowed = AllowedModelIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return allowed.Any(a => a.Equals(modelIdStr, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>Identificador fortemente tipado de AIAccessPolicy.</summary>
public sealed record AIAccessPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AIAccessPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AIAccessPolicyId From(Guid id) => new(id);
}
