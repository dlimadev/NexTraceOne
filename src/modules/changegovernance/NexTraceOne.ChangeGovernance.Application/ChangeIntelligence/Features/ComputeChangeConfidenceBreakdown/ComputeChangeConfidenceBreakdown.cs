using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ConfigurationKeys;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ComputeChangeConfidenceBreakdown;

/// <summary>
/// Feature: ComputeChangeConfidenceBreakdown — calcula e persiste o breakdown detalhado
/// do Change Confidence Score 2.0 para uma release, com sub-scores auditáveis e citações de fontes.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ComputeChangeConfidenceBreakdown
{
    /// <summary>Comando para computar e persistir o breakdown de confiança de uma release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        string Environment,
        int BlastSurfaceConsumers,
        bool CanaryAvailable,
        decimal CanaryErrorRate,
        bool PreProdBaselineAvailable,
        decimal PreProdDeltaPercent,
        decimal TestCoveragePercent,
        int ContractBreakingChanges,
        int HistoricalRegressionCount) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de computação do breakdown de confiança.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(200);
            RuleFor(x => x.BlastSurfaceConsumers).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CanaryErrorRate).InclusiveBetween(0m, 100m);
            RuleFor(x => x.PreProdDeltaPercent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.TestCoveragePercent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.ContractBreakingChanges).GreaterThanOrEqualTo(0);
            RuleFor(x => x.HistoricalRegressionCount).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>
    /// Handler que computa os 7 sub-scores do Change Confidence Score 2.0, cria o breakdown
    /// e persiste na base de dados.
    /// </summary>
    public sealed class Handler(
        IChangeConfidenceBreakdownRepository repository,
        IReleaseRepository releaseRepository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IConfigurationResolutionService configService) : ICommandHandler<Command, Response>
    {
        private const decimal DefaultWeightTestCoverage = 0.15m;
        private const decimal DefaultWeightContractStability = 0.20m;
        private const decimal DefaultWeightHistoricalRegression = 0.15m;
        private const decimal DefaultWeightBlastSurface = 0.15m;
        private const decimal DefaultWeightDependencyHealth = 0.10m;
        private const decimal DefaultWeightCanarySignal = 0.10m;
        private const decimal DefaultWeightPreProdDelta = 0.15m;

        // Penalidades por ocorrência — valores conservadores calibrados para 0-100 scale
        private const decimal PenaltyPerBreakingChange = 25m;
        private const decimal PenaltyPerHistoricalRegression = 20m;
        private const decimal PenaltyPerBlastConsumer = 5m;

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var weights = await ResolveWeightsAsync(cancellationToken);
            var subScores = BuildSubScores(request, weights);

            var breakdown = ChangeConfidenceBreakdown.Create(releaseId, subScores, dateTimeProvider.UtcNow);
            repository.Add(breakdown);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                breakdown.ReleaseId.Value,
                breakdown.AggregatedScore,
                breakdown.ComputedAt,
                breakdown.SubScores
                    .Select(s => new SubScoreDto(
                        s.SubScoreType.ToString(),
                        s.Value,
                        s.Weight,
                        s.Confidence.ToString(),
                        s.Reason,
                        s.Citations,
                        s.SimulatedNote))
                    .ToList());
        }

        private async Task<WeightSet> ResolveWeightsAsync(CancellationToken ct)
        {
            static decimal ParseWeight(string? raw, decimal fallback)
                => decimal.TryParse(raw, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var v) && v > 0 && v <= 10 ? v : fallback;

            var testCovDto = await configService.ResolveEffectiveValueAsync(ChangeConfigKeys.ConfidenceWeightTestCoverage, ConfigurationScope.System, null, ct);
            var contractDto = await configService.ResolveEffectiveValueAsync(ChangeConfigKeys.ConfidenceWeightContractStability, ConfigurationScope.System, null, ct);
            var histDto = await configService.ResolveEffectiveValueAsync(ChangeConfigKeys.ConfidenceWeightHistoricalRegression, ConfigurationScope.System, null, ct);
            var blastDto = await configService.ResolveEffectiveValueAsync(ChangeConfigKeys.ConfidenceWeightBlastSurface, ConfigurationScope.System, null, ct);
            var depDto = await configService.ResolveEffectiveValueAsync(ChangeConfigKeys.ConfidenceWeightDependencyHealth, ConfigurationScope.System, null, ct);
            var canaryDto = await configService.ResolveEffectiveValueAsync(ChangeConfigKeys.ConfidenceWeightCanarySignal, ConfigurationScope.System, null, ct);
            var preProdDto = await configService.ResolveEffectiveValueAsync(ChangeConfigKeys.ConfidenceWeightPreProdDelta, ConfigurationScope.System, null, ct);

            return new WeightSet(
                ParseWeight(testCovDto?.EffectiveValue, DefaultWeightTestCoverage),
                ParseWeight(contractDto?.EffectiveValue, DefaultWeightContractStability),
                ParseWeight(histDto?.EffectiveValue, DefaultWeightHistoricalRegression),
                ParseWeight(blastDto?.EffectiveValue, DefaultWeightBlastSurface),
                ParseWeight(depDto?.EffectiveValue, DefaultWeightDependencyHealth),
                ParseWeight(canaryDto?.EffectiveValue, DefaultWeightCanarySignal),
                ParseWeight(preProdDto?.EffectiveValue, DefaultWeightPreProdDelta));
        }

        private static List<ChangeConfidenceSubScore> BuildSubScores(Command req, WeightSet w)
        {
            var scores = new List<ChangeConfidenceSubScore>(7);

            // TestCoverage
            var testValue = Math.Min(req.TestCoveragePercent, 100m);
            var testConfidence = req.TestCoveragePercent > 0m ? ConfidenceDataQuality.High : ConfidenceDataQuality.Low;
            scores.Add(ChangeConfidenceSubScore.Create(
                ConfidenceSubScoreType.TestCoverage,
                testValue,
                w.TestCoverage,
                testConfidence,
                $"Test coverage reported by CI pipeline: {req.TestCoveragePercent:F1}%",
                ["citation://integrations/ci/test-coverage"]));

            // ContractStability
            var contractValue = Math.Max(0m, 100m - req.ContractBreakingChanges * PenaltyPerBreakingChange);
            scores.Add(ChangeConfidenceSubScore.Create(
                ConfidenceSubScoreType.ContractStability,
                contractValue,
                w.ContractStability,
                ConfidenceDataQuality.High,
                $"{req.ContractBreakingChanges} breaking contract change(s) detected, reducing stability score by {req.ContractBreakingChanges * PenaltyPerBreakingChange}pts",
                ["citation://catalog/contracts/breaking-changes"]));

            // HistoricalRegression
            var histValue = Math.Max(0m, 100m - req.HistoricalRegressionCount * PenaltyPerHistoricalRegression);
            scores.Add(ChangeConfidenceSubScore.Create(
                ConfidenceSubScoreType.HistoricalRegression,
                histValue,
                w.HistoricalRegression,
                ConfidenceDataQuality.Medium,
                $"{req.HistoricalRegressionCount} historical regression(s) found for this service, reducing score by {req.HistoricalRegressionCount * PenaltyPerHistoricalRegression}pts",
                ["citation://operationalintelligence/incidents/regression"]));

            // BlastSurface
            var blastValue = req.BlastSurfaceConsumers == 0
                ? 100m
                : Math.Max(0m, 100m - req.BlastSurfaceConsumers * PenaltyPerBlastConsumer);
            scores.Add(ChangeConfidenceSubScore.Create(
                ConfidenceSubScoreType.BlastSurface,
                blastValue,
                w.BlastSurface,
                ConfidenceDataQuality.High,
                $"Blast surface of {req.BlastSurfaceConsumers} consumer(s) detected",
                ["citation://changegovernance/blast-radius"]));

            // DependencyHealth — placeholder (real provider not yet integrated)
            scores.Add(ChangeConfidenceSubScore.Create(
                ConfidenceSubScoreType.DependencyHealth,
                75m,
                w.DependencyHealth,
                ConfidenceDataQuality.Low,
                "Dependency health uses a conservative placeholder until real provider data is available",
                ["citation://operationalintelligence/reliability"],
                simulatedNote: "Pending real dependency health data"));

            // CanarySignal
            if (req.CanaryAvailable)
            {
                var canaryValue = Math.Max(0m, 100m - req.CanaryErrorRate * 100m);
                scores.Add(ChangeConfidenceSubScore.Create(
                    ConfidenceSubScoreType.CanarySignal,
                    canaryValue,
                    w.CanarySignal,
                    ConfidenceDataQuality.High,
                    $"Canary error rate of {req.CanaryErrorRate:P1} observed",
                    ["citation://changegovernance/canary-rollout"]));
            }
            else
            {
                scores.Add(ChangeConfidenceSubScore.Create(
                    ConfidenceSubScoreType.CanarySignal,
                    50m,
                    w.CanarySignal,
                    ConfidenceDataQuality.Low,
                    "Canary signal not available — conservative value applied",
                    ["citation://changegovernance/canary-rollout"],
                    simulatedNote: "ICanaryProvider not configured"));
            }

            // PreProdDelta
            if (req.PreProdBaselineAvailable)
            {
                var preProdValue = Math.Max(0m, 100m - req.PreProdDeltaPercent * 100m);
                scores.Add(ChangeConfidenceSubScore.Create(
                    ConfidenceSubScoreType.PreProdDelta,
                    preProdValue,
                    w.PreProdDelta,
                    ConfidenceDataQuality.High,
                    $"Pre-prod baseline delta of {req.PreProdDeltaPercent:P1} detected",
                    ["citation://operationalintelligence/runtime-baseline"]));
            }
            else
            {
                scores.Add(ChangeConfidenceSubScore.Create(
                    ConfidenceSubScoreType.PreProdDelta,
                    50m,
                    w.PreProdDelta,
                    ConfidenceDataQuality.Low,
                    "Pre-prod baseline not available — conservative value applied",
                    ["citation://operationalintelligence/runtime-baseline"],
                    simulatedNote: "No pre-prod baseline available"));
            }

            return scores;
        }

        private sealed record WeightSet(
            decimal TestCoverage,
            decimal ContractStability,
            decimal HistoricalRegression,
            decimal BlastSurface,
            decimal DependencyHealth,
            decimal CanarySignal,
            decimal PreProdDelta);
    }

    /// <summary>DTO de sub-score para a resposta da feature.</summary>
    public sealed record SubScoreDto(
        string SubScoreType,
        decimal Value,
        decimal Weight,
        string Confidence,
        string Reason,
        IReadOnlyList<string> Citations,
        string? SimulatedNote);

    /// <summary>Resposta com os dados do breakdown de confiança computado.</summary>
    public sealed record Response(
        Guid ReleaseId,
        decimal AggregatedScore,
        DateTimeOffset ComputedAt,
        IReadOnlyList<SubScoreDto> SubScores);
}
