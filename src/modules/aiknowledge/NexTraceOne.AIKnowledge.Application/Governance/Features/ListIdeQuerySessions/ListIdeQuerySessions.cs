using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListIdeQuerySessions;

/// <summary>
/// Feature: ListIdeQuerySessions — lista sessões de consulta IDE com filtros opcionais.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ListIdeQuerySessions
{
    /// <summary>Query de listagem de sessões de consulta IDE com filtros opcionais.</summary>
    public sealed record Query(
        string? UserId = null,
        string? IdeClient = null,
        string? StatusValue = null) : IQuery<Response>;

    /// <summary>Validador da query ListIdeQuerySessions.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.StatusValue)
                .Must(v => v is null or "Processing" or "Responded" or "Blocked" or "Failed")
                .WithMessage("StatusValue must be 'Processing', 'Responded', 'Blocked', or 'Failed'.");
        }
    }

    /// <summary>Handler que lista sessões de consulta IDE com filtros opcionais.</summary>
    public sealed class Handler(
        IIdeQuerySessionRepository sessionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            IdeQuerySessionStatus? status = request.StatusValue is not null
                ? Enum.Parse<IdeQuerySessionStatus>(request.StatusValue)
                : null;

            var sessions = await sessionRepository.ListAsync(
                request.UserId, request.IdeClient, status, cancellationToken);

            var items = sessions
                .Select(s => new IdeQuerySessionItem(
                    s.Id.Value,
                    s.UserId,
                    s.IdeClient,
                    s.IdeClientVersion,
                    s.QueryType,
                    s.QueryText,
                    s.ModelUsed,
                    s.TokensUsed,
                    s.Status,
                    s.ResponseTimeMs,
                    s.SubmittedAt,
                    s.RespondedAt))
                .ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta da listagem de sessões de consulta IDE.</summary>
    public sealed record Response(IReadOnlyList<IdeQuerySessionItem> Items);

    /// <summary>Item resumido de uma sessão de consulta IDE.</summary>
    public sealed record IdeQuerySessionItem(
        Guid SessionId,
        string UserId,
        string IdeClient,
        string IdeClientVersion,
        IdeQueryType QueryType,
        string QueryText,
        string ModelUsed,
        int TokensUsed,
        IdeQuerySessionStatus Status,
        long? ResponseTimeMs,
        DateTimeOffset SubmittedAt,
        DateTimeOffset? RespondedAt);
}
