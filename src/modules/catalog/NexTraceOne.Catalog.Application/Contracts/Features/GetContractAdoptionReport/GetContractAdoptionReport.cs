using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractAdoptionReport;

/// <summary>
/// Feature: GetContractAdoptionReport — progresso de migração de versões de contrato.
///
/// Para cada API asset distinto, identifica a versão estável mais recente
/// (Approved ou Locked) e classifica o estado de adoção:
/// - <c>Complete</c>   — todas as versões conhecidas são estáveis; nenhuma deprecated/sunset existe
/// - <c>InProgress</c> — versão estável mais recente coexiste com versões deprecated/sunset mas a maioria das versões são estáveis
/// - <c>Lagging</c>    — a maioria das versões ainda são deprecated/sunset; migração está atrasada
/// - <c>NoConsumers</c> — asset existe mas não tem versão estável registada (só draft/review)
///
/// Produz:
/// - totais de contratos analisados por tier
/// - distribuição de MigrationTier
/// - versão mais antiga ainda activa por asset
/// - top contratos com migração mais lenta (maior número de versões antigas)
///
/// Orienta Architect e Tech Lead a priorizar a limpeza de contratos obsoletos e
/// acelerar a adopção das versões mais recentes pelos consumidores.
///
/// Wave S.2 — Contract Adoption Report (Catalog Contracts).
/// </summary>
public static class GetContractAdoptionReport
{
    /// <summary>
    /// <para><c>TopLaggingCount</c>: número máximo de contratos com migração mais lenta a listar (1–50, default 10).</para>
    /// <para><c>PageSize</c>: tamanho da página de pesquisa interna (10–1000, default 500).</para>
    /// </summary>
    public sealed record Query(
        int TopLaggingCount = 10,
        int PageSize = 500) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Classificação do estado de adoção da versão mais recente de um contrato.</summary>
    public enum MigrationTier
    {
        /// <summary>Apenas versões estáveis existem; migração completa.</summary>
        Complete,
        /// <summary>Versão estável recente coexiste com poucas versões obsoletas.</summary>
        InProgress,
        /// <summary>Maioria das versões ainda são obsoletas; adopção atrasada.</summary>
        Lagging,
        /// <summary>Sem versão estável registada; asset apenas em rascunho ou revisão.</summary>
        NoConsumers
    }

    /// <summary>Distribuição de contratos por tier de migração.</summary>
    public sealed record MigrationTierDistribution(
        int CompleteCount,
        int InProgressCount,
        int LaggingCount,
        int NoConsumersCount);

    /// <summary>Detalhe de adoção de um API asset.</summary>
    public sealed record ContractAdoptionEntry(
        Guid ApiAssetId,
        string LatestStableVersion,
        int TotalVersions,
        int StableVersionCount,
        int ObsoleteVersionCount,
        decimal ObsoleteRatioPct,
        MigrationTier MigrationTier,
        string OldestVersionSemVer);

    /// <summary>Resultado do relatório de adoção de versões de contrato.</summary>
    public sealed record Report(
        int TotalDistinctAssets,
        int AssetsWithStableVersion,
        int AssetsWithoutStableVersion,
        decimal GlobalObsoleteRatioPct,
        MigrationTierDistribution TierDistribution,
        IReadOnlyList<ContractAdoptionEntry> TopLaggingContracts,
        IReadOnlyList<ContractAdoptionEntry> AllContracts);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TopLaggingCount).InclusiveBetween(1, 50);
            RuleFor(q => q.PageSize).InclusiveBetween(10, 1000);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler(
        IContractVersionRepository repository) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);

            // Fetch all contract versions
            var (allVersions, _) = await repository.SearchAsync(
                protocol: null,
                lifecycleState: null,
                apiAssetId: null,
                searchTerm: null,
                page: 1,
                pageSize: query.PageSize,
                cancellationToken: cancellationToken);

            if (allVersions.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TotalDistinctAssets: 0,
                    AssetsWithStableVersion: 0,
                    AssetsWithoutStableVersion: 0,
                    GlobalObsoleteRatioPct: 0m,
                    TierDistribution: new MigrationTierDistribution(0, 0, 0, 0),
                    TopLaggingContracts: [],
                    AllContracts: []));
            }

            var stableStates = new HashSet<ContractLifecycleState>
            {
                ContractLifecycleState.Approved,
                ContractLifecycleState.Locked
            };

            var obsoleteStates = new HashSet<ContractLifecycleState>
            {
                ContractLifecycleState.Deprecated,
                ContractLifecycleState.Sunset,
                ContractLifecycleState.Retired
            };

            // Group by API asset
            var byAsset = allVersions
                .GroupBy(v => v.ApiAssetId)
                .ToList();

            var entries = new List<ContractAdoptionEntry>();

            foreach (var group in byAsset)
            {
                var versions = group.ToList();
                var stableVersions = versions.Where(v => stableStates.Contains(v.LifecycleState)).ToList();
                var obsoleteVersions = versions.Where(v => obsoleteStates.Contains(v.LifecycleState)).ToList();

                int total = versions.Count;
                int stableCount = stableVersions.Count;
                int obsoleteCount = obsoleteVersions.Count;

                decimal obsoleteRatio = total > 0
                    ? Math.Round(obsoleteCount * 100m / total, 1)
                    : 0m;

                // Latest stable version by SemVer ordering (alphabetical as fallback)
                string latestStable = stableVersions.Count > 0
                    ? stableVersions.OrderByDescending(v => v.SemVer).First().SemVer
                    : string.Empty;

                // Oldest version (by SemVer ascending)
                string oldestSemVer = versions.OrderBy(v => v.SemVer).First().SemVer;

                MigrationTier tier = ClassifyTier(stableCount, obsoleteCount, total);

                entries.Add(new ContractAdoptionEntry(
                    ApiAssetId: group.Key,
                    LatestStableVersion: latestStable,
                    TotalVersions: total,
                    StableVersionCount: stableCount,
                    ObsoleteVersionCount: obsoleteCount,
                    ObsoleteRatioPct: obsoleteRatio,
                    MigrationTier: tier,
                    OldestVersionSemVer: oldestSemVer));
            }

            int completeCount = entries.Count(e => e.MigrationTier == MigrationTier.Complete);
            int inProgressCount = entries.Count(e => e.MigrationTier == MigrationTier.InProgress);
            int laggingCount = entries.Count(e => e.MigrationTier == MigrationTier.Lagging);
            int noConsumersCount = entries.Count(e => e.MigrationTier == MigrationTier.NoConsumers);

            int assetsWithStable = entries.Count(e => e.StableVersionCount > 0);
            int assetsWithoutStable = entries.Count - assetsWithStable;

            int globalTotalVersions = entries.Sum(e => e.TotalVersions);
            int globalObsolete = entries.Sum(e => e.ObsoleteVersionCount);
            decimal globalObsoleteRatio = globalTotalVersions > 0
                ? Math.Round(globalObsolete * 100m / globalTotalVersions, 1)
                : 0m;

            var topLagging = entries
                .OrderByDescending(e => e.ObsoleteVersionCount)
                .ThenByDescending(e => e.ObsoleteRatioPct)
                .Take(query.TopLaggingCount)
                .ToList();

            return Result<Report>.Success(new Report(
                TotalDistinctAssets: entries.Count,
                AssetsWithStableVersion: assetsWithStable,
                AssetsWithoutStableVersion: assetsWithoutStable,
                GlobalObsoleteRatioPct: globalObsoleteRatio,
                TierDistribution: new MigrationTierDistribution(completeCount, inProgressCount, laggingCount, noConsumersCount),
                TopLaggingContracts: topLagging,
                AllContracts: entries.OrderBy(e => e.ApiAssetId).ToList()));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static MigrationTier ClassifyTier(int stableCount, int obsoleteCount, int total)
        {
            if (stableCount == 0)
                return MigrationTier.NoConsumers;

            if (obsoleteCount == 0)
                return MigrationTier.Complete;

            // Majority obsolete = Lagging (obsolete > 50%)
            decimal obsoleteRatio = total > 0 ? (decimal)obsoleteCount / total : 0m;
            return obsoleteRatio > 0.5m
                ? MigrationTier.Lagging
                : MigrationTier.InProgress;
        }
    }
}
