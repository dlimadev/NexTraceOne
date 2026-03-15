using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.ListConversations;

/// <summary>
/// Feature: ListConversations — lista resumos de conversas do assistente de IA.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// Stub: retorna lista vazia enquanto a persistência de conversas é implementada.
/// </summary>
public static class ListConversations
{
    /// <summary>Query de listagem de conversas do assistente de IA.</summary>
    public sealed record Query(
        string? UserId,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que lista resumos de conversas — stub retorna lista vazia.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Stub: persistência de conversas ainda não implementada
            var items = new List<ConversationSummary>();
            return Task.FromResult(Result<Response>.Success(new Response(items, 0)));
        }
    }

    /// <summary>Resposta da listagem de conversas do assistente de IA.</summary>
    public sealed record Response(
        IReadOnlyList<ConversationSummary> Items,
        int TotalCount);

    /// <summary>Resumo de uma conversa com o assistente de IA.</summary>
    public sealed record ConversationSummary(
        Guid Id,
        string Topic,
        string? ServiceName,
        DateTimeOffset StartedAt,
        int TurnCount,
        string Status);
}
