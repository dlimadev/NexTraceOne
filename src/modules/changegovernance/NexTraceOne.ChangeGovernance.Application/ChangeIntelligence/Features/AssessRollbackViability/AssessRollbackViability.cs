using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AssessRollbackViability;

/// <summary>
/// Feature: AssessRollbackViability — avalia viabilidade de rollback de uma release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class AssessRollbackViability
{
    /// <summary>Comando para avaliar viabilidade de rollback de uma release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        bool IsViable,
        string? PreviousVersion,
        bool HasReversibleMigrations,
        int ConsumersAlreadyMigrated,
        int TotalConsumersImpacted,
        string? InviabilityReason,
        string Recommendation) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de avaliação de rollback.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.Recommendation).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.ConsumersAlreadyMigrated).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalConsumersImpacted).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>
    /// Handler que avalia a viabilidade de rollback de uma release.
    /// Calcula readiness score baseado nos fatores de rollback disponíveis.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IRollbackAssessmentRepository assessmentRepository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var readinessScore = CalculateReadinessScore(request);

            var assessment = RollbackAssessment.Create(
                releaseId,
                request.IsViable,
                readinessScore,
                request.PreviousVersion,
                request.HasReversibleMigrations,
                request.ConsumersAlreadyMigrated,
                request.TotalConsumersImpacted,
                request.InviabilityReason,
                request.Recommendation,
                dateTimeProvider.UtcNow);

            assessmentRepository.Add(assessment);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                assessment.Id.Value,
                release.Id.Value,
                assessment.IsViable,
                assessment.ReadinessScore,
                assessment.Recommendation,
                assessment.AssessedAt);
        }

        /// <summary>
        /// Calcula o score de readiness de rollback baseado nos fatores disponíveis.
        /// Score mais alto = rollback mais seguro e viável.
        /// </summary>
        private static decimal CalculateReadinessScore(Command request)
        {
            if (!request.IsViable) return 0m;

            var score = 0.5m;

            if (request.PreviousVersion is not null)
                score += 0.15m;

            if (request.HasReversibleMigrations)
                score += 0.15m;

            if (request.TotalConsumersImpacted > 0 && request.ConsumersAlreadyMigrated == 0)
                score += 0.1m;
            else if (request.TotalConsumersImpacted > 0)
                score -= 0.1m * ((decimal)request.ConsumersAlreadyMigrated / request.TotalConsumersImpacted);

            return Math.Clamp(score, 0m, 1m);
        }
    }

    /// <summary>Resposta da avaliação de rollback.</summary>
    public sealed record Response(
        Guid AssessmentId,
        Guid ReleaseId,
        bool IsViable,
        decimal ReadinessScore,
        string Recommendation,
        DateTimeOffset AssessedAt);
}
