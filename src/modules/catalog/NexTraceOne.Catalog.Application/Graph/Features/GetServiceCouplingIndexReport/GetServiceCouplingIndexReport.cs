using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetServiceCouplingIndexReport;

/// <summary>
/// Feature: GetServiceCouplingIndexReport — índice de acoplamento estrutural entre serviços.
///
/// Analisa o grafo de APIs registadas (<see cref="IApiAssetRepository.ListAllAsync"/>)
/// para derivar fan-in e fan-out de cada serviço:
/// - <c>FanIn</c>  — número de consumidores distintos que dependem de APIs publicadas por este serviço
/// - <c>FanOut</c> — número de serviços distintos cujas APIs este serviço consome
/// - <c>CouplingIndex</c> = min(100, (FanIn × 3 + FanOut × 2) / sqrt(max(1, TotalServices)) × 10)
///
/// Classifica por <c>CouplingTier</c>:
/// - <c>HubService</c>        — CouplingIndex ≥ 70 (alto fan-in, alto blast radius)
/// - <c>HighlyCoupled</c>     — CouplingIndex ≥ 50
/// - <c>ModeratelyCoupled</c> — CouplingIndex ≥ 25
/// - <c>LooselyCoupled</c>    — CouplingIndex ≥ 10
/// - <c>Isolated</c>          — CouplingIndex &lt; 10 e FanIn = 0 e FanOut = 0
///
/// Sinaliza:
/// - <c>ArchitecturalRisk</c> — HubService com tier Critical e FanIn ≥ 5
/// - <c>IsolationRisk</c>     — Isolated com tier Standard ou superior
///
/// Produz CouplingIndex médio, % de serviços Isolated, top hub services e top acoplados.
///
/// Orienta Architect e Platform Admin na análise de blast radius e decomposição de serviços.
///
/// Wave W.2 — Service Coupling Index Report (Catalog Graph).
/// </summary>
public static class GetServiceCouplingIndexReport
{
    private const decimal HubThreshold = 70m;
    private const decimal HighlyCoupledThreshold = 50m;
    private const decimal ModeratelyCoupledThreshold = 25m;
    private const decimal LooselyCoupledThreshold = 10m;
    private const int ArchitecturalRiskMinFanIn = 5;

    /// <summary>
    /// <para><c>MaxTopServices</c>: número máximo de serviços em cada ranking (1–100, default 10).</para>
    /// </summary>
    public sealed record Query(int MaxTopServices = 10) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Tier de acoplamento de um serviço.</summary>
    public enum CouplingTier
    {
        /// <summary>Sem dependências registadas — isolado do ecossistema.</summary>
        Isolated,
        /// <summary>Acoplamento baixo (CouplingIndex &lt; 10).</summary>
        LooselyCoupled,
        /// <summary>Acoplamento moderado (CouplingIndex 25–49).</summary>
        ModeratelyCoupled,
        /// <summary>Alto acoplamento (CouplingIndex 50–69) — alto fan-out.</summary>
        HighlyCoupled,
        /// <summary>Hub service (CouplingIndex ≥ 70) — alto fan-in e blast radius elevado.</summary>
        HubService
    }

    /// <summary>Distribuição global de serviços por CouplingTier.</summary>
    public sealed record TierDistribution(
        int IsolatedCount,
        int LooselyCoupledCount,
        int ModeratelyCoupledCount,
        int HighlyCoupledCount,
        int HubServiceCount);

    /// <summary>Entrada de acoplamento de um serviço.</summary>
    public sealed record ServiceCouplingEntry(
        string ServiceName,
        string TeamName,
        ServiceTierType Tier,
        int FanIn,
        int FanOut,
        decimal CouplingIndex,
        CouplingTier CouplingTier,
        bool ArchitecturalRisk,
        bool IsolationRisk);

    /// <summary>Resultado do relatório de acoplamento de serviços.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int TotalServicesAnalyzed,
        decimal AvgCouplingIndex,
        decimal IsolatedServicePct,
        TierDistribution Distribution,
        IReadOnlyList<ServiceCouplingEntry> TopHubServices,
        IReadOnlyList<ServiceCouplingEntry> TopHighlyCoupledServices,
        IReadOnlyList<ServiceCouplingEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IServiceAssetRepository _serviceRepo;
        private readonly IApiAssetRepository _apiRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IServiceAssetRepository serviceRepo,
            IApiAssetRepository apiRepo,
            IDateTimeProvider clock)
        {
            _serviceRepo = Guard.Against.Null(serviceRepo);
            _apiRepo = Guard.Against.Null(apiRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = _clock.UtcNow;

            // 1. Load all services and all APIs (with consumer relationships eagerly loaded)
            var services = await _serviceRepo.ListAllAsync(cancellationToken);
            var apis = await _apiRepo.ListAllAsync(cancellationToken);

            if (services.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    TotalServicesAnalyzed: 0,
                    AvgCouplingIndex: 0m,
                    IsolatedServicePct: 0m,
                    Distribution: new TierDistribution(0, 0, 0, 0, 0),
                    TopHubServices: [],
                    TopHighlyCoupledServices: [],
                    AllServices: []));
            }

            int totalServices = services.Count;
            double sqrtTotal = Math.Sqrt(Math.Max(1, totalServices));

            // 2. Build service name → service lookup
            var serviceByName = services
                .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // 3. Build fan-in/fan-out maps from the API graph
            //    FanIn(S)  = distinct consumer names of APIs where OwnerService == S
            //    FanOut(S) = distinct provider service names of APIs consumed by S
            //
            //    API.OwnerService.Name → provider
            //    ConsumerRelationship.ConsumerName → consumer of that API
            //
            //    FanIn (S) = how many distinct consumers use any API owned by S
            //    FanOut(S) = how many distinct service providers S consumes from

            // Build: serviceProviderName → set of distinct consumerNames
            var fanInMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            // Build: consumerName → set of distinct provider serviceNames
            var fanOutMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var api in apis)
            {
                var providerName = api.OwnerService?.Name;
                if (string.IsNullOrWhiteSpace(providerName)) continue;

                if (!fanInMap.ContainsKey(providerName))
                    fanInMap[providerName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var cr in api.ConsumerRelationships)
                {
                    var consumerName = cr.ConsumerName;
                    if (string.IsNullOrWhiteSpace(consumerName)) continue;

                    // FanIn of provider: count distinct consumers
                    fanInMap[providerName].Add(consumerName);

                    // FanOut of consumer: count distinct providers
                    if (!fanOutMap.ContainsKey(consumerName))
                        fanOutMap[consumerName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    fanOutMap[consumerName].Add(providerName);
                }
            }

            // 4. Build entries for every known service
            var entries = new List<ServiceCouplingEntry>(totalServices);

            foreach (var svc in services)
            {
                int fanIn = fanInMap.TryGetValue(svc.Name, out var consumers) ? consumers.Count : 0;
                int fanOut = fanOutMap.TryGetValue(svc.Name, out var providers) ? providers.Count : 0;

                decimal couplingIndex = Math.Min(100m,
                    (decimal)((fanIn * 3 + fanOut * 2) / sqrtTotal * 10));
                couplingIndex = Math.Round(couplingIndex, 1);

                var tier = ClassifyTier(couplingIndex, fanIn, fanOut);
                bool architecturalRisk = tier == CouplingTier.HubService
                    && svc.Tier == ServiceTierType.Critical
                    && fanIn >= ArchitecturalRiskMinFanIn;
                bool isolationRisk = tier == CouplingTier.Isolated
                    && svc.Tier != ServiceTierType.Experimental;

                entries.Add(new ServiceCouplingEntry(
                    ServiceName: svc.Name,
                    TeamName: svc.TeamName,
                    Tier: svc.Tier,
                    FanIn: fanIn,
                    FanOut: fanOut,
                    CouplingIndex: couplingIndex,
                    CouplingTier: tier,
                    ArchitecturalRisk: architecturalRisk,
                    IsolationRisk: isolationRisk));
            }

            // 5. Aggregate
            decimal avgCouplingIndex = entries.Count > 0
                ? Math.Round(entries.Average(e => e.CouplingIndex), 1)
                : 0m;

            int isolatedCount = entries.Count(e => e.CouplingTier == CouplingTier.Isolated);
            decimal isolatedPct = entries.Count > 0
                ? Math.Round((decimal)isolatedCount / entries.Count * 100m, 1)
                : 0m;

            var distribution = new TierDistribution(
                IsolatedCount: isolatedCount,
                LooselyCoupledCount: entries.Count(e => e.CouplingTier == CouplingTier.LooselyCoupled),
                ModeratelyCoupledCount: entries.Count(e => e.CouplingTier == CouplingTier.ModeratelyCoupled),
                HighlyCoupledCount: entries.Count(e => e.CouplingTier == CouplingTier.HighlyCoupled),
                HubServiceCount: entries.Count(e => e.CouplingTier == CouplingTier.HubService));

            var topHubs = entries
                .Where(e => e.CouplingTier == CouplingTier.HubService)
                .OrderByDescending(e => e.FanIn)
                .ThenByDescending(e => e.CouplingIndex)
                .Take(query.MaxTopServices)
                .ToList();

            var topHighlyCoupled = entries
                .Where(e => e.FanOut > 0)
                .OrderByDescending(e => e.FanOut)
                .ThenByDescending(e => e.CouplingIndex)
                .Take(query.MaxTopServices)
                .ToList();

            var allSorted = entries
                .OrderByDescending(e => e.CouplingIndex)
                .ThenBy(e => e.ServiceName)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TotalServicesAnalyzed: entries.Count,
                AvgCouplingIndex: avgCouplingIndex,
                IsolatedServicePct: isolatedPct,
                Distribution: distribution,
                TopHubServices: topHubs,
                TopHighlyCoupledServices: topHighlyCoupled,
                AllServices: allSorted));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static CouplingTier ClassifyTier(decimal couplingIndex, int fanIn, int fanOut)
        {
            if (couplingIndex >= HubThreshold)
                return CouplingTier.HubService;
            if (couplingIndex >= HighlyCoupledThreshold)
                return CouplingTier.HighlyCoupled;
            if (couplingIndex >= ModeratelyCoupledThreshold)
                return CouplingTier.ModeratelyCoupled;
            if (couplingIndex >= LooselyCoupledThreshold || fanIn > 0 || fanOut > 0)
                return CouplingTier.LooselyCoupled;
            return CouplingTier.Isolated;
        }
    }
}
