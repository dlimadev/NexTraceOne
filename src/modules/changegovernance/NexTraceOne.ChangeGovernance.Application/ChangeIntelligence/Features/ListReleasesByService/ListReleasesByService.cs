using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleasesByService;

/// <summary>
/// Feature: ListReleasesByService — lista releases de um serviço específico para consumo da Ingestion API.
/// Permite que sistemas externos (dashboards, pipelines, portais de governança) consultem
/// o histórico de releases de um serviço pelo seu nome, sem necessidade de GUID interno.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListReleasesByService
{
    /// <summary>Query de listagem de releases por nome de serviço com paginação.</summary>
    public sealed record Query(
        string ServiceName,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem de releases por serviço.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que lista releases de um serviço pelo nome com paginação e contagem total.
    /// Usa ListByServiceNameAsync do repositório que já filtra e ordena por data de criação descendente.
    /// </summary>
    public sealed class Handler(IReleaseRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releases = await repository.ListByServiceNameAsync(
                request.ServiceName,
                request.Page,
                request.PageSize,
                cancellationToken);

            var total = await repository.CountByServiceNameAsync(request.ServiceName, cancellationToken);

            var dtos = releases.Select(r => new ReleaseItem(
                r.Id.Value,
                r.ServiceName,
                r.Version,
                r.Environment,
                r.Status.ToString(),
                r.ChangeLevel,
                r.ChangeScore,
                r.ExternalReleaseId,
                r.ExternalSystem,
                r.CreatedAt)).ToList();

            return new Response(dtos, total, request.Page, request.PageSize);
        }
    }

    /// <summary>DTO de resumo de Release para listagem por serviço.</summary>
    public sealed record ReleaseItem(
        Guid ReleaseId,
        string ServiceName,
        string Version,
        string Environment,
        string Status,
        ChangeLevel ChangeLevel,
        decimal ChangeScore,
        string? ExternalReleaseId,
        string? ExternalSystem,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta paginada da listagem de releases por serviço.</summary>
    public sealed record Response(
        IReadOnlyList<ReleaseItem> Releases,
        int TotalCount,
        int Page,
        int PageSize);
}
