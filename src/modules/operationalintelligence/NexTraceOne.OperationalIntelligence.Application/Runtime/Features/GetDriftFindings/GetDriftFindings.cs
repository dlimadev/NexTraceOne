using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.RuntimeIntelligence.Application.Abstractions;

namespace NexTraceOne.RuntimeIntelligence.Application.Features.GetDriftFindings;

/// <summary>
/// Feature: GetDriftFindings — lista drift findings de um serviço com paginação.
/// Suporta filtro opcional para retornar apenas findings não reconhecidos (unacknowledged),
/// permitindo priorização de investigação de desvios ativos.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetDriftFindings
{
    /// <summary>Query para listar drift findings de um serviço e ambiente.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment,
        bool UnacknowledgedOnly = false,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de drift findings.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que consulta drift findings por serviço/ambiente com suporte a filtro por estado.
    /// Quando UnacknowledgedOnly é true, delega para o repositório de findings não reconhecidos.
    /// </summary>
    public sealed class Handler(
        IDriftFindingRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var findings = request.UnacknowledgedOnly
                ? await repository.ListUnacknowledgedAsync(
                    request.Page,
                    request.PageSize,
                    cancellationToken)
                : await repository.ListByServiceAsync(
                    request.ServiceName,
                    request.Environment,
                    request.Page,
                    request.PageSize,
                    cancellationToken);

            var items = findings.Select(f => new DriftFindingItem(
                f.Id.Value,
                f.ServiceName,
                f.Environment,
                f.MetricName,
                f.ExpectedValue,
                f.ActualValue,
                f.DeviationPercent,
                f.Severity.ToString(),
                f.DetectedAt,
                f.ReleaseId,
                f.IsAcknowledged)).ToList();

            return new Response(
                request.ServiceName,
                request.Environment,
                items,
                request.Page,
                request.PageSize);
        }
    }

    /// <summary>Resposta com lista paginada de drift findings.</summary>
    public sealed record Response(
        string ServiceName,
        string Environment,
        IReadOnlyList<DriftFindingItem> Findings,
        int Page,
        int PageSize);

    /// <summary>Item individual de drift finding na listagem.</summary>
    public sealed record DriftFindingItem(
        Guid FindingId,
        string ServiceName,
        string Environment,
        string MetricName,
        decimal ExpectedValue,
        decimal ActualValue,
        decimal DeviationPercent,
        string Severity,
        DateTimeOffset DetectedAt,
        Guid? ReleaseId,
        bool IsAcknowledged);
}
