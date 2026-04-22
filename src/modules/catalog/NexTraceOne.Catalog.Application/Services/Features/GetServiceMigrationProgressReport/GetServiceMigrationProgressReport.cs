using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services.Features.GetServiceMigrationProgressReport;

/// <summary>
/// Feature: GetServiceMigrationProgressReport — progresso de migração de consumidores
/// de serviços Deprecated/Sunset para a alternativa designada.
///
/// Para cada serviço com alternativa designada, calcula:
/// - <c>MigrationCompletionRate</c>: MigratedConsumers / TotalConsumers * 100
/// - <c>MigrationTier</c>: Complete ≥100% / Advanced ≥75% / InProgress ≥25% / Lagging &lt;25%
/// - <c>EstimatedCompletionDate</c>: projeção linear baseada na taxa de migração diária
/// - <c>StuckConsumers</c>: consumidores sem sinal de migração há mais de <c>stuck_threshold_days</c>
/// - <c>DailyMigrationTimeline</c>: série temporal de 30 dias de progresso cumulativo
///
/// Orientado para Tech Lead, Architect e Platform Admin — suporta sunset controlado
/// de serviços com visibilidade do progresso de migração por equipa consumidora.
///
/// Wave AF.3 — GetServiceMigrationProgressReport (Catalog Services).
/// </summary>
public static class GetServiceMigrationProgressReport
{
    private const int DefaultLookbackDays = 90;
    private const int DefaultStuckThresholdDays = 30;
    private const int DefaultMaxServices = 100;
    private const int DefaultTopStuckCount = 10;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal de análise (1–365, default 90).</para>
    /// <para><c>StuckThresholdDays</c>: dias sem sinal de migração para StuckConsumer (1–180, default 30).</para>
    /// <para><c>MaxServices</c>: máximo de serviços no relatório (1–500, default 100).</para>
    /// <para><c>TopStuckCount</c>: máximo de consumidores bloqueados a listar (1–50, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        int StuckThresholdDays = DefaultStuckThresholdDays,
        int MaxServices = DefaultMaxServices,
        int TopStuckCount = DefaultTopStuckCount) : IQuery<Report>;

    // ── Enums ─────────────────────────────────────────────────────────────

    /// <summary>Classificação do progresso de migração de consumidores.</summary>
    public enum MigrationTier
    {
        /// <summary>MigrationCompletionRate ≥ 100%. Todos os consumidores migrados.</summary>
        Complete,
        /// <summary>MigrationCompletionRate ≥ 75%. A maioria migrada.</summary>
        Advanced,
        /// <summary>MigrationCompletionRate ≥ 25%. Migração em curso.</summary>
        InProgress,
        /// <summary>MigrationCompletionRate &lt; 25%. Migração com poucos avanços.</summary>
        Lagging
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Distribuição de serviços por tier de migração.</summary>
    public sealed record MigrationTierDistribution(
        int CompleteCount,
        int AdvancedCount,
        int InProgressCount,
        int LaggingCount);

    /// <summary>Ponto diário de progresso cumulativo de migração.</summary>
    public sealed record MigrationTimelinePoint(
        DateOnly Date,
        int CumulativeMigratedCount);

    /// <summary>Consumidor sem sinal de migração no período.</summary>
    public sealed record StuckConsumerEntry(
        string ConsumerServiceName,
        string ConsumerTeamName,
        string ConsumerTier,
        int DaysSinceLastActivity);

    /// <summary>Perfil de progresso de migração para um serviço específico.</summary>
    public sealed record ServiceMigrationProfile(
        string ServiceId,
        string ServiceName,
        string SuccessorServiceName,
        string TeamName,
        string CurrentLifecycleState,
        int TotalConsumers,
        int MigratedConsumers,
        int InProgressConsumers,
        int StuckConsumerCount,
        double MigrationCompletionRate,
        MigrationTier Tier,
        DateOnly? EstimatedCompletionDate,
        IReadOnlyList<StuckConsumerEntry> TopStuckConsumers,
        IReadOnlyList<MigrationTimelinePoint> DailyMigrationTimeline);

    /// <summary>Relatório de progresso de migração do tenant.</summary>
    public sealed record Report(
        string TenantId,
        int TotalDeprecatedServices,
        MigrationTierDistribution TierDistribution,
        double TenantMigrationCompletionRate,
        int TotalStuckConsumers,
        IReadOnlyList<ServiceMigrationProfile> Services,
        IReadOnlyList<ServiceMigrationProfile> TopLaggingServices);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.StuckThresholdDays).InclusiveBetween(1, 180);
            RuleFor(q => q.MaxServices).InclusiveBetween(1, 500);
            RuleFor(q => q.TopStuckCount).InclusiveBetween(1, 50);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    internal sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IMigrationProgressReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(IMigrationProgressReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var entries = await _reader.ListByTenantAsync(
                query.TenantId, query.LookbackDays, ct);

            var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
            var profiles = new List<ServiceMigrationProfile>();

            foreach (var entry in entries.Take(query.MaxServices))
            {
                var stuckConsumers = entry.StuckConsumerDetails
                    .Where(s => s.DaysSinceLastActivity >= query.StuckThresholdDays)
                    .ToList();

                var completionRate = entry.TotalConsumers > 0
                    ? Math.Round((double)entry.MigratedConsumers / entry.TotalConsumers * 100.0, 1)
                    : 100.0;

                var tier = completionRate >= 100.0 ? MigrationTier.Complete
                    : completionRate >= 75.0 ? MigrationTier.Advanced
                    : completionRate >= 25.0 ? MigrationTier.InProgress
                    : MigrationTier.Lagging;

                // Linear projection of completion date
                DateOnly? estimatedCompletion = null;
                var remainingConsumers = entry.TotalConsumers - entry.MigratedConsumers;
                if (remainingConsumers > 0 && entry.DailyTimeline.Count >= 2)
                {
                    var daysElapsed = (today.DayNumber - DateOnly.FromDateTime(entry.StateEnteredAt.UtcDateTime).DayNumber);
                    if (daysElapsed > 0 && entry.MigratedConsumers > 0)
                    {
                        var dailyRate = (double)entry.MigratedConsumers / daysElapsed;
                        var daysToComplete = (int)Math.Ceiling(remainingConsumers / dailyRate);
                        estimatedCompletion = today.AddDays(daysToComplete);
                    }
                }

                var timeline = entry.DailyTimeline
                    .Select(p => new MigrationTimelinePoint(p.Date, p.CumulativeMigratedCount))
                    .ToList();

                var topStuck = stuckConsumers
                    .OrderByDescending(s => s.DaysSinceLastActivity)
                    .Take(query.TopStuckCount)
                    .Select(s => new StuckConsumerEntry(
                        s.ConsumerServiceName, s.ConsumerTeamName, s.ConsumerTier, s.DaysSinceLastActivity))
                    .ToList();

                profiles.Add(new ServiceMigrationProfile(
                    ServiceId: entry.ServiceId,
                    ServiceName: entry.ServiceName,
                    SuccessorServiceName: entry.SuccessorServiceName,
                    TeamName: entry.TeamName,
                    CurrentLifecycleState: entry.CurrentLifecycleState,
                    TotalConsumers: entry.TotalConsumers,
                    MigratedConsumers: entry.MigratedConsumers,
                    InProgressConsumers: entry.InProgressConsumers,
                    StuckConsumerCount: stuckConsumers.Count,
                    MigrationCompletionRate: completionRate,
                    Tier: tier,
                    EstimatedCompletionDate: estimatedCompletion,
                    TopStuckConsumers: topStuck,
                    DailyMigrationTimeline: timeline));
            }

            var dist = new MigrationTierDistribution(
                CompleteCount: profiles.Count(p => p.Tier == MigrationTier.Complete),
                AdvancedCount: profiles.Count(p => p.Tier == MigrationTier.Advanced),
                InProgressCount: profiles.Count(p => p.Tier == MigrationTier.InProgress),
                LaggingCount: profiles.Count(p => p.Tier == MigrationTier.Lagging));

            var tenantRate = profiles.Count > 0
                ? Math.Round(profiles.Average(p => p.MigrationCompletionRate), 1)
                : 0.0;

            var topLagging = profiles
                .Where(p => p.Tier is MigrationTier.Lagging or MigrationTier.InProgress)
                .OrderBy(p => p.MigrationCompletionRate)
                .Take(10)
                .ToList();

            return Result<Report>.Success(new Report(
                TenantId: query.TenantId,
                TotalDeprecatedServices: profiles.Count,
                TierDistribution: dist,
                TenantMigrationCompletionRate: tenantRate,
                TotalStuckConsumers: profiles.Sum(p => p.StuckConsumerCount),
                Services: profiles,
                TopLaggingServices: topLagging));
        }
    }
}
