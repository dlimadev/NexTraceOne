using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Features.ComputeDeveloperExperienceScore;

/// <summary>
/// Feature: ComputeDeveloperExperienceScore — calcula e persiste o DX Score de uma equipa.
/// </summary>
public static class ComputeDeveloperExperienceScore
{
    public sealed record Command(
        string TeamId,
        string TeamName,
        string? ServiceId,
        string Period,
        decimal CycleTimeHours,
        decimal DeploymentFrequencyPerWeek,
        decimal CognitivLoadScore,
        decimal ToilPercentage,
        string? Notes) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Period)
                .Must(x => new[] { "weekly", "monthly", "quarterly" }.Contains(x))
                .WithMessage("Valid periods: weekly, monthly, quarterly.");
            RuleFor(x => x.CycleTimeHours).GreaterThan(0m);
            RuleFor(x => x.DeploymentFrequencyPerWeek).GreaterThanOrEqualTo(0m);
            RuleFor(x => x.CognitivLoadScore).InclusiveBetween(0m, 10m);
            RuleFor(x => x.ToilPercentage).InclusiveBetween(0m, 100m);
        }
    }

    public sealed class Handler(
        IDxScoreRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var result = DxScore.Create(
                request.TeamId,
                request.TeamName,
                request.ServiceId,
                request.Period,
                request.CycleTimeHours,
                request.DeploymentFrequencyPerWeek,
                request.CognitivLoadScore,
                request.ToilPercentage,
                request.Notes,
                clock.UtcNow);

            if (!result.IsSuccess) return result.Error;

            repository.Add(result.Value!);
            await unitOfWork.CommitAsync(cancellationToken);

            var score = result.Value!;
            return Result<Response>.Success(new Response(
                score.Id.Value,
                score.TeamId,
                score.TeamName,
                score.Period,
                score.OverallScore,
                score.ScoreLevel,
                score.DeploymentFrequencyPerWeek,
                score.CycleTimeHours,
                score.ComputedAt));
        }
    }

    public sealed record Response(
        Guid ScoreId,
        string TeamId,
        string TeamName,
        string Period,
        decimal OverallScore,
        string ScoreLevel,
        decimal DeploymentFrequencyPerWeek,
        decimal CycleTimeHours,
        DateTimeOffset ComputedAt);
}
