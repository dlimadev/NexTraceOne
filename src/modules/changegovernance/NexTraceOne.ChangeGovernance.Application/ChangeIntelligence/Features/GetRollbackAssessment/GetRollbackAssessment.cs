using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRollbackAssessment;

/// <summary>
/// Feature: GetRollbackAssessment — retorna a avaliação de viabilidade de rollback de uma release.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetRollbackAssessment
{
    /// <summary>Query para obter a avaliação de rollback de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que retorna a avaliação de viabilidade de rollback de uma release.
    /// Retorna erro 404 se a release não existir ou se ainda não houver avaliação.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IRollbackAssessmentRepository assessmentRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);

            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var assessment = await assessmentRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            if (assessment is null)
                return ChangeIntelligenceErrors.RollbackAssessmentNotFound(request.ReleaseId.ToString());

            return new Response(
                assessment.Id.Value,
                assessment.ReleaseId.Value,
                release.ServiceName,
                release.Version,
                release.Environment,
                assessment.IsViable,
                assessment.ReadinessScore,
                assessment.PreviousVersion,
                assessment.HasReversibleMigrations,
                assessment.ConsumersAlreadyMigrated,
                assessment.TotalConsumersImpacted,
                assessment.InviabilityReason,
                assessment.Recommendation,
                assessment.AssessedAt);
        }
    }

    /// <summary>Resposta com os dados da avaliação de rollback.</summary>
    public sealed record Response(
        Guid AssessmentId,
        Guid ReleaseId,
        string ServiceName,
        string Version,
        string Environment,
        bool IsViable,
        decimal ReadinessScore,
        string? PreviousVersion,
        bool HasReversibleMigrations,
        int ConsumersAlreadyMigrated,
        int TotalConsumersImpacted,
        string? InviabilityReason,
        string Recommendation,
        DateTimeOffset AssessedAt);
}
