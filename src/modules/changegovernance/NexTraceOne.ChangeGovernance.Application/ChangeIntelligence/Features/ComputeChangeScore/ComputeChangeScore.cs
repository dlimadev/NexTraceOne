using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.ComputeChangeScore;

/// <summary>
/// Feature: ComputeChangeScore — computa o score de risco de uma Release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ComputeChangeScore
{
    /// <summary>Comando de computação do score de mudança de uma Release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        decimal BreakingChangeWeight,
        decimal BlastRadiusWeight,
        decimal EnvironmentWeight) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de computação do score de mudança.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.BreakingChangeWeight).InclusiveBetween(0m, 1m);
            RuleFor(x => x.BlastRadiusWeight).InclusiveBetween(0m, 1m);
            RuleFor(x => x.EnvironmentWeight).InclusiveBetween(0m, 1m);
        }
    }

    /// <summary>Handler que computa e persiste o score de risco de uma Release.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeScoreRepository scoreRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await releaseRepository.GetByIdAsync(ReleaseId.From(request.ReleaseId), cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var score = ChangeIntelligenceScore.Compute(
                release.Id,
                request.BreakingChangeWeight,
                request.BlastRadiusWeight,
                request.EnvironmentWeight,
                dateTimeProvider.UtcNow);

            var setResult = release.SetChangeScore(score.Score);
            if (setResult.IsFailure)
                return setResult.Error;

            scoreRepository.Add(score);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(release.Id.Value, score.Score, score.ComputedAt);
        }
    }

    /// <summary>Resposta da computação do score de mudança da Release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        decimal Score,
        DateTimeOffset ComputedAt);
}
