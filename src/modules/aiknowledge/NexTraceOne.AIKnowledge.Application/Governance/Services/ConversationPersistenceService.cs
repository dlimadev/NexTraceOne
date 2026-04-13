using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação do serviço de gestão do ciclo de vida de conversas do assistente.
/// Encapsula a lógica de resolução ou criação de conversa, persistência de mensagens
/// e atualização do estado da conversa após cada interação.
/// </summary>
public sealed class ConversationPersistenceService(
    IAiAssistantConversationRepository conversationRepository,
    IAiMessageRepository messageRepository,
    IDateTimeProvider dateTimeProvider) : IConversationPersistenceService
{
    public async Task<(AiAssistantConversation? Conversation, Error? Error)> GetOrCreateAsync(
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
        CancellationToken cancellationToken = default)
    {
        AiAssistantConversation? conversation = null;

        if (conversationId.HasValue)
        {
            var convId = AiAssistantConversationId.From(conversationId.Value);
            conversation = await conversationRepository.GetByIdAsync(convId, cancellationToken);

            if (conversation is null)
                return (null, AiGovernanceErrors.ConversationNotFound(conversationId.Value.ToString()));

            if (!IsConversationOwner(conversation, userId, userEmail))
                return (null, AiGovernanceErrors.ConversationAccessDenied(conversationId.Value.ToString()));

            if (!conversation.IsActive)
                return (null, AiGovernanceErrors.ConversationNotActive(conversationId.Value.ToString()));

            return (conversation, null);
        }

        // Create new conversation
        var owner = !string.IsNullOrWhiteSpace(userEmail) ? userEmail : userId;
        var title = message.Length > 100
            ? string.Concat(message.AsSpan(0, 97), "...")
            : message;

        conversation = AiAssistantConversation.Start(
            title, persona, clientType,
            contextScope ?? string.Empty,
            owner,
            serviceId: serviceId,
            contractId: contractId,
            incidentId: incidentId,
            teamId: teamId,
            changeId: changeId);

        await conversationRepository.AddAsync(conversation, cancellationToken);
        return (conversation, null);
    }

    public async Task<MessagePersistenceResult> PersistMessagePairAsync(
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
        CancellationToken cancellationToken = default)
    {
        const string degradedProviderId = "system-fallback";
        const string degradedModelName = "deterministic-fallback";

        // Persist user message
        var userMsg = AiMessage.UserMessage(conversation.Id.Value, userMessage, now);
        conversation.RecordMessage(null, now);
        await messageRepository.AddAsync(userMsg, cancellationToken);

        // Persist assistant message
        conversation.RecordMessage(selectedModel, now);

        var assistantMsg = isDegraded
            ? AiMessage.DegradedAssistantMessage(
                conversation.Id.Value,
                assistantContent,
                string.IsNullOrWhiteSpace(selectedModel) ? degradedModelName : selectedModel,
                string.IsNullOrWhiteSpace(selectedProvider) ? degradedProviderId : selectedProvider,
                promptTokens,
                completionTokens,
                appliedPolicyName,
                groundingSources,
                contextReferences,
                correlationId,
                now)
            : AiMessage.AssistantMessage(
                conversation.Id.Value,
                assistantContent,
                string.IsNullOrWhiteSpace(selectedModel) ? degradedModelName : selectedModel,
                string.IsNullOrWhiteSpace(selectedProvider) ? degradedProviderId : selectedProvider,
                isInternal: isInternal,
                promptTokens,
                completionTokens,
                appliedPolicyName,
                groundingSources,
                contextReferences,
                correlationId,
                now);

        await messageRepository.AddAsync(assistantMsg, cancellationToken);
        await conversationRepository.UpdateAsync(conversation, cancellationToken);

        return new MessagePersistenceResult(
            conversation.Id.Value,
            userMsg.Id.Value,
            userMsg.Timestamp,
            assistantMsg.Id.Value,
            assistantMsg.Timestamp,
            assistantMsg.GetResponseState(),
            assistantMsg.IsDegradedResponse(),
            assistantMsg.GetDegradedReason(),
            conversation.Title,
            conversation.MessageCount,
            conversation.LastMessageAt);
    }

    private static bool IsConversationOwner(AiAssistantConversation conversation, string userId, string? userEmail)
        => string.Equals(conversation.CreatedBy, userId, StringComparison.OrdinalIgnoreCase)
           || (!string.IsNullOrWhiteSpace(userEmail) &&
               string.Equals(conversation.CreatedBy, userEmail, StringComparison.OrdinalIgnoreCase));
}
