using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Audit.Application.Abstractions;

namespace NexTraceOne.Audit.Application.Features.ExportAuditReport;

/// <summary>
/// Feature: ExportAuditReport — exporta relatório de auditoria para um período.
/// </summary>
public static class ExportAuditReport
{
    /// <summary>Query de exportação de relatório de auditoria.</summary>
    public sealed record Query(DateTimeOffset From, DateTimeOffset To) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThan(x => x.To);
        }
    }

    /// <summary>Handler que gera o relatório de auditoria para o período.</summary>
    public sealed class Handler(IAuditEventRepository auditEventRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var events = await auditEventRepository.SearchAsync(null, null, request.From, request.To, 1, 10000, cancellationToken);

            var entries = events
                .Select(e => new ReportEntry(e.Id.Value, e.SourceModule, e.ActionType, e.ResourceType, e.ResourceId, e.PerformedBy, e.OccurredAt))
                .ToArray();

            return new Response(request.From, request.To, entries.Length, entries);
        }
    }

    /// <summary>Resposta do relatório de auditoria.</summary>
    public sealed record Response(DateTimeOffset From, DateTimeOffset To, int TotalEvents, IReadOnlyList<ReportEntry> Entries);

    /// <summary>Entrada do relatório de auditoria.</summary>
    public sealed record ReportEntry(Guid EventId, string SourceModule, string ActionType, string ResourceType, string ResourceId, string PerformedBy, DateTimeOffset OccurredAt);
}
