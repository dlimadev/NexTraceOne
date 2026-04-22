using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services.Features.GetServiceLifecycleTransitionReport;

/// <summary>
/// Feature: GetServiceLifecycleTransitionReport — relatório de transições de estado no ciclo de vida dos serviços.
///
/// Para cada serviço do tenant, calcula:
/// - <c>CurrentLifecycleState</c>: estado actual (PreProduction/Active/Deprecating/Deprecated/Retired)
/// - <c>DaysInCurrentState</c>: dias no estado actual
/// - <c>TransitionCount</c>: número de transições no período lookback
/// - <c>StagnationFlag</c>: serviços <c>Deprecated</c> há mais de <c>stagnation_days</c> sem migração de consumidores
/// - <c>AcceleratedRetirementFlag</c>: serviços que passaram de Active para Deprecated/Retired em menos de <c>min_deprecation_days</c>
/// - <c>BlockedTransitionFlag</c>: serviços <c>Deprecated</c> com consumidores Critical/Standard ainda activos
/// - <c>LifecycleDistribution</c>: distribuição de serviços por estado de ciclo de vida
///
/// Orientado para Architect, Platform Admin e Tech Lead — suporta decisões de retirada
/// segura de serviços sem quebrar consumidores.
///
/// Wave AF.1 — GetServiceLifecycleTransitionReport (Catalog Services).
/// </summary>
public static class GetServiceLifecycleTransitionReport
{
    private const int DefaultStagnationDays = 90;
    private const int DefaultMinDeprecationDays = 30;
    private const int DefaultMaxServices = 200;
    private const int DefaultTopCount = 10;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal de análise (1–365, default 90).</para>
    /// <para><c>StagnationDays</c>: dias no estado Deprecated sem progresso para StagnationFlag (1–365, default 90).</para>
    /// <para><c>MinDeprecationDays</c>: dias mínimos esperados antes de Retired para AcceleratedRetirementFlag (1–365, default 30).</para>
    /// <para><c>MaxServices</c>: máximo de serviços no relatório (1–1000, default 200).</para>
    /// <para><c>TopCount</c>: máximo de entradas nas listas top (1–100, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int StagnationDays = DefaultStagnationDays,
        int MinDeprecationDays = DefaultMinDeprecationDays,
        int MaxServices = DefaultMaxServices,
        int TopCount = DefaultTopCount) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Distribuição de serviços por estado de ciclo de vida.</summary>
    public sealed record LifecycleStateDistribution(
        int PreProductionCount,
        int ActiveCount,
        int DeprecatingCount,
        int DeprecatedCount,
        int RetiredCount);

    /// <summary>Perfil de ciclo de vida de um serviço individual.</summary>
    public sealed record ServiceLifecycleProfile(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string ServiceTier,
        ServiceLifecycleState CurrentLifecycleState,
        int DaysInCurrentState,
        int TransitionCount,
        bool StagnationFlag,
        bool AcceleratedRetirementFlag,
        bool BlockedTransitionFlag);

    /// <summary>Relatório de transições de ciclo de vida dos serviços do tenant.</summary>
    public sealed record Report(
        string TenantId,
        int TotalServicesAnalyzed,
        LifecycleStateDistribution LifecycleDistribution,
        int StagnationFlagCount,
        int AcceleratedRetirementFlagCount,
        int BlockedTransitionFlagCount,
        IReadOnlyList<ServiceLifecycleProfile> AllServices,
        IReadOnlyList<ServiceLifecycleProfile> TopStagnatedServices,
        IReadOnlyList<ServiceLifecycleProfile> TopBlockedServices);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.StagnationDays).InclusiveBetween(1, 365);
            RuleFor(q => q.MinDeprecationDays).InclusiveBetween(1, 365);
            RuleFor(q => q.MaxServices).InclusiveBetween(1, 1000);
            RuleFor(q => q.TopCount).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    internal sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IServiceLifecycleReader _lifecycleReader;
        private readonly IDateTimeProvider _clock;

        public Handler(IServiceLifecycleReader lifecycleReader, IDateTimeProvider clock)
        {
            _lifecycleReader = Guard.Against.Null(lifecycleReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var entries = await _lifecycleReader.ListByTenantAsync(
                query.TenantId, query.LookbackDays, ct);

            var now = _clock.UtcNow;
            var profiles = new List<ServiceLifecycleProfile>();

            foreach (var entry in entries.Take(query.MaxServices))
            {
                var daysInState = (int)(now - entry.StateEnteredAt).TotalDays;

                var stagnationFlag =
                    (entry.CurrentState == ServiceLifecycleState.Deprecated ||
                     entry.CurrentState == ServiceLifecycleState.Deprecating) &&
                    daysInState >= query.StagnationDays &&
                    entry.MigratingConsumerCount == 0;

                var acceleratedRetirementFlag =
                    entry.CurrentState == ServiceLifecycleState.Retired &&
                    daysInState <= query.MinDeprecationDays;

                var blockedTransitionFlag =
                    (entry.CurrentState == ServiceLifecycleState.Deprecated ||
                     entry.CurrentState == ServiceLifecycleState.Deprecating) &&
                    entry.ActiveCriticalConsumerCount > 0;

                profiles.Add(new ServiceLifecycleProfile(
                    ServiceId: entry.ServiceId,
                    ServiceName: entry.ServiceName,
                    TeamName: entry.TeamName,
                    ServiceTier: entry.ServiceTier,
                    CurrentLifecycleState: entry.CurrentState,
                    DaysInCurrentState: daysInState,
                    TransitionCount: entry.TransitionCount,
                    StagnationFlag: stagnationFlag,
                    AcceleratedRetirementFlag: acceleratedRetirementFlag,
                    BlockedTransitionFlag: blockedTransitionFlag));
            }

            var dist = new LifecycleStateDistribution(
                PreProductionCount: profiles.Count(p => p.CurrentLifecycleState == ServiceLifecycleState.PreProduction),
                ActiveCount: profiles.Count(p => p.CurrentLifecycleState == ServiceLifecycleState.Active),
                DeprecatingCount: profiles.Count(p => p.CurrentLifecycleState == ServiceLifecycleState.Deprecating),
                DeprecatedCount: profiles.Count(p => p.CurrentLifecycleState == ServiceLifecycleState.Deprecated),
                RetiredCount: profiles.Count(p => p.CurrentLifecycleState == ServiceLifecycleState.Retired));

            var topStagnated = profiles
                .Where(p => p.StagnationFlag)
                .OrderByDescending(p => p.DaysInCurrentState)
                .Take(query.TopCount)
                .ToList();

            var topBlocked = profiles
                .Where(p => p.BlockedTransitionFlag)
                .OrderByDescending(p => p.DaysInCurrentState)
                .Take(query.TopCount)
                .ToList();

            return Result<Report>.Success(new Report(
                TenantId: query.TenantId,
                TotalServicesAnalyzed: profiles.Count,
                LifecycleDistribution: dist,
                StagnationFlagCount: profiles.Count(p => p.StagnationFlag),
                AcceleratedRetirementFlagCount: profiles.Count(p => p.AcceleratedRetirementFlag),
                BlockedTransitionFlagCount: profiles.Count(p => p.BlockedTransitionFlag),
                AllServices: profiles,
                TopStagnatedServices: topStagnated,
                TopBlockedServices: topBlocked));
        }
    }
}
