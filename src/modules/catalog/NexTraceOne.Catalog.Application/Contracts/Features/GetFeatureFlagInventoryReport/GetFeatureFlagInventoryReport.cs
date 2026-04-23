using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetFeatureFlagInventoryReport;

/// <summary>
/// Feature: GetFeatureFlagInventoryReport — inventário governado de feature flags por tenant.
///
/// Agrupa as flags por serviço e calcula:
/// - distribuição por <c>FlagType</c>
/// - <c>StaleFlagsCount</c> — flags sem toggle há mais de <c>StaleFlagDays</c>
/// - <c>OwnerlessFlags</c> — flags sem <c>OwnerId</c>
/// - <c>FlagsInAllEnvironments</c> — flags activas em todos os ambientes declarados
/// - <c>FlagsByEnvironment</c> — contagem de flags activas por ambiente
///
/// Wave AS.1 — Feature Flag &amp; Experimentation Governance (Catalog Contracts).
/// </summary>
public static class GetFeatureFlagInventoryReport
{
    internal const int DefaultStaleFlagDays = 60;

    // ── Query ──────────────────────────────────────────────────────────────
    /// <summary>Query para o relatório de inventário de feature flags.</summary>
    public sealed record Query(
        string TenantId,
        int StaleFlagDays = DefaultStaleFlagDays) : IQuery<Report>;

    /// <summary>Validador da query <see cref="Query"/>.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.StaleFlagDays).InclusiveBetween(1, 730);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    /// <summary>Tipo de feature flag.</summary>
    public enum FlagType { Release, Experiment, Permission, KillSwitch }

    // ── Value objects ──────────────────────────────────────────────────────
    /// <summary>Linha de inventário por serviço.</summary>
    public sealed record ServiceFlagRow(
        string ServiceId,
        string ServiceName,
        int TotalFlags,
        int ActiveFlags,
        IReadOnlyDictionary<FlagType, int> ByType,
        int StaleFlagsCount,
        int OwnerlessFlags,
        int FlagsInAllEnvironments);

    /// <summary>Sumário global de feature flags do tenant.</summary>
    public sealed record TenantFeatureFlagSummary(
        int TotalFlags,
        int ActiveFlags,
        int StaleFlags,
        int OwnerlessFlags,
        int KillSwitchCount,
        IReadOnlyList<ServiceFlagRow> TopServicesWithStaleFlags);

    /// <summary>Relatório completo de inventário de feature flags.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<ServiceFlagRow> ByService,
        TenantFeatureFlagSummary Summary,
        IReadOnlyDictionary<string, int> FlagsByEnvironment,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler da query <see cref="Query"/>.</summary>
    public sealed class Handler(
        IFeatureFlagRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var flags = await repository.ListByTenantAsync(request.TenantId, cancellationToken);

            var staleThreshold = now.AddDays(-request.StaleFlagDays);

            var byService = flags
                .GroupBy(f => f.ServiceId)
                .Select(g =>
                {
                    var serviceFlags = g.ToList();
                    var activeFlags = serviceFlags.Count(f => f.IsEnabled);

                    var byType = serviceFlags
                        .GroupBy(f => MapFlagType(f.Type))
                        .ToDictionary(t => t.Key, t => t.Count());

                    foreach (FlagType ft in Enum.GetValues<FlagType>())
                        byType.TryAdd(ft, 0);

                    var staleCount = serviceFlags.Count(f =>
                        f.LastToggledAt.HasValue
                            ? f.LastToggledAt.Value < staleThreshold
                            : f.CreatedAt < staleThreshold);

                    var ownerless = serviceFlags.Count(f => f.OwnerId == null);

                    // Contagem de ambientes únicos activos no serviço
                    var allEnvs = serviceFlags
                        .Where(f => f.IsEnabled && f.EnabledEnvironmentsJson != null)
                        .SelectMany(f => ParseEnvironments(f.EnabledEnvironmentsJson!))
                        .Distinct()
                        .ToHashSet();

                    var flagsInAllEnvs = allEnvs.Count == 0 ? 0 : serviceFlags.Count(f =>
                        f.IsEnabled &&
                        f.EnabledEnvironmentsJson != null &&
                        ParseEnvironments(f.EnabledEnvironmentsJson).ToHashSet().IsSupersetOf(allEnvs));

                    var serviceName = g.Key; // sem reader de nome — usa serviceId como fallback

                    return new ServiceFlagRow(g.Key, serviceName, serviceFlags.Count,
                        activeFlags, byType, staleCount, ownerless, flagsInAllEnvs);
                })
                .ToList();

            // FlagsByEnvironment — contagem de flags activas por ambiente
            var flagsByEnv = flags
                .Where(f => f.IsEnabled && f.EnabledEnvironmentsJson != null)
                .SelectMany(f => ParseEnvironments(f.EnabledEnvironmentsJson!))
                .GroupBy(env => env)
                .ToDictionary(g => g.Key, g => g.Count());

            var totalFlags = flags.Count;
            var totalActive = flags.Count(f => f.IsEnabled);
            var totalStale = byService.Sum(s => s.StaleFlagsCount);
            var totalOwnerless = byService.Sum(s => s.OwnerlessFlags);
            var killSwitchCount = flags.Count(f => f.Type == FeatureFlagRecord.FlagType.KillSwitch);

            var topStale = byService
                .OrderByDescending(s => s.StaleFlagsCount)
                .Take(5)
                .ToList();

            var summary = new TenantFeatureFlagSummary(
                totalFlags, totalActive, totalStale, totalOwnerless, killSwitchCount, topStale);

            return Result<Report>.Success(new Report(
                request.TenantId, byService, summary, flagsByEnv, now));
        }

        private static FlagType MapFlagType(FeatureFlagRecord.FlagType t) => t switch
        {
            FeatureFlagRecord.FlagType.Release    => FlagType.Release,
            FeatureFlagRecord.FlagType.Experiment => FlagType.Experiment,
            FeatureFlagRecord.FlagType.Permission => FlagType.Permission,
            FeatureFlagRecord.FlagType.KillSwitch => FlagType.KillSwitch,
            _                                     => FlagType.Release
        };

        private static IReadOnlyList<string> ParseEnvironments(string json)
        {
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
            catch { return []; }
        }
    }
}
