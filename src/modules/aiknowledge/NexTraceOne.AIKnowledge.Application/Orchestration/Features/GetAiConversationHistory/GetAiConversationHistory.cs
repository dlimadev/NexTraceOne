using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.GetAiConversationHistory;

/// <summary>
/// Feature: GetAiConversationHistory — recupera histórico real de conversas de IA
/// persistidas na base de dados de orquestração. Suporta filtros por período, status,
/// serviço e tópico, com paginação. Dados sempre reais, sem mock.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetAiConversationHistory
{
    // ── QUERY ─────────────────────────────────────────────────────────────

    /// <summary>Query para recuperar histórico de conversas de IA com filtros opcionais.</summary>
    public sealed record Query(
        Guid? ReleaseId,
        string? ServiceName,
        string? TopicFilter,
        ConversationStatus? Status,
        DateTimeOffset? From,
        DateTimeOffset? To,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.ServiceName).MaximumLength(300).When(x => x.ServiceName is not null);
            RuleFor(x => x.TopicFilter).MaximumLength(500).When(x => x.TopicFilter is not null);
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IAiOrchestrationConversationRepository conversationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (items, total) = await conversationRepository.ListHistoryAsync(
                request.ReleaseId,
                request.ServiceName,
                request.TopicFilter,
                request.Status,
                request.From,
                request.To,
                request.Page,
                request.PageSize,
                cancellationToken);

            var conversationItems = items.Select(c => new ConversationItem(
                c.Id.Value,
                c.ReleaseId,
                c.ServiceName,
                c.Topic,
                c.TurnCount,
                c.Status.ToString(),
                c.StartedBy,
                c.StartedAt,
                c.LastTurnAt,
                c.Summary)).ToList();

            return new Response(
                conversationItems,
                total,
                request.Page,
                request.PageSize,
                (int)Math.Ceiling(total / (double)request.PageSize));
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Histórico paginado de conversas de IA.</summary>
    public sealed record Response(
        IReadOnlyList<ConversationItem> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);

    /// <summary>Item de conversa de IA para listagem histórica.</summary>
    public sealed record ConversationItem(
        Guid ConversationId,
        Guid? ReleaseId,
        string ServiceName,
        string Topic,
        int TurnCount,
        string Status,
        string StartedBy,
        DateTimeOffset StartedAt,
        DateTimeOffset? LastTurnAt,
        string? Summary);
}
