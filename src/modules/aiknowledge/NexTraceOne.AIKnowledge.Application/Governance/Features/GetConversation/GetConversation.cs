using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetConversation;

/// <summary>
/// Feature: GetConversation — obtém detalhes de uma conversa do assistente de IA
/// incluindo mensagens recentes com metadados de grounding e modelo.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetConversation
{
    private static bool IsConversationOwner(AiAssistantConversation conversation, ICurrentUser currentUser)
        => string.Equals(conversation.CreatedBy, currentUser.Id, StringComparison.OrdinalIgnoreCase)
           || string.Equals(conversation.CreatedBy, currentUser.Email, StringComparison.OrdinalIgnoreCase);

    /// <summary>Query de obtenção de conversa com mensagens.</summary>
    public sealed record Query(
        Guid ConversationId,
        int MessagePageSize = 50) : IQuery<Response>;

    /// <summary>Validador da query GetConversation.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).NotEmpty();
            RuleFor(x => x.MessagePageSize).InclusiveBetween(1, 200);
        }
    }

    /// <summary>Handler que obtém conversa e mensagens com metadados completos.</summary>
    public sealed class Handler(
        IAiAssistantConversationRepository conversationRepository,
        IAiMessageRepository messageRepository,
        ICurrentUser currentUser) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var conversationId = AiAssistantConversationId.From(request.ConversationId);
            var conversation = await conversationRepository.GetByIdAsync(conversationId, cancellationToken);

            if (conversation is null)
                return AiGovernanceErrors.ConversationNotFound(request.ConversationId.ToString());

            if (!IsConversationOwner(conversation, currentUser))
                return AiGovernanceErrors.ConversationAccessDenied(request.ConversationId.ToString());

            var messages = await messageRepository.ListByConversationAsync(
                request.ConversationId,
                request.MessagePageSize,
                cancellationToken);

            var messageItems = messages.Select(m => new MessageItem(
                m.Id.Value,
                m.Role,
                m.GetDisplayContent(),
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

            return new Response(
                conversation.Id.Value,
                conversation.Title,
                conversation.Persona,
                conversation.ClientType.ToString(),
                conversation.DefaultContextScope,
                conversation.LastModelUsed,
                conversation.CreatedBy,
                conversation.MessageCount,
                conversation.Tags,
                conversation.IsActive,
                conversation.LastMessageAt,
                conversation.ServiceId,
                conversation.ContractId,
                conversation.IncidentId,
                conversation.ChangeId,
                conversation.TeamId,
                messageItems,
                messageItems.Count);
        }
    }

    /// <summary>Resposta com detalhes da conversa e mensagens.</summary>
    public sealed record Response(
        Guid ConversationId,
        string Title,
        string Persona,
        string ClientType,
        string DefaultContextScope,
        string? LastModelUsed,
        string CreatedBy,
        int MessageCount,
        string Tags,
        bool IsActive,
        DateTimeOffset? LastMessageAt,
        Guid? ServiceId,
        Guid? ContractId,
        Guid? IncidentId,
        Guid? ChangeId,
        Guid? TeamId,
        IReadOnlyList<MessageItem> Messages,
        int ReturnedMessageCount);

    /// <summary>Item de mensagem com metadados completos.</summary>
    public sealed record MessageItem(
        Guid MessageId,
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
