using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetHistoricalPatternInsight;

/// <summary>
/// Feature: GetHistoricalPatternInsight — analisa o padrão histórico de releases similares
/// para um dado serviço, ambiente e nível de mudança.
///
/// Responde à pergunta: "mudanças similares no passado tiveram X% de falha?"
/// Os dados são usados para enriquecer o advisory de confiança (GetChangeAdvisory)
/// com um sinal baseado em evidência histórica real.
///
/// Algoritmo de similaridade:
///   Critérios exactos  — ServiceName + Environment + ChangeLevel
///   Janela temporal    — configurável, padrão 90 dias anteriores à release
///   Máximo de amostras — 50 releases (suficiente para padrão estatístico básico)
///
/// Resultado inclui:
///   - TotalSamples    — total de releases similares encontradas
///   - SuccessRate     — percentagem de releases que terminaram em Succeeded
///   - RollbackRate    — percentagem de releases que foram RolledBack
///   - FailureRate     — percentagem de releases que terminaram em Failed
///   - AverageScore    — média do ChangeScore das releases similares
///   - PatternRisk     — sinal de risco derivado (Low / Moderate / High / Insufficient)
///   - PatternRationale — explicação textual do sinal
/// </summary>
public static class GetHistoricalPatternInsight
{
    private const int DefaultLookbackDays = 90;
    private const int MaxSamples = 50;
    private const int MinSamplesForSignal = 5;

    /// <summary>Query para obter o padrão histórico de uma release.</summary>
    public sealed record Query(Guid ReleaseId, int? LookbackDays = null) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.LookbackDays)
                .InclusiveBetween(7, 365)
                .When(x => x.LookbackDays.HasValue)
                .WithMessage("LookbackDays must be between 7 and 365.");
        }
    }

    /// <summary>
    /// Handler que consulta releases históricas similares e produz métricas de padrão.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var lookback = request.LookbackDays ?? DefaultLookbackDays;
            var windowEnd = release.CreatedAt;
            var windowStart = windowEnd.AddDays(-lookback);

            var similar = await releaseRepository.ListSimilarReleasesAsync(
                excludeReleaseId: releaseId,
                serviceName: release.ServiceName,
                environment: release.Environment,
                changeLevel: release.ChangeLevel,
                from: windowStart,
                to: windowEnd,
                maxResults: MaxSamples,
                cancellationToken: cancellationToken);

            var metrics = ComputeMetrics(similar);
            var (risk, rationale) = DerivePatternRisk(metrics, release.ChangeLevel);

            return new Response(
                ReleaseId: release.Id.Value,
                ServiceName: release.ServiceName,
                Environment: release.Environment,
                ChangeLevel: release.ChangeLevel.ToString(),
                LookbackDays: lookback,
                WindowStart: windowStart,
                WindowEnd: windowEnd,
                TotalSamples: metrics.TotalSamples,
                SuccessRate: metrics.SuccessRate,
                RollbackRate: metrics.RollbackRate,
                FailureRate: metrics.FailureRate,
                AverageScore: metrics.AverageScore,
                PatternRisk: risk,
                PatternRationale: rationale,
                GeneratedAt: dateTimeProvider.UtcNow);
        }

        private static PatternMetrics ComputeMetrics(IReadOnlyList<Release> releases)
        {
            var total = releases.Count;

            if (total == 0)
                return new PatternMetrics(0, 0m, 0m, 0m, 0m);

            var succeeded = releases.Count(r => r.Status == DeploymentStatus.Succeeded);
            var rolledBack = releases.Count(r => r.Status == DeploymentStatus.RolledBack);
            var failed = releases.Count(r => r.Status == DeploymentStatus.Failed);
            var avgScore = Math.Round(releases.Average(r => r.ChangeScore), 4);

            return new PatternMetrics(
                TotalSamples: total,
                SuccessRate: Math.Round((decimal)succeeded / total, 4),
                RollbackRate: Math.Round((decimal)rolledBack / total, 4),
                FailureRate: Math.Round((decimal)failed / total, 4),
                AverageScore: avgScore);
        }

        private static (string Risk, string Rationale) DerivePatternRisk(
            PatternMetrics metrics,
            ChangeLevel changeLevel)
        {
            if (metrics.TotalSamples < MinSamplesForSignal)
                return (
                    "Insufficient",
                    $"Only {metrics.TotalSamples} similar release(s) found in the lookback window — " +
                    "insufficient data to derive a reliable pattern risk signal.");

            // Combined adverse rate: rollback + failure
            var adverseRate = metrics.RollbackRate + metrics.FailureRate;

            if (adverseRate >= 0.50m)
                return (
                    "High",
                    $"{adverseRate:P0} of similar past {changeLevel} changes in this environment " +
                    $"resulted in rollback or failure ({metrics.TotalSamples} samples). " +
                    "Historical evidence strongly suggests elevated risk.");

            if (adverseRate >= 0.25m)
                return (
                    "Moderate",
                    $"{adverseRate:P0} of similar past {changeLevel} changes in this environment " +
                    $"resulted in rollback or failure ({metrics.TotalSamples} samples). " +
                    "Historical evidence suggests moderate risk — review carefully.");

            return (
                "Low",
                $"{metrics.SuccessRate:P0} success rate on {metrics.TotalSamples} similar past changes. " +
                "Historical pattern is consistent with low deployment risk.");
        }
    }

    private sealed record PatternMetrics(
        int TotalSamples,
        decimal SuccessRate,
        decimal RollbackRate,
        decimal FailureRate,
        decimal AverageScore);

    /// <summary>
    /// Resposta com o padrão histórico de releases similares e o sinal de risco derivado.
    /// </summary>
    public sealed record Response(
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        string ChangeLevel,
        int LookbackDays,
        DateTimeOffset WindowStart,
        DateTimeOffset WindowEnd,
        int TotalSamples,
        decimal SuccessRate,
        decimal RollbackRate,
        decimal FailureRate,
        decimal AverageScore,
        string PatternRisk,
        string PatternRationale,
        DateTimeOffset GeneratedAt);
}
