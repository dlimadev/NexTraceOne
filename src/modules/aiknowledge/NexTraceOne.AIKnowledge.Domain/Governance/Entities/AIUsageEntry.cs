using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Domain.Entities;

/// <summary>
/// Representa uma entrada imutável na trilha de auditoria de uso de IA.
/// Cada entrada captura todos os detalhes de uma interação com IA, incluindo
/// utilizador, modelo, política avaliada, tokens consumidos e resultado da governança.
///
/// Invariantes:
/// - Entidade imutável após criação — não possui métodos de atualização.
/// - IsExternal é derivado de !IsInternal no momento do registo.
/// - TotalTokens é a soma de PromptTokens + CompletionTokens.
/// - CorrelationId permite rastreamento fim-a-fim da requisição.
/// </summary>
public sealed class AIUsageEntry : AuditableEntity<AIUsageEntryId>
{
    private AIUsageEntry() { }

    /// <summary>Identificador do utilizador que realizou a interação.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Nome de exibição do utilizador para contexto de auditoria.</summary>
    public string UserDisplayName { get; private set; } = string.Empty;

    /// <summary>Identificador do modelo de IA utilizado.</summary>
    public Guid ModelId { get; private set; }

    /// <summary>Nome do modelo de IA utilizado.</summary>
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>Provedor do modelo de IA.</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>Indica se o modelo utilizado é interno/local.</summary>
    public bool IsInternal { get; private set; }

    /// <summary>Indica se o modelo utilizado é externo.</summary>
    public bool IsExternal { get; private set; }

    /// <summary>Data/hora UTC da interação.</summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>Número de tokens consumidos no prompt/entrada.</summary>
    public int PromptTokens { get; private set; }

    /// <summary>Número de tokens gerados na resposta/saída.</summary>
    public int CompletionTokens { get; private set; }

    /// <summary>Total de tokens consumidos (prompt + completion).</summary>
    public int TotalTokens { get; private set; }

    /// <summary>Identificador da política de acesso avaliada (null se nenhuma aplicável).</summary>
    public Guid? PolicyId { get; private set; }

    /// <summary>Nome da política de acesso avaliada (null se nenhuma aplicável).</summary>
    public string? PolicyName { get; private set; }

    /// <summary>Resultado da avaliação de governança.</summary>
    public UsageResult Result { get; private set; }

    /// <summary>Identificador da conversa associada (null se interação isolada).</summary>
    public Guid? ConversationId { get; private set; }

    /// <summary>Escopo de contexto da interação (ex: "change-analysis", "contract-generation").</summary>
    public string ContextScope { get; private set; } = string.Empty;

    /// <summary>Tipo de cliente que originou a interação.</summary>
    public AIClientType ClientType { get; private set; }

    /// <summary>Identificador de correlação para rastreamento fim-a-fim.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>
    /// Regista uma nova entrada de auditoria de uso de IA.
    /// Calcula automaticamente TotalTokens e IsExternal.
    /// Entidade imutável — não possui métodos de atualização.
    /// </summary>
    public static AIUsageEntry Record(
        string userId,
        string userDisplayName,
        Guid modelId,
        string modelName,
        string provider,
        bool isInternal,
        DateTimeOffset timestamp,
        int promptTokens,
        int completionTokens,
        Guid? policyId,
        string? policyName,
        UsageResult result,
        string contextScope,
        AIClientType clientType,
        string correlationId,
        Guid? conversationId = null)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(userDisplayName);
        Guard.Against.NullOrWhiteSpace(modelName);
        Guard.Against.NullOrWhiteSpace(provider);
        Guard.Against.Negative(promptTokens);
        Guard.Against.Negative(completionTokens);
        Guard.Against.NullOrWhiteSpace(correlationId);

        return new AIUsageEntry
        {
            Id = AIUsageEntryId.New(),
            UserId = userId,
            UserDisplayName = userDisplayName,
            ModelId = modelId,
            ModelName = modelName,
            Provider = provider,
            IsInternal = isInternal,
            IsExternal = !isInternal,
            Timestamp = timestamp,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = promptTokens + completionTokens,
            PolicyId = policyId,
            PolicyName = policyName,
            Result = result,
            ConversationId = conversationId,
            ContextScope = contextScope ?? string.Empty,
            ClientType = clientType,
            CorrelationId = correlationId
        };
    }
}

/// <summary>Identificador fortemente tipado de AIUsageEntry.</summary>
public sealed record AIUsageEntryId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AIUsageEntryId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AIUsageEntryId From(Guid id) => new(id);
}
