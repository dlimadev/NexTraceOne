using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Features.ListDeveloperExperienceScores;

/// <summary>
/// Feature: ListDeveloperExperienceScores — lista os DX Scores com filtros opcionais e paginação.
/// </summary>
public static class ListDeveloperExperienceScores
{
    public sealed record Query(string? Period, string? ScoreLevel, int Page = 1, int PageSize = 20) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Period).MaximumLength(50).When(x => x.Period is not null);
            RuleFor(x => x.ScoreLevel).MaximumLength(50).When(x => x.ScoreLevel is not null);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(IDxScoreRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var items = await repository.ListAsync(request.Period, request.ScoreLevel, cancellationToken);
            var totalCount = items.Count;
            var paged = items
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new DxScoreSummary(
                    s.Id.Value, s.TeamId, s.TeamName, s.Period,
                    s.OverallScore, s.ScoreLevel, s.DeploymentFrequencyPerWeek,
                    s.CycleTimeHours, s.ComputedAt))
                .ToList();

            return Result<Response>.Success(new Response(totalCount, request.Page, request.PageSize, paged));
        }
    }

    public sealed record DxScoreSummary(
        Guid ScoreId, string TeamId, string TeamName, string Period,
        decimal OverallScore, string ScoreLevel,
        decimal DeploymentFrequencyPerWeek, decimal CycleTimeHours,
        DateTimeOffset ComputedAt);

    public sealed record Response(int TotalCount, int Page, int PageSize, IReadOnlyList<DxScoreSummary> Items);
}
