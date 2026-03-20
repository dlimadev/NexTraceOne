using Ardalis.GuardClauses;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListMessages;

/// <summary>
/// Feature: ListMessages — lista mensagens de uma conversa do assistente de IA.
/// Retorna mensagens com metadados completos de modelo, grounding e política.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListMessages
{
    private static bool IsConversationOwner(AiAssistantConversation conversation, ICurrentUser currentUser)
        => string.Equals(conversation.CreatedBy, currentUser.Id, StringComparison.OrdinalIgnoreCase)
           || string.Equals(conversation.CreatedBy, currentUser.Email, StringComparison.OrdinalIgnoreCase);

    /// <summary>Query de listagem de mensagens de uma conversa.</summary>
    public sealed record Query(
        Guid ConversationId,
        int PageSize = 50) : IQuery<Response>;

    /// <summary>Handler que lista mensagens com metadados completos.</summary>
    public sealed class Handler(
        IAiAssistantConversationRepository conversationRepository,
        IAiMessageRepository messageRepository,
        ICurrentUser currentUser) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var conversation = await conversationRepository.GetByIdAsync(
                AiAssistantConversationId.From(request.ConversationId),
                cancellationToken);

            if (conversation is null)
                return AiGovernanceErrors.ConversationNotFound(request.ConversationId.ToString());

            var messages = await messageRepository.ListByConversationAsync(
                request.ConversationId,
                request.PageSize,
                cancellationToken);

            var totalCount = await messageRepository.CountByConversationAsync(
                request.ConversationId,
                cancellationToken);

            var items = messages.Select(m => new MessageItem(
                m.Id.Value,
                m.ConversationId,
                m.Role,
                m.Content,
                m.ModelName,
                m.Provider,
                m.IsInternalModel,
                m.PromptTokens,
                m.CompletionTokens,
                m.AppliedPolicyName,
                m.GroundingSources.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                m.ContextReferences.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                m.CorrelationId,
                m.Timestamp,
                m.GetResponseState(),
                m.IsDegradedResponse(),
                m.GetDegradedReason())).ToList();

            return new Response(items, totalCount);
        }
    }

    /// <summary>Resposta da listagem de mensagens.</summary>
    public sealed record Response(
        IReadOnlyList<MessageItem> Items,
        int TotalCount);

    /// <summary>Item de mensagem com metadados de resposta.</summary>
    public sealed record MessageItem(
        Guid MessageId,
        Guid ConversationId,
        string Role,
        string Content,
        string? ModelName,
        string? Provider,
        bool IsInternalModel,
        int PromptTokens,
        int CompletionTokens,
        string? AppliedPolicyName,
        List<string> GroundingSources,
        List<string> ContextReferences,
        string CorrelationId,
        DateTimeOffset Timestamp,
        string ResponseState,
        bool IsDegraded,
        string? DegradedReason);
}
