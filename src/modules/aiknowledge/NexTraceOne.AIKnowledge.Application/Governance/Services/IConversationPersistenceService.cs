using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Resultado da persistência de um par de mensagens (user + assistant) numa conversa.
/// </summary>
public sealed record MessagePersistenceResult(
    Guid ConversationId,
    Guid UserMessageId,
    DateTimeOffset UserMessageTimestamp,
    Guid AssistantMessageId,
    DateTimeOffset AssistantMessageTimestamp,
    string ResponseState,
    bool IsDegraded,
    string? DegradedReason,
    string ConversationTitle,
    int ConversationMessageCount,
    DateTimeOffset? ConversationLastMessageAt);

/// <summary>
/// Serviço responsável por gerir o ciclo de vida de conversas do assistente.
/// Encapsula: resolução ou criação de conversa, persistência de mensagens user/assistant,
/// e atualização do estado da conversa.
/// </summary>
public interface IConversationPersistenceService
{
    /// <summary>
    /// Obtém uma conversa existente pelo ID ou cria uma nova.
    /// Valida ownership quando conversa é fornecida.
    /// Retorna error se conversa não existe, não pertence ao utilizador, ou não está activa.
    /// </summary>
    Task<(AiAssistantConversation? Conversation, NexTraceOne.BuildingBlocks.Core.Results.Error? Error)> GetOrCreateAsync(
        Guid? conversationId,
        string userId,
        string? userEmail,
        string message,
        string persona,
        AIClientType clientType,
        string? contextScope,
        Guid? serviceId,
        Guid? contractId,
        Guid? incidentId,
        Guid? changeId,
        Guid? teamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste o par de mensagens (user + assistant) na conversa e actualiza o estado.
    /// Retorna metadados da persistência para inclusão na resposta.
    /// </summary>
    Task<MessagePersistenceResult> PersistMessagePairAsync(
        AiAssistantConversation conversation,
        string userMessage,
        DateTimeOffset now,
        string assistantContent,
        string selectedModel,
        string selectedProvider,
        bool isInternal,
        int promptTokens,
        int completionTokens,
        string? appliedPolicyName,
        string groundingSources,
        string contextReferences,
        string correlationId,
        bool isDegraded,
        CancellationToken cancellationToken = default);
}
