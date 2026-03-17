using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListChanges;

/// <summary>
/// Feature: ListChanges — lista mudanças com filtros avançados para o catálogo de Change Confidence.
/// Suporta filtro por serviço, equipa, ambiente, tipo, confiança, status e período.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListChanges
{
    /// <summary>Query de listagem de mudanças com filtros avançados.</summary>
    public sealed record Query(
        string? ServiceName,
        string? TeamName,
        string? Environment,
        ChangeType? ChangeType,
        ConfidenceStatus? ConfidenceStatus,
        DeploymentStatus? DeploymentStatus,
        string? SearchTerm,
        DateTimeOffset? From,
        DateTimeOffset? To,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista mudanças com filtros avançados.</summary>
    public sealed class Handler(IReleaseRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releases = await repository.ListFilteredAsync(
                request.ServiceName, request.TeamName, request.Environment,
                request.ChangeType, request.ConfidenceStatus, request.DeploymentStatus,
                request.SearchTerm, request.From, request.To,
                request.Page, request.PageSize, cancellationToken);

            var total = await repository.CountFilteredAsync(
                request.ServiceName, request.TeamName, request.Environment,
                request.ChangeType, request.ConfidenceStatus, request.DeploymentStatus,
                request.SearchTerm, request.From, request.To, cancellationToken);

            var dtos = releases.Select(r => new ChangeDto(
                r.Id.Value, r.ApiAssetId, r.ServiceName, r.Version, r.Environment,
                r.ChangeType.ToString(), r.Status.ToString(), r.ChangeLevel.ToString(),
                r.ConfidenceStatus.ToString(), r.ValidationStatus.ToString(),
                r.ChangeScore, r.TeamName, r.Domain, r.Description,
                r.WorkItemReference, r.CommitSha, r.CreatedAt)).ToList();

            return new Response(dtos, total, request.Page, request.PageSize);
        }
    }

    /// <summary>DTO de mudança para listagem do catálogo de Change Confidence.</summary>
    public sealed record ChangeDto(
        Guid ChangeId,
        Guid ApiAssetId,
        string ServiceName,
        string Version,
        string Environment,
        string ChangeType,
        string DeploymentStatus,
        string ChangeLevel,
        string ConfidenceStatus,
        string ValidationStatus,
        decimal ChangeScore,
        string? TeamName,
        string? Domain,
        string? Description,
        string? WorkItemReference,
        string CommitSha,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta paginada da listagem de mudanças.</summary>
    public sealed record Response(
        IReadOnlyList<ChangeDto> Changes,
        int TotalCount,
        int Page,
        int PageSize);
}
