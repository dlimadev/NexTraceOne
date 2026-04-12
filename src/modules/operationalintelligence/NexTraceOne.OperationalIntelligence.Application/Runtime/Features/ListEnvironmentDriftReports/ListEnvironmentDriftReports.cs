using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListEnvironmentDriftReports;

/// <summary>
/// Feature: ListEnvironmentDriftReports — lista relatórios de drift com filtros opcionais.
/// Permite filtrar por ambiente de origem, ambiente alvo e status.
/// </summary>
public static class ListEnvironmentDriftReports
{
    /// <summary>Query para listar relatórios de drift com filtros opcionais.</summary>
    public sealed record Query(
        string? SourceEnvironment = null,
        string? TargetEnvironment = null,
        string? Status = null) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SourceEnvironment).MaximumLength(100).When(x => x.SourceEnvironment is not null);
            RuleFor(x => x.TargetEnvironment).MaximumLength(100).When(x => x.TargetEnvironment is not null);
            RuleFor(x => x.Status).MaximumLength(50).When(x => x.Status is not null);
        }
    }

    /// <summary>Handler que lista relatórios de drift.</summary>
    public sealed class Handler(
        IEnvironmentDriftReportRepository reportRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            DriftReportStatus? statusFilter = null;
            if (request.Status is not null && Enum.TryParse<DriftReportStatus>(request.Status, true, out var parsed))
                statusFilter = parsed;

            var reports = await reportRepository.ListAsync(
                request.SourceEnvironment,
                request.TargetEnvironment,
                statusFilter,
                cancellationToken);

            var items = reports
                .Select(r => new ReportSummaryItem(
                    r.Id.Value,
                    r.SourceEnvironment,
                    r.TargetEnvironment,
                    r.AnalyzedDimensions,
                    r.TotalDriftItems,
                    r.CriticalDriftItems,
                    r.OverallSeverity.ToString(),
                    r.Status.ToString(),
                    r.GeneratedAt))
                .ToList();

            return Result<Response>.Success(new Response(items, items.Count));
        }
    }

    /// <summary>Item de resumo do relatório de drift.</summary>
    public sealed record ReportSummaryItem(
        Guid ReportId,
        string SourceEnvironment,
        string TargetEnvironment,
        string AnalyzedDimensions,
        int TotalDriftItems,
        int CriticalDriftItems,
        string OverallSeverity,
        string Status,
        DateTimeOffset GeneratedAt);

    /// <summary>Resposta com a lista de relatórios.</summary>
    public sealed record Response(
        IReadOnlyList<ReportSummaryItem> Reports,
        int TotalCount);
}
