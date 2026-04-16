using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.EvaluateReleaseTrain;

/// <summary>
/// Feature: EvaluateReleaseTrain — avalia uma composição de múltiplas releases como um "Release Train"
/// coordenado entre vários serviços.
///
/// Um Release Train é um conjunto de releases de múltiplos serviços que devem ser promovidos juntos,
/// por exemplo, numa data acordada (como num sprint release). Esta feature compõe uma visão consolidada:
///   - Lista de releases com seu estado atual
///   - Score de risco agregado (média ponderada)
///   - Blast radius combinado (union dos consumidores afectados)
///   - Readiness signal: Ready / PartiallyReady / NotReady
///   - Serviços bloqueadores (releases com score alto ou estado de falha)
///
/// A feature é stateless — não persiste o train. O estado é calculado on-demand
/// a partir dos releaseIds fornecidos.
///
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class EvaluateReleaseTrain
{
    private const decimal HighRiskThreshold = 0.7m;
    private const decimal ReadyScoreThreshold = 0.6m;

    /// <summary>Comando para avaliar um Release Train.</summary>
    public sealed record Command(
        string TrainName,
        IReadOnlyList<Guid> ReleaseIds) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de avaliação de Release Train.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TrainName)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.ReleaseIds)
                .NotEmpty()
                .Must(ids => ids.Count >= 2)
                .WithMessage("A Release Train must contain at least 2 releases.")
                .Must(ids => ids.Count <= 50)
                .WithMessage("A Release Train cannot contain more than 50 releases.")
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Release IDs must be unique within a Release Train.");
        }
    }

    /// <summary>Handler que compõe a visão de um Release Train a partir das releases fornecidas.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IBlastRadiusRepository blastRadiusRepository,
        IChangeScoreRepository scoreRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseItems = new List<TrainReleaseItem>(request.ReleaseIds.Count);
            var allAffectedConsumers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var blockers = new List<string>();
            var notFoundIds = new List<Guid>();

            foreach (var releaseId in request.ReleaseIds)
            {
                var id = ReleaseId.From(releaseId);
                var release = await releaseRepository.GetByIdAsync(id, cancellationToken);

                if (release is null)
                {
                    notFoundIds.Add(releaseId);
                    continue;
                }

                var score = await scoreRepository.GetByReleaseIdAsync(id, cancellationToken);
                var blastRadius = await blastRadiusRepository.GetByReleaseIdAsync(id, cancellationToken);

                var riskScore = score?.Score;
                var isHighRisk = riskScore.HasValue && riskScore.Value >= HighRiskThreshold;
                var isFailedOrRolledBack =
                    release.Status.ToString() is "Failed" or "RolledBack";

                if (isHighRisk || isFailedOrRolledBack)
                    blockers.Add(release.ServiceName);

                if (blastRadius is not null)
                {
                    foreach (var consumer in blastRadius.DirectConsumers)
                        allAffectedConsumers.Add(consumer);
                    foreach (var consumer in blastRadius.TransitiveConsumers)
                        allAffectedConsumers.Add(consumer);
                }

                releaseItems.Add(new TrainReleaseItem(
                    ReleaseId: release.Id.Value,
                    ServiceName: release.ServiceName,
                    Version: release.Version,
                    Environment: release.Environment,
                    Status: release.Status.ToString(),
                    ChangeLevel: release.ChangeLevel.ToString(),
                    RiskScore: riskScore,
                    IsHighRisk: isHighRisk,
                    TotalAffectedConsumers: blastRadius?.TotalAffectedConsumers ?? 0,
                    CreatedAt: release.CreatedAt));
            }

            var scoredItems = releaseItems.Where(r => r.RiskScore.HasValue).ToList();
            decimal? aggregateScore = scoredItems.Count > 0
                ? Math.Round(scoredItems.Average(r => r.RiskScore!.Value), 4)
                : null;

            var readiness = DeriveReadiness(releaseItems, blockers, notFoundIds, request.ReleaseIds.Count);

            return new Response(
                TrainName: request.TrainName,
                RequestedCount: request.ReleaseIds.Count,
                FoundCount: releaseItems.Count,
                NotFoundIds: notFoundIds,
                Releases: releaseItems,
                AggregateRiskScore: aggregateScore,
                CombinedAffectedConsumers: allAffectedConsumers.Count,
                BlockingServices: blockers.Distinct().ToList(),
                Readiness: readiness,
                EvaluatedAt: dateTimeProvider.UtcNow);
        }

        private static string DeriveReadiness(
            IReadOnlyList<TrainReleaseItem> releases,
            IReadOnlyList<string> blockers,
            IReadOnlyList<Guid> notFound,
            int totalRequested)
        {
            if (notFound.Count > 0)
                return "NotReady";

            if (blockers.Count > 0)
                return "NotReady";

            var highRiskCount = releases.Count(r => r.IsHighRisk);
            if (highRiskCount > 0)
                return "PartiallyReady";

            var unscoredCount = releases.Count(r => !r.RiskScore.HasValue);
            if (unscoredCount > 0)
                return "PartiallyReady";

            return "Ready";
        }
    }

    /// <summary>Item de release dentro do Release Train.</summary>
    public sealed record TrainReleaseItem(
        Guid ReleaseId,
        string ServiceName,
        string Version,
        string Environment,
        string Status,
        string ChangeLevel,
        decimal? RiskScore,
        bool IsHighRisk,
        int TotalAffectedConsumers,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta da avaliação de um Release Train.</summary>
    public sealed record Response(
        string TrainName,
        int RequestedCount,
        int FoundCount,
        IReadOnlyList<Guid> NotFoundIds,
        IReadOnlyList<TrainReleaseItem> Releases,
        decimal? AggregateRiskScore,
        int CombinedAffectedConsumers,
        IReadOnlyList<string> BlockingServices,
        string Readiness,
        DateTimeOffset EvaluatedAt);
}
