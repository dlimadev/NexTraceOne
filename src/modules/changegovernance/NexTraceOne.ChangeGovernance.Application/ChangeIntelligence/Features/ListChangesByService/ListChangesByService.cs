using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListChangesByService;

/// <summary>
/// Feature: ListChangesByService — lista mudanças por serviço para integração com Source of Truth.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListChangesByService
{
    /// <summary>Query de listagem de mudanças por serviço.</summary>
    public sealed record Query(
        string ServiceName,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista mudanças por serviço.</summary>
    public sealed class Handler(IReleaseRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releases = await repository.ListByServiceNameAsync(
                request.ServiceName, request.Page, request.PageSize, cancellationToken);

            var total = await repository.CountByServiceNameAsync(
                request.ServiceName, cancellationToken);

            var dtos = releases.Select(r => new ChangeDto(
                r.Id.Value, r.ApiAssetId, r.ServiceName, r.Version, r.Environment,
                r.ChangeType.ToString(), r.Status.ToString(), r.ChangeLevel.ToString(),
                r.ConfidenceStatus.ToString(), r.ValidationStatus.ToString(),
                r.ChangeScore, r.Description, r.CommitSha, r.CreatedAt)).ToList();

            return new Response(dtos, total, request.Page, request.PageSize);
        }
    }

    /// <summary>DTO de mudança por serviço.</summary>
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
        string? Description,
        string CommitSha,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta paginada de mudanças por serviço.</summary>
    public sealed record Response(
        IReadOnlyList<ChangeDto> Changes,
        int TotalCount,
        int Page,
        int PageSize);
}
