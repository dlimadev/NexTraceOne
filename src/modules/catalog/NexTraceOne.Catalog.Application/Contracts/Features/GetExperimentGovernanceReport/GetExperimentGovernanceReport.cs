using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetExperimentGovernanceReport;

/// <summary>
/// Feature: GetExperimentGovernanceReport — governança de experimentos A/B do tenant.
///
/// Classifica cada experimento em <c>ExperimentStatus</c>:
/// - Concluded  — <c>ConcludedAt</c> preenchido
/// - Overdue    — duração &gt; <c>ExperimentMaxDays</c>
/// - Stale      — sem toggle há mais de <c>StaleFlagDays</c>
/// - Active     — estado normal
///
/// <c>ExperimentGovernanceTier</c>:
/// - Governed   — overdueRate ≤ 20% E noCriteriaRate ≤ 10%
/// - Improving  — overdueRate ≤ 40% E noCriteriaRate ≤ 25%
/// - AtRisk     — overdueRate ≤ 60%
/// - Unmanaged  — restantes
///
/// Wave AS.3 — Feature Flag &amp; Experimentation Governance (Catalog Contracts).
/// </summary>
public static class GetExperimentGovernanceReport
{
    internal const int DefaultExperimentMaxDays = 30;
    internal const int DefaultStaleFlagDays = 60;

    // ── Query ──────────────────────────────────────────────────────────────
    /// <summary>Query para o relatório de governança de experimentos.</summary>
    public sealed record Query(
        string TenantId,
        int ExperimentMaxDays = DefaultExperimentMaxDays,
        int StaleFlagDays = DefaultStaleFlagDays) : IQuery<Report>;

    /// <summary>Validador da query <see cref="Query"/>.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ExperimentMaxDays).InclusiveBetween(1, 365);
            RuleFor(x => x.StaleFlagDays).InclusiveBetween(1, 730);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    /// <summary>Estado do ciclo de vida do experimento.</summary>
    public enum ExperimentStatus { Active, Overdue, Stale, Concluded }

    /// <summary>Tier de governança de experimentos do tenant.</summary>
    public enum ExperimentGovernanceTier { Governed, Improving, AtRisk, Unmanaged }

    // ── Value objects ──────────────────────────────────────────────────────
    /// <summary>Linha de análise por experimento.</summary>
    public sealed record ExperimentRow(
        string ServiceId,
        string ServiceName,
        string FlagKey,
        int ExperimentDurationDays,
        bool HasSuccessCriteria,
        ExperimentStatus ExperimentStatus,
        int EnvironmentCoverage,
        bool ExperimentProdOnlyRisk);

    /// <summary>Sumário de saúde dos experimentos do tenant.</summary>
    public sealed record ExperimentHealthSummary(
        int ActiveExperiments,
        int OverdueExperiments,
        int ExperimentsWithoutSuccessCriteria,
        double MedianExperimentDurationDays,
        decimal ExperimentVelocity);

    /// <summary>Relatório completo de governança de experimentos.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<ExperimentRow> ByExperiment,
        ExperimentHealthSummary Summary,
        ExperimentGovernanceTier ExperimentGovernanceTier,
        IReadOnlyList<string> LongRunningExperiments,
        IReadOnlyList<string> ExperimentProdOnlyRisk,
        decimal TenantExperimentGovernanceScore,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler da query <see cref="Query"/>.</summary>
    public sealed class Handler(
        IExperimentGovernanceReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListExperimentsByTenantAsync(request.TenantId, cancellationToken);

            var rows = entries.Select(e =>
            {
                var status = DeriveStatus(e, request.ExperimentMaxDays, request.StaleFlagDays, now);
                var prodOnly = e.ActiveEnvironments.Count == 1 &&
                               e.ActiveEnvironments[0].Equals("prod", StringComparison.OrdinalIgnoreCase) ||
                               e.ActiveEnvironments.Count == 1 &&
                               e.ActiveEnvironments[0].StartsWith("production", StringComparison.OrdinalIgnoreCase);

                return new ExperimentRow(
                    e.ServiceId, e.ServiceName, e.FlagKey,
                    e.ExperimentDurationDays, e.HasSuccessCriteria,
                    status, e.ActiveEnvironments.Count, prodOnly);
            }).ToList();

            var total = rows.Count;
            var activeCount = rows.Count(r => r.ExperimentStatus == ExperimentStatus.Active);
            var overdueCount = rows.Count(r => r.ExperimentStatus == ExperimentStatus.Overdue);
            var noCriteriaCount = rows.Count(r => !r.HasSuccessCriteria);
            var concludedCount = rows.Count(r => r.ExperimentStatus == ExperimentStatus.Concluded);

            var overdueRate   = total == 0 ? 0m : Math.Round((decimal)overdueCount   / total * 100m, 2);
            var noCriteriaRate = total == 0 ? 0m : Math.Round((decimal)noCriteriaCount / total * 100m, 2);

            var tier = DeriveGovernanceTier(overdueRate, noCriteriaRate);

            var score = Math.Max(0m, Math.Min(100m,
                Math.Round(100m - overdueRate * 0.60m - noCriteriaRate * 0.40m, 2)));

            var velocity = total == 0 ? 0m : Math.Round((decimal)concludedCount / Math.Max(1, total) * 100m, 2);

            var durations = rows.Select(r => (double)r.ExperimentDurationDays).OrderBy(d => d).ToList();
            var median = ComputeMedian(durations);

            var summary = new ExperimentHealthSummary(
                activeCount, overdueCount, noCriteriaCount, median, velocity);

            var longRunning = rows
                .Where(r => r.ExperimentDurationDays > request.ExperimentMaxDays)
                .Select(r => r.FlagKey)
                .ToList();

            var prodOnlyRisk = rows
                .Where(r => r.ExperimentProdOnlyRisk)
                .Select(r => r.FlagKey)
                .ToList();

            return Result<Report>.Success(new Report(
                request.TenantId, rows, summary, tier,
                longRunning, prodOnlyRisk, score, now));
        }

        private static ExperimentStatus DeriveStatus(
            IExperimentGovernanceReader.ExperimentEntry e,
            int maxDays, int staleDays, DateTimeOffset now)
        {
            if (e.ConcludedAt.HasValue)
                return ExperimentStatus.Concluded;

            if (e.ExperimentDurationDays > maxDays)
                return ExperimentStatus.Overdue;

            var lastActivity = e.LastToggledAt ?? e.CreatedAt;
            if ((now - lastActivity).TotalDays > staleDays)
                return ExperimentStatus.Stale;

            return ExperimentStatus.Active;
        }

        private static ExperimentGovernanceTier DeriveGovernanceTier(decimal overdueRate, decimal noCriteriaRate)
        {
            if (overdueRate <= 20m && noCriteriaRate <= 10m)
                return ExperimentGovernanceTier.Governed;
            if (overdueRate <= 40m && noCriteriaRate <= 25m)
                return ExperimentGovernanceTier.Improving;
            if (overdueRate <= 60m)
                return ExperimentGovernanceTier.AtRisk;
            return ExperimentGovernanceTier.Unmanaged;
        }

        private static double ComputeMedian(IList<double> sorted)
        {
            if (sorted.Count == 0) return 0;
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2.0
                : sorted[mid];
        }
    }
}
