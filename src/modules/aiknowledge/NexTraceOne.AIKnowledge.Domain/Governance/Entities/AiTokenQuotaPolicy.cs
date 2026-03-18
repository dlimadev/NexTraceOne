using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Política de quota de tokens para controlo de consumo de IA externa.
/// Define limites por pedido, dia, mês e acumulado, aplicáveis por scope
/// (utilizador, tenant, provedor ou modelo).
///
/// Invariantes:
/// - Nome e Scope são obrigatórios.
/// - ScopeValue é obrigatório — identifica o alvo da política.
/// - Limites de tokens por pedido devem ser positivos.
/// - Limites diários e mensais devem ser positivos.
/// - Política inicia sempre com IsEnabled = true.
/// </summary>
public sealed class AiTokenQuotaPolicy : AuditableEntity<AiTokenQuotaPolicyId>
{
    private AiTokenQuotaPolicy() { }

    /// <summary>Nome da política (ex: "default-user-quota", "premium-team-quota").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição operacional da política.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Escopo de aplicação da política (ex: "user", "tenant", "provider", "model").</summary>
    public string Scope { get; private set; } = string.Empty;

    /// <summary>Valor do escopo — identifica o alvo concreto (ex: userId, tenantId).</summary>
    public string ScopeValue { get; private set; } = string.Empty;

    /// <summary>Identificador do provedor alvo (opcional — null aplica a todos).</summary>
    public string? ProviderId { get; private set; }

    /// <summary>Identificador do modelo alvo (opcional — null aplica a todos).</summary>
    public string? ModelId { get; private set; }

    /// <summary>Máximo de tokens de entrada por pedido individual.</summary>
    public int MaxInputTokensPerRequest { get; private set; }

    /// <summary>Máximo de tokens de saída por pedido individual.</summary>
    public int MaxOutputTokensPerRequest { get; private set; }

    /// <summary>Máximo de tokens totais (entrada + saída) por pedido individual.</summary>
    public int MaxTotalTokensPerRequest { get; private set; }

    /// <summary>Máximo de tokens consumíveis por dia.</summary>
    public long MaxTokensPerDay { get; private set; }

    /// <summary>Máximo de tokens consumíveis por mês.</summary>
    public long MaxTokensPerMonth { get; private set; }

    /// <summary>Máximo de tokens acumulados permitidos (lifetime cap).</summary>
    public long MaxTokensAccumulated { get; private set; }

    /// <summary>Indica se o limite é rígido (bloqueia) ou flexível (avisa).</summary>
    public bool IsHardLimit { get; private set; }

    /// <summary>Indica se o escopo permite envio de dados sensíveis para IA externa.</summary>
    public bool AllowSensitiveData { get; private set; }

    /// <summary>Indica se o escopo permite promoção de conhecimento gerado externamente.</summary>
    public bool AllowKnowledgePromotion { get; private set; }

    /// <summary>Indica se a política está ativa.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Cria uma nova política de quota de tokens com validações de invariantes.
    /// A política inicia ativa.
    /// </summary>
    public static AiTokenQuotaPolicy Create(
        string name,
        string description,
        string scope,
        string scopeValue,
        string? providerId,
        string? modelId,
        int maxInputTokensPerRequest,
        int maxOutputTokensPerRequest,
        int maxTotalTokensPerRequest,
        long maxTokensPerDay,
        long maxTokensPerMonth,
        long maxTokensAccumulated,
        bool isHardLimit,
        bool allowSensitiveData,
        bool allowKnowledgePromotion)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(scope);
        Guard.Against.NullOrWhiteSpace(scopeValue);
        Guard.Against.NegativeOrZero(maxInputTokensPerRequest);
        Guard.Against.NegativeOrZero(maxOutputTokensPerRequest);
        Guard.Against.NegativeOrZero(maxTotalTokensPerRequest);
        Guard.Against.NegativeOrZero(maxTokensPerDay);
        Guard.Against.NegativeOrZero(maxTokensPerMonth);
        Guard.Against.NegativeOrZero(maxTokensAccumulated);

        return new AiTokenQuotaPolicy
        {
            Id = AiTokenQuotaPolicyId.New(),
            Name = name,
            Description = description ?? string.Empty,
            Scope = scope,
            ScopeValue = scopeValue,
            ProviderId = providerId,
            ModelId = modelId,
            MaxInputTokensPerRequest = maxInputTokensPerRequest,
            MaxOutputTokensPerRequest = maxOutputTokensPerRequest,
            MaxTotalTokensPerRequest = maxTotalTokensPerRequest,
            MaxTokensPerDay = maxTokensPerDay,
            MaxTokensPerMonth = maxTokensPerMonth,
            MaxTokensAccumulated = maxTokensAccumulated,
            IsHardLimit = isHardLimit,
            AllowSensitiveData = allowSensitiveData,
            AllowKnowledgePromotion = allowKnowledgePromotion,
            IsEnabled = true
        };
    }

    /// <summary>
    /// Atualiza os parâmetros da política de quota.
    /// Permite ajustar limites, permissões e descrição.
    /// </summary>
    public Result<Unit> Update(
        string description,
        int maxInputTokensPerRequest,
        int maxOutputTokensPerRequest,
        int maxTotalTokensPerRequest,
        long maxTokensPerDay,
        long maxTokensPerMonth,
        long maxTokensAccumulated,
        bool isHardLimit,
        bool allowSensitiveData,
        bool allowKnowledgePromotion)
    {
        Guard.Against.NegativeOrZero(maxInputTokensPerRequest);
        Guard.Against.NegativeOrZero(maxOutputTokensPerRequest);
        Guard.Against.NegativeOrZero(maxTotalTokensPerRequest);
        Guard.Against.NegativeOrZero(maxTokensPerDay);
        Guard.Against.NegativeOrZero(maxTokensPerMonth);
        Guard.Against.NegativeOrZero(maxTokensAccumulated);

        Description = description ?? string.Empty;
        MaxInputTokensPerRequest = maxInputTokensPerRequest;
        MaxOutputTokensPerRequest = maxOutputTokensPerRequest;
        MaxTotalTokensPerRequest = maxTotalTokensPerRequest;
        MaxTokensPerDay = maxTokensPerDay;
        MaxTokensPerMonth = maxTokensPerMonth;
        MaxTokensAccumulated = maxTokensAccumulated;
        IsHardLimit = isHardLimit;
        AllowSensitiveData = allowSensitiveData;
        AllowKnowledgePromotion = allowKnowledgePromotion;
        return Unit.Value;
    }

    /// <summary>
    /// Ativa a política, aplicando os limites definidos.
    /// Operação idempotente — não retorna erro se já ativa.
    /// </summary>
    public Result<Unit> Enable()
    {
        IsEnabled = true;
        return Unit.Value;
    }

    /// <summary>
    /// Desativa a política, suspendendo a aplicação dos limites.
    /// Operação idempotente.
    /// </summary>
    public Result<Unit> Disable()
    {
        IsEnabled = false;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AiTokenQuotaPolicy.</summary>
public sealed record AiTokenQuotaPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiTokenQuotaPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiTokenQuotaPolicyId From(Guid id) => new(id);
}
