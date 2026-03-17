using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListConversations;

/// <summary>
/// Feature: ListConversations — lista resumos de conversas do assistente de IA.
/// Retorna conversas com metadados de persona, modelo e escopo de contexto.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListConversations
{
    /// <summary>Query de listagem de conversas do assistente de IA.</summary>
    public sealed record Query(
        string? UserId,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que lista resumos de conversas maduras do assistente.</summary>
    public sealed class Handler(
        IAiAssistantConversationRepository conversationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var conversations = await conversationRepository.ListAsync(
                request.UserId,
                isActive: null,
                request.PageSize,
                cancellationToken);

            var totalCount = await conversationRepository.CountAsync(
                request.UserId,
                isActive: null,
                cancellationToken);

            var items = conversations.Select(c => new ConversationSummary(
                c.Id.Value,
                c.Title,
                c.Persona,
                c.ClientType.ToString(),
                c.DefaultContextScope,
                c.LastModelUsed,
                c.CreatedBy,
                c.MessageCount,
                c.Tags,
                c.IsActive,
                c.LastMessageAt)).ToList();

            return new Response(items, totalCount);
        }
    }

    /// <summary>Resposta da listagem de conversas do assistente de IA.</summary>
    public sealed record Response(
        IReadOnlyList<ConversationSummary> Items,
        int TotalCount);

    /// <summary>Resumo maduro de uma conversa com o assistente de IA.</summary>
    public sealed record ConversationSummary(
        Guid Id,
        string Title,
        string Persona,
        string ClientType,
        string DefaultContextScope,
        string? LastModelUsed,
        string CreatedBy,
        int MessageCount,
        string Tags,
        bool IsActive,
        DateTimeOffset? LastMessageAt);
}
