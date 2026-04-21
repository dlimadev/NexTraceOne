using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractConsumerImpactReport;

/// <summary>
/// Feature: GetContractConsumerImpactReport — impacto nos consumidores de contratos em risco de remoção.
///
/// Identifica contratos com estado de ciclo de vida <c>Deprecated</c> ou <c>Sunset</c>
/// e, para cada um, determina quais serviços consumidores ainda possuem expectativas activas
/// registadas. Produz:
/// - total de contratos em risco (Deprecated + Sunset)
/// - total de consumidores activos afetados (únicos)
/// - total de serviços consumidores distintos em risco
/// - distribuição por estado de lifecycle
/// - top contratos por número de consumidores registados
/// - top domínios consumidores com maior exposição ao risco
///
/// Permite que Architect e Tech Lead identifiquem proativamente contratos que não devem ser
/// removidos sem notificação prévia das equipas consumidoras, reforçando o NexTraceOne
/// como Source of Truth dos contratos e das dependências entre serviços.
///
/// Wave Q.2 — Contract Consumer Impact Report (Catalog Contracts).
/// </summary>
public static class GetContractConsumerImpactReport
{
    /// <summary>
    /// <para><c>MaxTopContracts</c>: número máximo de contratos no ranking por consumidores (1–100, default 10).</para>
    /// <para><c>MaxTopDomains</c>: número máximo de domínios consumidores no ranking (1–50, default 20).</para>
    /// <para><c>PageSize</c>: tamanho de página para pesquisa de contratos paginados (10–1000, default 500).</para>
    /// <para><c>IncludeRetired</c>: se true, inclui contratos no estado Retired além de Deprecated/Sunset.</para>
    /// </summary>
    public sealed record Query(
        int MaxTopContracts = 10,
        int MaxTopDomains = 20,
        int PageSize = 500,
        bool IncludeRetired = false) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Distribuição de contratos em risco por estado de lifecycle.</summary>
    public sealed record LifecycleStateDistribution(
        int DeprecatedCount,
        int SunsetCount,
        int RetiredCount);

    /// <summary>Relatório de impacto de um contrato em risco nos seus consumidores.</summary>
    public sealed record AtRiskContractEntry(
        Guid ApiAssetId,
        string SemVer,
        ContractLifecycleState LifecycleState,
        ContractProtocol Protocol,
        int ActiveConsumerCount,
        IReadOnlyList<string> ConsumerServiceNames,
        IReadOnlyList<string> ConsumerDomains);

    /// <summary>Domínio consumidor com exposição a contratos em risco.</summary>
    public sealed record DomainExposureEntry(
        string Domain,
        int AtRiskContractCount,
        int ConsumerExpectationCount);

    /// <summary>Resultado do relatório de impacto nos consumidores.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int TotalAtRiskContracts,
        int TotalAffectedExpectations,
        int DistinctConsumerServices,
        int DistinctConsumerDomains,
        LifecycleStateDistribution StateDistribution,
        IReadOnlyList<AtRiskContractEntry> TopContractsByConsumerCount,
        IReadOnlyList<DomainExposureEntry> TopDomainsByExposure);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.MaxTopContracts).InclusiveBetween(1, 100);
            RuleFor(q => q.MaxTopDomains).InclusiveBetween(1, 50);
            RuleFor(q => q.PageSize).InclusiveBetween(10, 1000);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IContractVersionRepository _versionRepo;
        private readonly IConsumerExpectationRepository _consumerRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IContractVersionRepository versionRepo,
            IConsumerExpectationRepository consumerRepo,
            IDateTimeProvider clock)
        {
            _versionRepo = Guard.Against.Null(versionRepo);
            _consumerRepo = Guard.Against.Null(consumerRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = _clock.UtcNow;

            // Collect at-risk lifecycle states
            var atRiskStates = new List<ContractLifecycleState>
            {
                ContractLifecycleState.Deprecated,
                ContractLifecycleState.Sunset
            };
            if (query.IncludeRetired)
                atRiskStates.Add(ContractLifecycleState.Retired);

            // Fetch contracts per at-risk lifecycle state (paginated)
            var allAtRisk = new List<AtRiskContractEntry>();
            int deprecatedCount = 0, sunsetCount = 0, retiredCount = 0;

            foreach (var state in atRiskStates)
            {
                var (items, _) = await _versionRepo.SearchAsync(
                    protocol: null,
                    lifecycleState: state,
                    apiAssetId: null,
                    searchTerm: null,
                    page: 1,
                    pageSize: query.PageSize,
                    cancellationToken: cancellationToken);

                foreach (var version in items)
                {
                    var consumers = await _consumerRepo.ListByApiAssetAsync(version.ApiAssetId, cancellationToken);
                    var active = consumers.Where(c => c.IsActive).ToList();

                    allAtRisk.Add(new AtRiskContractEntry(
                        ApiAssetId: version.ApiAssetId,
                        SemVer: version.SemVer,
                        LifecycleState: version.LifecycleState,
                        Protocol: version.Protocol,
                        ActiveConsumerCount: active.Count,
                        ConsumerServiceNames: active.Select(c => c.ConsumerServiceName).Distinct().ToList(),
                        ConsumerDomains: active.Select(c => c.ConsumerDomain).Distinct().ToList()));

                    switch (state)
                    {
                        case ContractLifecycleState.Deprecated: deprecatedCount++; break;
                        case ContractLifecycleState.Sunset: sunsetCount++; break;
                        case ContractLifecycleState.Retired: retiredCount++; break;
                    }
                }
            }

            int totalExpectations = allAtRisk.Sum(c => c.ActiveConsumerCount);
            var distinctServices = allAtRisk.SelectMany(c => c.ConsumerServiceNames).Distinct().Count();
            var distinctDomains = allAtRisk.SelectMany(c => c.ConsumerDomains).Distinct().Count();

            var topContracts = allAtRisk
                .OrderByDescending(c => c.ActiveConsumerCount)
                .Take(query.MaxTopContracts)
                .ToList();

            // Domain exposure: count per domain how many at-risk contracts it depends on
            var domainMap = new Dictionary<string, (HashSet<Guid> Contracts, int Expectations)>();
            foreach (var entry in allAtRisk)
            {
                foreach (var domain in entry.ConsumerDomains)
                {
                    if (!domainMap.TryGetValue(domain, out var bucket))
                    {
                        bucket = (new HashSet<Guid>(), 0);
                        domainMap[domain] = bucket;
                    }
                    bucket.Contracts.Add(entry.ApiAssetId);
                    domainMap[domain] = (bucket.Contracts, bucket.Expectations + entry.ActiveConsumerCount);
                }
            }

            var topDomains = domainMap
                .Select(kvp => new DomainExposureEntry(
                    Domain: kvp.Key,
                    AtRiskContractCount: kvp.Value.Contracts.Count,
                    ConsumerExpectationCount: kvp.Value.Expectations))
                .OrderByDescending(d => d.AtRiskContractCount)
                .ThenByDescending(d => d.ConsumerExpectationCount)
                .Take(query.MaxTopDomains)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TotalAtRiskContracts: allAtRisk.Count,
                TotalAffectedExpectations: totalExpectations,
                DistinctConsumerServices: distinctServices,
                DistinctConsumerDomains: distinctDomains,
                StateDistribution: new LifecycleStateDistribution(deprecatedCount, sunsetCount, retiredCount),
                TopContractsByConsumerCount: topContracts,
                TopDomainsByExposure: topDomains));
        }
    }
}
