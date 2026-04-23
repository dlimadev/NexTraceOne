using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;

namespace NexTraceOne.Integrations.Application.Features.GetIntegrationHealthReport;

/// <summary>
/// Análise da saúde das integrações activas do NexTraceOne com sistemas externos.
/// Responde: as integrações com GitLab, Jenkins, Azure DevOps, OIDC e outros sistemas estão saudáveis —
/// ou existem falhas, atrasos e sincronizações incompletas que degradam a qualidade dos dados?
/// Orientado para Platform Admin e Architect.
/// </summary>
public static class GetIntegrationHealthReport
{
    // ── Enums ─────────────────────────────────────────────────────────────

    /// <summary>Estado de frescura dos dados de uma integração.</summary>
    public enum DataFreshnessStatus
    {
        /// <summary>SyncAge ≤ sync_freshness_hours.</summary>
        Fresh,
        /// <summary>SyncAge entre freshness e 2×freshness.</summary>
        Aging,
        /// <summary>SyncAge entre 2× e 4×freshness.</summary>
        Stale,
        /// <summary>SyncSuccessRate = 0 recentemente ou sem sincronização.</summary>
        Offline
    }

    /// <summary>Tier de saúde de uma integração.</summary>
    public enum IntegrationHealthTier
    {
        /// <summary>SyncSuccessRate ≥95% e DataFreshnessStatus = Fresh.</summary>
        Healthy,
        /// <summary>SyncSuccessRate ≥70% ou DataFreshnessStatus = Aging.</summary>
        Degraded,
        /// <summary>SyncSuccessRate <70% mas >0.</summary>
        Failing,
        /// <summary>SyncSuccessRate = 0 ou sem sincronização recente.</summary>
        Offline
    }

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Parâmetros de consulta do relatório de saúde das integrações.
    /// </summary>
    /// <param name="TenantId">Identificador do tenant.</param>
    /// <param name="SyncHealthWindowHours">Janela de horas para calcular SyncSuccessRate. Padrão: 72.</param>
    /// <param name="FreshnessHours">Horas máximas de SyncAge para DataFreshnessStatus Fresh. Padrão: 24.</param>
    /// <param name="HealthyMinSyncRate">% mínima de SyncSuccessRate para tier Healthy. Padrão: 95.</param>
    /// <param name="DegradedMinSyncRate">% mínima de SyncSuccessRate para tier Degraded. Padrão: 70.</param>
    public sealed record Query(
        string TenantId,
        int SyncHealthWindowHours = 72,
        int FreshnessHours = 24,
        decimal HealthyMinSyncRate = 95m,
        decimal DegradedMinSyncRate = 70m) : IQuery<Response>;

    // ── Response ──────────────────────────────────────────────────────────

    /// <summary>Resposta do relatório de saúde das integrações.</summary>
    /// <param name="Integrations">Análise por integração activa.</param>
    /// <param name="Summary">Sumário de saúde de integrações no tenant.</param>
    /// <param name="DataFreshnessImpact">Impacto de integrações Stale/Offline nos dados do NexTraceOne.</param>
    /// <param name="IntegrationHealthHistory">Histórico de 7 dias de tier de saúde por integração.</param>
    /// <param name="TopErrorIntegrations">Integrações com maior volume de erros de sincronização.</param>
    public sealed record Response(
        IReadOnlyList<IntegrationHealthResult> Integrations,
        TenantIntegrationHealthSummary Summary,
        IReadOnlyList<DataFreshnessImpactDto> DataFreshnessImpact,
        IReadOnlyList<IntegrationHealthHistoryDto> IntegrationHealthHistory,
        IReadOnlyList<IntegrationHealthResult> TopErrorIntegrations);

    /// <summary>Resultado de saúde de uma integração activa.</summary>
    public sealed record IntegrationHealthResult(
        string IntegrationId,
        string IntegrationName,
        string IntegrationType,
        DateTimeOffset? LastSyncAt,
        double SyncAgeHours,
        decimal SyncSuccessRate,
        string? LastErrorMessage,
        DataFreshnessStatus FreshnessStatus,
        IntegrationHealthTier HealthTier,
        bool IsCritical);

    /// <summary>Sumário de saúde de integrações no tenant.</summary>
    public sealed record TenantIntegrationHealthSummary(
        int HealthyIntegrations,
        int DegradedIntegrations,
        int FailingIntegrations,
        int OfflineIntegrations,
        decimal TenantIntegrationHealthScore,
        IReadOnlyList<string> CriticalOfflineIntegrations);

    /// <summary>Impacto de integração degradada nos dados do NexTraceOne.</summary>
    public sealed record DataFreshnessImpactDto(
        string IntegrationName,
        DataFreshnessStatus FreshnessStatus,
        IReadOnlyList<string> AffectedFeatures,
        string ImpactDescription);

    /// <summary>Snapshot de saúde de uma integração num dia específico.</summary>
    public sealed record IntegrationHealthHistoryDto(
        string IntegrationId,
        string IntegrationName,
        int DaysAgo,
        IntegrationHealthTier HealthTier);

    // ── Validator ─────────────────────────────────────────────────────────

    /// <summary>Validador da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        /// <summary>Inicializa regras de validação.</summary>
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.SyncHealthWindowHours).GreaterThan(0);
            RuleFor(x => x.FreshnessHours).GreaterThan(0);
            RuleFor(x => x.HealthyMinSyncRate).InclusiveBetween(0m, 100m);
            RuleFor(x => x.DegradedMinSyncRate).InclusiveBetween(0m, 100m);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    /// <summary>Handler que calcula a saúde das integrações activas.</summary>
    public sealed class Handler(
        IIntegrationSyncReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        /// <inheritdoc/>
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var from = now.AddHours(-request.SyncHealthWindowHours);
            var historyFrom = now.AddDays(-7);

            var entriesTask = reader.ListByTenantAsync(request.TenantId, from, now, cancellationToken);
            var historyTask = reader.GetHealthHistoryAsync(request.TenantId, historyFrom, now, cancellationToken);
            await Task.WhenAll(entriesTask, historyTask);

            var entries = entriesTask.Result;
            var historyEntries = historyTask.Result;

            var results = entries.Select(e => BuildResult(e, now, request)).ToList();

            // ── Summary ──────────────────────────────────────────────────
            var healthyCount = results.Count(r => r.HealthTier == IntegrationHealthTier.Healthy);
            var healthScore = results.Count > 0
                ? Math.Round((decimal)healthyCount / results.Count * 100m, 1)
                : 100m;

            var criticalOffline = results
                .Where(r => r.IsCritical && r.HealthTier == IntegrationHealthTier.Offline)
                .Select(r => r.IntegrationName)
                .ToList();

            var summary = new TenantIntegrationHealthSummary(
                HealthyIntegrations: healthyCount,
                DegradedIntegrations: results.Count(r => r.HealthTier == IntegrationHealthTier.Degraded),
                FailingIntegrations: results.Count(r => r.HealthTier == IntegrationHealthTier.Failing),
                OfflineIntegrations: results.Count(r => r.HealthTier == IntegrationHealthTier.Offline),
                TenantIntegrationHealthScore: healthScore,
                CriticalOfflineIntegrations: criticalOffline);

            // ── Data Freshness Impact ─────────────────────────────────────
            var freshnessImpact = results
                .Where(r => r.FreshnessStatus is DataFreshnessStatus.Stale or DataFreshnessStatus.Offline)
                .Select(r =>
                {
                    var entry = entries.First(e => e.IntegrationId == r.IntegrationId);
                    return new DataFreshnessImpactDto(
                        r.IntegrationName,
                        r.FreshnessStatus,
                        entry.AffectedFeatures,
                        $"Data from {r.IntegrationName} may be up to {r.SyncAgeHours:F0}h out of date");
                })
                .ToList();

            // ── History ───────────────────────────────────────────────────
            var historyDtos = historyEntries
                .Select(h => new IntegrationHealthHistoryDto(
                    h.IntegrationId,
                    h.IntegrationName,
                    h.DaysAgo,
                    ParseTier(h.HealthTier)))
                .ToList();

            // ── Top Error Integrations ────────────────────────────────────
            var topErrors = results
                .Where(r => r.HealthTier is IntegrationHealthTier.Failing or IntegrationHealthTier.Offline)
                .OrderBy(r => r.SyncSuccessRate)
                .Take(5)
                .ToList();

            return Result<Response>.Success(new Response(
                Integrations: results,
                Summary: summary,
                DataFreshnessImpact: freshnessImpact,
                IntegrationHealthHistory: historyDtos,
                TopErrorIntegrations: topErrors));
        }

        private IntegrationHealthResult BuildResult(
            IIntegrationSyncReader.IntegrationSyncEntry entry,
            DateTimeOffset now,
            Query request)
        {
            var syncAgeHours = entry.LastSyncAt.HasValue
                ? (now - entry.LastSyncAt.Value).TotalHours
                : double.MaxValue;

            var syncSuccessRate = entry.TotalSyncsInWindow > 0
                ? Math.Round((decimal)entry.SuccessfulSyncsInWindow / entry.TotalSyncsInWindow * 100m, 1)
                : 0m;

            var freshnessStatus = syncSuccessRate == 0m || entry.LastSyncAt is null
                ? DataFreshnessStatus.Offline
                : syncAgeHours <= request.FreshnessHours
                    ? DataFreshnessStatus.Fresh
                    : syncAgeHours <= request.FreshnessHours * 2
                        ? DataFreshnessStatus.Aging
                        : DataFreshnessStatus.Stale;

            var healthTier = freshnessStatus == DataFreshnessStatus.Offline
                ? IntegrationHealthTier.Offline
                : syncSuccessRate >= request.HealthyMinSyncRate && freshnessStatus == DataFreshnessStatus.Fresh
                    ? IntegrationHealthTier.Healthy
                    : syncSuccessRate >= request.DegradedMinSyncRate
                        ? IntegrationHealthTier.Degraded
                        : IntegrationHealthTier.Failing;

            return new IntegrationHealthResult(
                IntegrationId: entry.IntegrationId,
                IntegrationName: entry.IntegrationName,
                IntegrationType: entry.IntegrationType,
                LastSyncAt: entry.LastSyncAt,
                SyncAgeHours: syncAgeHours == double.MaxValue ? -1 : Math.Round(syncAgeHours, 1),
                SyncSuccessRate: syncSuccessRate,
                LastErrorMessage: entry.LastErrorMessage,
                FreshnessStatus: freshnessStatus,
                HealthTier: healthTier,
                IsCritical: entry.IsCritical);
        }

        private static IntegrationHealthTier ParseTier(string raw) =>
            Enum.TryParse<IntegrationHealthTier>(raw, true, out var parsed) ? parsed : IntegrationHealthTier.Offline;
    }
}
