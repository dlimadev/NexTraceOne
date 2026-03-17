using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.SearchAuditLog;

/// <summary>
/// Feature: SearchAuditLog — pesquisa eventos de auditoria com filtros.
/// </summary>
public static class SearchAuditLog
{
    /// <summary>Query de pesquisa de eventos de auditoria.</summary>
    public sealed record Query(string? SourceModule, string? ActionType, DateTimeOffset? From, DateTimeOffset? To, int Page, int PageSize) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que pesquisa eventos de auditoria.</summary>
    public sealed class Handler(IAuditEventRepository auditEventRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var events = await auditEventRepository.SearchAsync(
                request.SourceModule, request.ActionType, request.From, request.To, request.Page, request.PageSize, cancellationToken);

            var items = events
                .Select(e => new AuditLogEntry(e.Id.Value, e.SourceModule, e.ActionType, e.ResourceType, e.ResourceId, e.PerformedBy, e.OccurredAt))
                .ToArray();

            return new Response(items);
        }
    }

    /// <summary>Resposta da pesquisa de auditoria.</summary>
    public sealed record Response(IReadOnlyList<AuditLogEntry> Items);

    /// <summary>Entrada do log de auditoria.</summary>
    public sealed record AuditLogEntry(Guid EventId, string SourceModule, string ActionType, string ResourceType, string ResourceId, string PerformedBy, DateTimeOffset OccurredAt);
}
