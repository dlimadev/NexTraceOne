using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.GetChangeScore;

/// <summary>
/// Feature: GetChangeScore — retorna o score de risco computado de uma Release.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetChangeScore
{
    /// <summary>Query de consulta do score de mudança de uma Release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de consulta do score de mudança.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna o score de risco computado de uma Release.</summary>
    public sealed class Handler(IChangeScoreRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var score = await repository.GetByReleaseIdAsync(releaseId, cancellationToken);
            if (score is null)
                return ChangeIntelligenceErrors.ChangeScoreNotFound(request.ReleaseId.ToString());

            return new Response(
                score.Id.Value,
                score.ReleaseId.Value,
                score.Score,
                score.BreakingChangeWeight,
                score.BlastRadiusWeight,
                score.EnvironmentWeight,
                score.ComputedAt);
        }
    }

    /// <summary>Resposta com os dados do score de mudança da Release.</summary>
    public sealed record Response(
        Guid ScoreId,
        Guid ReleaseId,
        decimal Score,
        decimal BreakingChangeWeight,
        decimal BlastRadiusWeight,
        decimal EnvironmentWeight,
        DateTimeOffset ComputedAt);
}
