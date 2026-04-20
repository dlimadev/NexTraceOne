using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

using SubScoreDto = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ComputeChangeConfidenceBreakdown.ComputeChangeConfidenceBreakdown.SubScoreDto;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeConfidenceBreakdown;

/// <summary>
/// Feature: GetChangeConfidenceBreakdown — retorna o breakdown detalhado do Change Confidence Score 2.0
/// para uma release, incluindo sub-scores auditáveis com citações de fontes.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetChangeConfidenceBreakdown
{
    /// <summary>Query de consulta do breakdown de confiança de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de consulta do breakdown de confiança.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna o breakdown detalhado de confiança de uma release.</summary>
    public sealed class Handler(IChangeConfidenceBreakdownRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var breakdown = await repository.GetByReleaseIdAsync(releaseId, cancellationToken);

            if (breakdown is null)
                return ChangeIntelligenceErrors.ChangeScoreNotFound(request.ReleaseId.ToString());

            var subScoreDtos = breakdown.SubScores
                .Select(s => new SubScoreDto(
                    s.SubScoreType.ToString(),
                    s.Value,
                    s.Weight,
                    s.Confidence.ToString(),
                    s.Reason,
                    s.Citations,
                    s.SimulatedNote))
                .ToList();

            return new Response(
                breakdown.ReleaseId.Value,
                breakdown.Id.Value,
                breakdown.AggregatedScore,
                breakdown.ComputedAt,
                breakdown.ScoreVersion,
                subScoreDtos);
        }
    }

    /// <summary>Resposta com o breakdown detalhado de confiança da release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        Guid BreakdownId,
        decimal AggregatedScore,
        DateTimeOffset ComputedAt,
        string ScoreVersion,
        IReadOnlyList<SubScoreDto> SubScores);
}
