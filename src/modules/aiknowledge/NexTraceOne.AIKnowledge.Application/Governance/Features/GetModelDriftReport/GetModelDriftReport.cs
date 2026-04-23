using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetModelDriftReport;

/// <summary>
/// Feature: GetModelDriftReport — detecção de drift de modelos de IA em produção.
///
/// Compara a distribuição de inputs e outputs actual vs. o período baseline para cada
/// modelo do tenant. Calcula InputDriftScore, OutputDriftScore e ConfidenceDrift,
/// classificando cada modelo num <c>ModelDriftTier</c> (Stable/Warning/Drifting/Critical).
///
/// Thresholds configuráveis via IConfigurationResolutionService:
/// - <c>ai.model_drift.input_drift_warning_score</c> (default 20) — InputDrift para Warning
/// - <c>ai.model_drift.output_drift_warning_score</c> (default 15) — OutputDrift para Warning
///
/// Wave AT.1 — AI Model Quality &amp; Drift Governance (AIKnowledge Governance).
/// </summary>
public static class GetModelDriftReport
{
    // ── Configuration keys ─────────────────────────────────────────────────
    internal const string InputDriftWarningKey = "ai.model_drift.input_drift_warning_score";
    internal const string OutputDriftWarningKey = "ai.model_drift.output_drift_warning_score";
    internal const int DefaultInputDriftWarning = 20;
    internal const int DefaultOutputDriftWarning = 15;

    // ── Tier thresholds (relative to warning) ─────────────────────────────
    // Warning = config. Drifting = 2x warning. Critical = 3x warning.
    private const double DriftingMultiplier = 2.0;
    private const double CriticalMultiplier = 3.0;

    internal const int DefaultBaselineDays = 30;
    internal const int DefaultTimelineDays = 30;

    // ── Query ──────────────────────────────────────────────────────────────
    /// <summary>Query para o relatório de drift de modelos de IA.</summary>
    public sealed record Query(
        string TenantId,
        int BaselineDays = DefaultBaselineDays,
        int CurrentWindowDays = DefaultTimelineDays) : IQuery<Report>;

    /// <summary>Validador da query <see cref="Query"/>.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.BaselineDays).InclusiveBetween(7, 180);
            RuleFor(x => x.CurrentWindowDays).InclusiveBetween(7, 90);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    /// <summary>Tier de drift de modelo de IA.</summary>
    public enum ModelDriftTier { Stable, Warning, Drifting, Critical }

    /// <summary>Algoritmo utilizado para detecção de drift.</summary>
    public enum DriftDetectionAlgorithm { PsiSimplified, KsTestSimplified }

    // ── Value objects ──────────────────────────────────────────────────────
    /// <summary>Linha de drift por modelo.</summary>
    public sealed record ModelDriftRow(
        Guid ModelId,
        string ModelName,
        string ServiceId,
        double InputDriftScore,
        double OutputDriftScore,
        double ConfidenceDrift,
        double NullRateIncrease,
        DriftDetectionAlgorithm Algorithm,
        ModelDriftTier Tier,
        int SampleCount,
        bool DriftAcknowledged);

    /// <summary>Ponto da série temporal de drift (diário).</summary>
    public sealed record DriftTimelinePoint(
        DateTimeOffset Date,
        double InputDriftScore,
        double OutputDriftScore);

    /// <summary>Alerta de drift — modelos com tier Critical sem reconhecimento.</summary>
    public sealed record DriftAlert(
        Guid ModelId,
        string ModelName,
        double InputDriftScore,
        double OutputDriftScore,
        ModelDriftTier Tier);

    /// <summary>Sumário global de drift do tenant.</summary>
    public sealed record TenantModelDriftSummary(
        int TotalModels,
        int StableModels,
        int DriftingModels,
        int CriticalModels,
        IReadOnlyList<ModelDriftRow> TopDriftingModels);

    /// <summary>Relatório completo de drift de modelos de IA.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<ModelDriftRow> ByModel,
        TenantModelDriftSummary Summary,
        IReadOnlyList<DriftAlert> DriftAlerts,
        IReadOnlyList<DriftTimelinePoint> TimelineForTopModel,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler da query <see cref="Query"/>.</summary>
    public sealed class Handler(
        IModelDriftReader driftReader,
        IConfigurationResolutionService configService,
        NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;

            // Resolve config thresholds
            var inputWarnCfg = await configService.ResolveEffectiveValueAsync(
                InputDriftWarningKey, ConfigurationScope.System, null, cancellationToken);
            var outputWarnCfg = await configService.ResolveEffectiveValueAsync(
                OutputDriftWarningKey, ConfigurationScope.System, null, cancellationToken);

            var inputWarning = double.TryParse(inputWarnCfg?.EffectiveValue, out var iw)
                ? iw : DefaultInputDriftWarning;
            var outputWarning = double.TryParse(outputWarnCfg?.EffectiveValue, out var ow)
                ? ow : DefaultOutputDriftWarning;

            // Time windows
            var currentTo = now;
            var currentFrom = now.AddDays(-request.CurrentWindowDays);
            var baselineTo = currentFrom;
            var baselineFrom = baselineTo.AddDays(-request.BaselineDays);

            var rows = await driftReader.GetDriftRowsAsync(
                request.TenantId, baselineFrom, baselineTo, currentFrom, currentTo, cancellationToken);

            // Classify drift tiers
            var classifiedRows = rows
                .Select(r => r with { Tier = ClassifyTier(r.InputDriftScore, r.OutputDriftScore, inputWarning, outputWarning) })
                .ToList();

            // Build summary
            var stableCount = classifiedRows.Count(r => r.Tier == ModelDriftTier.Stable);
            var driftingCount = classifiedRows.Count(r => r.Tier is ModelDriftTier.Drifting or ModelDriftTier.Warning);
            var criticalCount = classifiedRows.Count(r => r.Tier == ModelDriftTier.Critical);
            var topDrifting = classifiedRows
                .OrderByDescending(r => r.InputDriftScore)
                .Take(5)
                .ToList();

            var summary = new TenantModelDriftSummary(
                TotalModels: classifiedRows.Count,
                StableModels: stableCount,
                DriftingModels: driftingCount,
                CriticalModels: criticalCount,
                TopDriftingModels: topDrifting);

            // Drift alerts — Critical + not acknowledged
            var alerts = classifiedRows
                .Where(r => r.Tier == ModelDriftTier.Critical && !r.DriftAcknowledged)
                .Select(r => new DriftAlert(r.ModelId, r.ModelName, r.InputDriftScore, r.OutputDriftScore, r.Tier))
                .ToList();

            // Timeline for the top drifting model
            IReadOnlyList<DriftTimelinePoint> timeline = [];
            var topModel = classifiedRows.OrderByDescending(r => r.InputDriftScore).FirstOrDefault();
            if (topModel is not null)
            {
                timeline = await driftReader.GetDriftTimelineAsync(
                    topModel.ModelId, request.TenantId, currentFrom, currentTo, cancellationToken);
            }

            return Result<Report>.Success(new Report(
                TenantId: request.TenantId,
                ByModel: classifiedRows,
                Summary: summary,
                DriftAlerts: alerts,
                TimelineForTopModel: timeline,
                GeneratedAt: now));
        }

        private static ModelDriftTier ClassifyTier(
            double inputDrift, double outputDrift, double inputWarning, double outputWarning)
        {
            var inputCritical = inputWarning * CriticalMultiplier;
            var outputCritical = outputWarning * CriticalMultiplier;
            var inputDrifting = inputWarning * DriftingMultiplier;
            var outputDrifting = outputWarning * DriftingMultiplier;

            if (inputDrift >= inputCritical || outputDrift >= outputCritical)
                return ModelDriftTier.Critical;
            if (inputDrift >= inputDrifting || outputDrift >= outputDrifting)
                return ModelDriftTier.Drifting;
            if (inputDrift >= inputWarning || outputDrift >= outputWarning)
                return ModelDriftTier.Warning;
            return ModelDriftTier.Stable;
        }
    }
}
