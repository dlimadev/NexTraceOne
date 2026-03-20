using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa uma mensagem individual dentro de uma conversa com o assistente de IA.
/// Captura prompt do utilizador e resposta do assistente com metadados completos
/// de modelo, tokens, política aplicada, fontes de grounding e explicabilidade.
///
/// Invariantes:
/// - Cada mensagem pertence a uma conversa (ConversationId obrigatório).
/// - Role identifica se é mensagem do utilizador ou do assistente.
/// - Metadados de resposta são opcionais (preenchidos apenas para role = assistant).
/// - Entidade imutável após criação — não possui métodos de atualização.
/// </summary>
public sealed class AiMessage : AuditableEntity<AiMessageId>
{
    public const string DeterministicFallbackPrefix = "[FALLBACK_PROVIDER_UNAVAILABLE]";
    public const string CompletedResponseState = "Completed";
    public const string DegradedResponseState = "Degraded";
    public const string ProviderUnavailableReason = "ProviderUnavailable";

    private AiMessage() { }

    /// <summary>Identificador da conversa à qual esta mensagem pertence.</summary>
    public Guid ConversationId { get; private set; }

    /// <summary>Papel do autor: "user" ou "assistant".</summary>
    public string Role { get; private set; } = string.Empty;

    /// <summary>Conteúdo da mensagem (prompt do utilizador ou resposta do assistente).</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Nome do modelo de IA utilizado (null para mensagens do utilizador).</summary>
    public string? ModelName { get; private set; }

    /// <summary>Provedor do modelo utilizado (ex: "Internal", "OpenAI").</summary>
    public string? Provider { get; private set; }

    /// <summary>Indica se o modelo utilizado é interno/local.</summary>
    public bool IsInternalModel { get; private set; }

    /// <summary>Tokens consumidos no prompt/entrada.</summary>
    public int PromptTokens { get; private set; }

    /// <summary>Tokens gerados na resposta/saída.</summary>
    public int CompletionTokens { get; private set; }

    /// <summary>Nome da política de acesso aplicada (null se nenhuma).</summary>
    public string? AppliedPolicyName { get; private set; }

    /// <summary>Fontes de grounding utilizadas, separadas por vírgula.</summary>
    public string GroundingSources { get; private set; } = string.Empty;

    /// <summary>Referências de contexto utilizadas para grounding, separadas por vírgula.</summary>
    public string ContextReferences { get; private set; } = string.Empty;

    /// <summary>Identificador de correlação para rastreamento fim-a-fim.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que a mensagem foi criada.</summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>
    /// Cria uma mensagem do utilizador na conversa.
    /// </summary>
    public static AiMessage UserMessage(
        Guid conversationId,
        string content,
        DateTimeOffset timestamp)
    {
        Guard.Against.Default(conversationId);
        Guard.Against.NullOrWhiteSpace(content);

        return new AiMessage
        {
            Id = AiMessageId.New(),
            ConversationId = conversationId,
            Role = "user",
            Content = content,
            IsInternalModel = false,
            PromptTokens = 0,
            CompletionTokens = 0,
            GroundingSources = string.Empty,
            ContextReferences = string.Empty,
            CorrelationId = Guid.NewGuid().ToString(),
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// Cria uma mensagem do assistente com metadados completos de resposta.
    /// </summary>
    public static AiMessage AssistantMessage(
        Guid conversationId,
        string content,
        string modelName,
        string provider,
        bool isInternal,
        int promptTokens,
        int completionTokens,
        string? appliedPolicyName,
        string groundingSources,
        string contextReferences,
        string correlationId,
        DateTimeOffset timestamp)
    {
        Guard.Against.Default(conversationId);
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NullOrWhiteSpace(modelName);
        Guard.Against.NullOrWhiteSpace(provider);
        Guard.Against.NullOrWhiteSpace(correlationId);

        return new AiMessage
        {
            Id = AiMessageId.New(),
            ConversationId = conversationId,
            Role = "assistant",
            Content = content,
            ModelName = modelName,
            Provider = provider,
            IsInternalModel = isInternal,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            AppliedPolicyName = appliedPolicyName,
            GroundingSources = groundingSources ?? string.Empty,
            ContextReferences = contextReferences ?? string.Empty,
            CorrelationId = correlationId,
            Timestamp = timestamp
        };
    }

    public bool IsDegradedResponse()
        => string.Equals(Role, "assistant", StringComparison.OrdinalIgnoreCase)
           && Content.StartsWith(DeterministicFallbackPrefix, StringComparison.OrdinalIgnoreCase);

    public string GetResponseState()
        => string.Equals(Role, "assistant", StringComparison.OrdinalIgnoreCase)
            ? (IsDegradedResponse() ? DegradedResponseState : CompletedResponseState)
            : CompletedResponseState;

    public string? GetDegradedReason()
        => IsDegradedResponse() ? ProviderUnavailableReason : null;

    /// <summary>
    /// Cria uma mensagem degradada explícita quando o provider real não consegue responder.
    /// A mensagem continua persistida para manter histórico íntegro e recarregável.
    /// </summary>
    public static AiMessage DegradedAssistantMessage(
        Guid conversationId,
        string content,
        string modelName,
        string provider,
        int promptTokens,
        string correlationId,
        DateTimeOffset timestamp)
    {
        Guard.Against.Default(conversationId);
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NullOrWhiteSpace(modelName);
        Guard.Against.NullOrWhiteSpace(provider);
        Guard.Against.NullOrWhiteSpace(correlationId);

        var persistedContent = content.StartsWith(DeterministicFallbackPrefix, StringComparison.OrdinalIgnoreCase)
            ? content
            : $"{DeterministicFallbackPrefix} {content}";

        return new AiMessage
        {
            Id = AiMessageId.New(),
            ConversationId = conversationId,
            Role = "assistant",
            Content = persistedContent,
            ModelName = modelName,
            Provider = provider,
            IsInternalModel = true,
            PromptTokens = promptTokens,
            CompletionTokens = 0,
            AppliedPolicyName = null,
            GroundingSources = string.Empty,
            ContextReferences = string.Empty,
            CorrelationId = correlationId,
            Timestamp = timestamp
        };
    }
}

/// <summary>Identificador fortemente tipado de AiMessage.</summary>
public sealed record AiMessageId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiMessageId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiMessageId From(Guid id) => new(id);
}
