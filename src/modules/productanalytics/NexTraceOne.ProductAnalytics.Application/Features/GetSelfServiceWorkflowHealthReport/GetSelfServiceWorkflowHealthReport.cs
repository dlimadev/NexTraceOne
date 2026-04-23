using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetSelfServiceWorkflowHealthReport;

/// <summary>
/// Análise da saúde dos workflows de self-service do NexTraceOne.
/// Responde: os developers conseguem completar workflows (CreateService, CreateContractDraft, RequestPromotion…)
/// sem fricção e sem depender de um admin — ou existem friction points, abandono e AdminDependencyIndex elevado?
/// Orientado para Platform Admin e Product.
/// </summary>
public static class GetSelfServiceWorkflowHealthReport
{
    // ── Enums ─────────────────────────────────────────────────────────────

    /// <summary>Tier de saúde de um workflow de self-service.</summary>
    public enum WorkflowHealthTier
    {
        /// <summary>CompletionRate ≥ smooth_completion_rate e AdminIntervention ≤ smooth_admin_rate.</summary>
        Smooth,
        /// <summary>CompletionRate razoável mas com alguma fricção ou intervenção admin moderada.</summary>
        Functional,
        /// <summary>CompletionRate <90% ou AdminIntervention >5% mas não Broken.</summary>
        FrictionHeavy,
        /// <summary>CompletionRate <50%.</summary>
        Broken
    }

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Parâmetros de consulta do relatório de saúde de workflows de self-service.
    /// </summary>
    /// <param name="TenantId">Identificador do tenant.</param>
    /// <param name="LookbackDays">Janela de análise em dias. Padrão: 30.</param>
    /// <param name="SmoothCompletionRate">% mínima de CompletionRate para tier Smooth. Padrão: 90.</param>
    /// <param name="SmoothAdminRate">% máxima de AdminInterventionRate para tier Smooth. Padrão: 5.</param>
    /// <param name="BrokenCompletionRate">% máxima de CompletionRate para tier Broken. Padrão: 50.</param>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        decimal SmoothCompletionRate = 90m,
        decimal SmoothAdminRate = 5m,
        decimal BrokenCompletionRate = 50m) : IQuery<Response>;

    // ── Response ──────────────────────────────────────────────────────────

    /// <summary>Resposta do relatório de saúde de workflows de self-service.</summary>
    /// <param name="Workflows">Análise por workflow.</param>
    /// <param name="Summary">Sumário de saúde de self-service no tenant.</param>
    /// <param name="WorkflowAbandonmentHotspots">Etapas específicas com maior frequência de abandono.</param>
    /// <param name="SlowestWorkflows">Top 5 workflows por AvgCompletionTimeMinutes.</param>
    /// <param name="WorkflowTrendByFeatureRelease">Variação de CompletionRate após releases da plataforma.</param>
    public sealed record Response(
        IReadOnlyList<WorkflowHealthResult> Workflows,
        TenantSelfServiceHealthSummary Summary,
        IReadOnlyList<AbandonmentHotspotDto> WorkflowAbandonmentHotspots,
        IReadOnlyList<WorkflowHealthResult> SlowestWorkflows,
        IReadOnlyList<WorkflowReleaseSnapshotDto> WorkflowTrendByFeatureRelease);

    /// <summary>Resultado de saúde de um workflow de self-service.</summary>
    public sealed record WorkflowHealthResult(
        string WorkflowName,
        int AttemptCount,
        decimal CompletionRate,
        decimal AbandonmentRate,
        decimal AdminInterventionRate,
        double AvgCompletionTimeMinutes,
        WorkflowHealthTier HealthTier);

    /// <summary>Sumário de saúde de self-service no tenant.</summary>
    public sealed record TenantSelfServiceHealthSummary(
        decimal OverallSelfServiceScore,
        IReadOnlyList<string> FrictionWorkflows,
        decimal AdminDependencyIndex,
        int SmoothWorkflowCount,
        int BrokenWorkflowCount);

    /// <summary>Hotspot de abandono numa etapa específica de workflow.</summary>
    public sealed record AbandonmentHotspotDto(
        string WorkflowName,
        string StepName,
        int AbandonCount,
        string Description);

    /// <summary>Variação de CompletionRate após release de plataforma.</summary>
    public sealed record WorkflowReleaseSnapshotDto(
        string ReleaseLabel,
        DateTimeOffset ReleasedAt,
        decimal AvgCompletionRate);

    // ── Validator ─────────────────────────────────────────────────────────

    /// <summary>Validador da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        /// <summary>Inicializa regras de validação.</summary>
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.LookbackDays).GreaterThan(0);
            RuleFor(x => x.SmoothCompletionRate).InclusiveBetween(0m, 100m);
            RuleFor(x => x.SmoothAdminRate).InclusiveBetween(0m, 100m);
            RuleFor(x => x.BrokenCompletionRate).InclusiveBetween(0m, 100m);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    /// <summary>Handler que calcula a saúde dos workflows de self-service.</summary>
    public sealed class Handler(
        ISelfServiceWorkflowReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        /// <inheritdoc/>
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var from = now.AddDays(-request.LookbackDays);

            var execTask = reader.ListByTenantAsync(request.TenantId, from, now, cancellationToken);
            var hotspotsTask = reader.GetAbandonmentHotspotsAsync(request.TenantId, from, now, cancellationToken);
            var releaseTask = reader.GetTrendByReleaseAsync(request.TenantId, from, now, cancellationToken);
            await Task.WhenAll(execTask, hotspotsTask, releaseTask);

            var execEntries = execTask.Result;
            var hotspots = hotspotsTask.Result;
            var releaseTrend = releaseTask.Result;

            var workflowResults = execEntries
                .Select(e => BuildWorkflowResult(e, request))
                .ToList();

            // ── Summary ──────────────────────────────────────────────────
            var totalAttempts = workflowResults.Sum(w => w.AttemptCount);
            var overallScore = workflowResults.Count > 0
                ? Math.Round(ComputeWeightedScore(workflowResults), 1)
                : 100m;

            var frictionWorkflows = workflowResults
                .Where(w => w.HealthTier is WorkflowHealthTier.FrictionHeavy or WorkflowHealthTier.Broken)
                .Select(w => w.WorkflowName)
                .ToList();

            var adminDependencyIndex = totalAttempts > 0
                ? Math.Round(workflowResults
                    .Sum(w => w.AdminInterventionRate * w.AttemptCount) / totalAttempts, 1)
                : 0m;

            var summary = new TenantSelfServiceHealthSummary(
                OverallSelfServiceScore: overallScore,
                FrictionWorkflows: frictionWorkflows,
                AdminDependencyIndex: adminDependencyIndex,
                SmoothWorkflowCount: workflowResults.Count(w => w.HealthTier == WorkflowHealthTier.Smooth),
                BrokenWorkflowCount: workflowResults.Count(w => w.HealthTier == WorkflowHealthTier.Broken));

            var slowestWorkflows = workflowResults
                .OrderByDescending(w => w.AvgCompletionTimeMinutes)
                .Take(5)
                .ToList();

            var hotspotDtos = hotspots
                .Select(h => new AbandonmentHotspotDto(h.WorkflowName, h.StepName, h.AbandonCount, h.Description))
                .ToList();

            var releaseDtos = releaseTrend
                .Select(r => new WorkflowReleaseSnapshotDto(r.ReleaseLabel, r.ReleasedAt, (decimal)r.AvgCompletionRate))
                .ToList();

            return Result<Response>.Success(new Response(
                Workflows: workflowResults,
                Summary: summary,
                WorkflowAbandonmentHotspots: hotspotDtos,
                SlowestWorkflows: slowestWorkflows,
                WorkflowTrendByFeatureRelease: releaseDtos));
        }

        private WorkflowHealthResult BuildWorkflowResult(
            ISelfServiceWorkflowReader.WorkflowExecutionEntry e,
            Query request)
        {
            if (e.AttemptCount == 0)
                return new WorkflowHealthResult(e.WorkflowName, 0, 100m, 0m, 0m, 0, WorkflowHealthTier.Smooth);

            var completionRate = Math.Round((decimal)e.SuccessfulCompletions / e.AttemptCount * 100m, 1);
            var abandonmentRate = Math.Round((decimal)e.AbandonedCount / e.AttemptCount * 100m, 1);
            var adminRate = Math.Round((decimal)e.AdminInterventionCount / e.AttemptCount * 100m, 1);

            var tier = completionRate < request.BrokenCompletionRate
                ? WorkflowHealthTier.Broken
                : completionRate >= request.SmoothCompletionRate && adminRate <= request.SmoothAdminRate
                    ? WorkflowHealthTier.Smooth
                    : completionRate >= (request.BrokenCompletionRate + request.SmoothCompletionRate) / 2m
                        ? WorkflowHealthTier.Functional
                        : WorkflowHealthTier.FrictionHeavy;

            return new WorkflowHealthResult(
                WorkflowName: e.WorkflowName,
                AttemptCount: e.AttemptCount,
                CompletionRate: completionRate,
                AbandonmentRate: abandonmentRate,
                AdminInterventionRate: adminRate,
                AvgCompletionTimeMinutes: e.AvgCompletionTimeMinutes,
                HealthTier: tier);
        }

        private static decimal ComputeWeightedScore(List<WorkflowHealthResult> workflows)
        {
            var totalWeight = workflows.Sum(w => (decimal)w.AttemptCount);
            if (totalWeight <= 0) return 100m;
            return workflows.Sum(w => TierScore(w.HealthTier) * w.AttemptCount) / totalWeight;
        }

        private static decimal TierScore(WorkflowHealthTier tier) => tier switch
        {
            WorkflowHealthTier.Smooth => 100m,
            WorkflowHealthTier.Functional => 75m,
            WorkflowHealthTier.FrictionHeavy => 40m,
            WorkflowHealthTier.Broken => 0m,
            _ => 0m
        };
    }
}
