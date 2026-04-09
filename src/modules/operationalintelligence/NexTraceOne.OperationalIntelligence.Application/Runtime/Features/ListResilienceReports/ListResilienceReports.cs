using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListResilienceReports;

/// <summary>
/// Feature: ListResilienceReports — lista relatórios de resiliência com filtro opcional por serviço.
/// Permite consultar todos os relatórios de resiliência gerados, opcionalmente filtrados
/// por nome de serviço.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListResilienceReports
{
    /// <summary>Query para listar relatórios de resiliência com filtro opcional.</summary>
    public sealed record Query(
        string? ServiceName = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista relatórios de resiliência.</summary>
    public sealed class Handler(
        IResilienceReportRepository reportRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var reports = await reportRepository.ListByServiceAsync(
                request.ServiceName,
                cancellationToken);

            var paged = reports
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new ResilienceReportSummary(
                    r.Id.Value,
                    r.ChaosExperimentId,
                    r.ServiceName,
                    r.Environment,
                    r.ExperimentType,
                    r.ResilienceScore,
                    r.Status.ToString(),
                    r.GeneratedAt))
                .ToList();

            return Result<Response>.Success(new Response(paged, reports.Count));
        }
    }

    /// <summary>Resumo de um relatório de resiliência para listagem.</summary>
    public sealed record ResilienceReportSummary(
        Guid ReportId,
        Guid ChaosExperimentId,
        string ServiceName,
        string Environment,
        string ExperimentType,
        int ResilienceScore,
        string Status,
        DateTimeOffset GeneratedAt);

    /// <summary>Resposta paginada com lista de relatórios de resiliência.</summary>
    public sealed record Response(
        IReadOnlyList<ResilienceReportSummary> Items,
        int TotalCount);
}
