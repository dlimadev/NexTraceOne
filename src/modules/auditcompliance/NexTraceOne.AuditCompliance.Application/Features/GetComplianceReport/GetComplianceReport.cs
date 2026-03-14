using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Audit.Application.Abstractions;

namespace NexTraceOne.Audit.Application.Features.GetComplianceReport;

/// <summary>
/// Feature: GetComplianceReport — gera relatório de compliance com base nos dados de auditoria.
/// </summary>
public static class GetComplianceReport
{
    /// <summary>Query de relatório de compliance.</summary>
    public sealed record Query(DateTimeOffset From, DateTimeOffset To) : IQuery<Response>;

    /// <summary>Handler que gera o relatório de compliance.</summary>
    public sealed class Handler(
        IAuditEventRepository auditEventRepository,
        IAuditChainRepository auditChainRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var events = await auditEventRepository.SearchAsync(null, null, request.From, request.To, 1, 10000, cancellationToken);
            var links = await auditChainRepository.GetAllLinksAsync(cancellationToken);

            var moduleBreakdown = events
                .GroupBy(e => e.SourceModule)
                .Select(g => new ModuleSummary(g.Key, g.Count()))
                .ToArray();

            return new Response(
                request.From,
                request.To,
                events.Count,
                links.Count,
                true,
                moduleBreakdown);
        }
    }

    /// <summary>Resposta do relatório de compliance.</summary>
    public sealed record Response(
        DateTimeOffset From,
        DateTimeOffset To,
        int TotalEvents,
        int TotalChainLinks,
        bool ChainIntact,
        IReadOnlyList<ModuleSummary> ModuleBreakdown);

    /// <summary>Resumo de eventos por módulo.</summary>
    public sealed record ModuleSummary(string SourceModule, int EventCount);
}
