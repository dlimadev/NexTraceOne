using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Domain.DeveloperExperience;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetDeveloperExperienceScore;

/// <summary>
/// Feature: GetDeveloperExperienceScore — obtém o DX Score de uma equipa por período.
/// </summary>
public static class GetDeveloperExperienceScore
{
    public sealed record Query(string TeamId, string Period) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Period)
                .Must(x => new[] { "weekly", "monthly", "quarterly" }.Contains(x))
                .WithMessage("Valid periods: weekly, monthly, quarterly.");
        }
    }

    public sealed class Handler(IDxScoreRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var score = await repository.GetByTeamAsync(request.TeamId, request.Period, cancellationToken);
            if (score is null)
                return DeveloperExperienceErrors.TeamNotFound(request.TeamId);

            return Result<Response>.Success(new Response(
                score.Id.Value,
                score.TeamId,
                score.TeamName,
                score.ServiceId,
                score.Period,
                score.CycleTimeHours,
                score.DeploymentFrequencyPerWeek,
                score.CognitiveLoadScore,
                score.ToilPercentage,
                score.OverallScore,
                score.ScoreLevel,
                score.Notes,
                score.ComputedAt));
        }
    }

    public sealed record Response(
        Guid ScoreId,
        string TeamId,
        string TeamName,
        string? ServiceId,
        string Period,
        decimal CycleTimeHours,
        decimal DeploymentFrequencyPerWeek,
        decimal CognitiveLoadScore,
        decimal ToilPercentage,
        decimal OverallScore,
        string ScoreLevel,
        string? Notes,
        DateTimeOffset ComputedAt);
}
