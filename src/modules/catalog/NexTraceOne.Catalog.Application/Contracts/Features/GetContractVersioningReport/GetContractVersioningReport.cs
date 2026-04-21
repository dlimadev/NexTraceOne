using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractVersioningReport;

/// <summary>
/// Feature: GetContractVersioningReport — visão consolidada de versionamento de contratos do tenant.
///
/// Agrega todos os contratos registados e produz:
/// - distribuição por estado de ciclo de vida (Draft/InReview/Approved/Locked/Deprecated/Sunset/Retired)
/// - distribuição por protocolo (OpenAPI, AsyncAPI, GraphQL, Protobuf, WSDL, …)
/// - rácios de contratos deprecados e em sunset
/// - lista dos contratos mais deprecados/sunset (candidatos a remoção ou revisão)
/// - total de contratos distintos e versões totais
///
/// Serve como fonte de verdade do estado de saúde do ciclo de vida dos contratos.
/// Orientado para Architect, Tech Lead e Platform Admin personas.
///
/// Wave O.1 — Contract Versioning Report (Catalog Contracts).
/// </summary>
public static class GetContractVersioningReport
{
    /// <summary>
    /// <para><c>TopDeprecatedCount</c>: número máximo de contratos deprecados/sunset a listar (1–50, default 10).</para>
    /// <para><c>PageSize</c>: tamanho da página de pesquisa para agregação interna (10–500, default 200).</para>
    /// </summary>
    public sealed record Query(
        int TopDeprecatedCount = 10,
        int PageSize = 200) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Contagem de versões por estado de ciclo de vida.</summary>
    public sealed record LifecycleDistributionEntry(string State, int Count, decimal Percent);

    /// <summary>Contagem de versões por protocolo de contrato.</summary>
    public sealed record ProtocolDistributionEntry(string Protocol, int Count, decimal Percent);

    /// <summary>Entrada de contrato deprecado/sunset na lista de top candidatos.</summary>
    public sealed record DeprecatedContractEntry(
        Guid ApiAssetId,
        string SemVer,
        string Protocol,
        string LifecycleState);

    /// <summary>Relatório de versionamento de contratos do tenant.</summary>
    public sealed record Report(
        int TotalVersions,
        int DistinctContracts,
        int DeprecatedCount,
        int SunsetCount,
        int RetiredCount,
        decimal DeprecatedRatio,
        decimal ActiveRatio,
        IReadOnlyList<LifecycleDistributionEntry> ByLifecycleState,
        IReadOnlyList<ProtocolDistributionEntry> ByProtocol,
        IReadOnlyList<DeprecatedContractEntry> TopDeprecatedContracts);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TopDeprecatedCount).InclusiveBetween(1, 50);
            RuleFor(x => x.PageSize).InclusiveBetween(10, 500);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IContractVersionRepository repository) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Fetch summary from repository (aggregated data)
            var summary = await repository.GetSummaryAsync(cancellationToken);

            // Fetch latest page of all versions for lifecycle/protocol breakdown
            var (items, _) = await repository.SearchAsync(
                protocol: null,
                lifecycleState: null,
                apiAssetId: null,
                searchTerm: null,
                page: 1,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            var totalVersions = items.Count;
            var distinctContracts = summary.DistinctContracts;

            // Lifecycle distribution
            var lifecycleGroups = items
                .GroupBy(v => v.LifecycleState)
                .OrderByDescending(g => g.Count())
                .Select(g => new LifecycleDistributionEntry(
                    g.Key.ToString(),
                    g.Count(),
                    totalVersions == 0 ? 0m : Math.Round(g.Count() * 100m / totalVersions, 1)))
                .ToList();

            // Protocol distribution
            var protocolGroups = items
                .GroupBy(v => v.Protocol)
                .OrderByDescending(g => g.Count())
                .Select(g => new ProtocolDistributionEntry(
                    g.Key.ToString(),
                    g.Count(),
                    totalVersions == 0 ? 0m : Math.Round(g.Count() * 100m / totalVersions, 1)))
                .ToList();

            // Deprecated/Sunset/Retired counts
            var deprecatedCount = summary.DeprecatedCount;
            var sunsetCount = items.Count(v => v.LifecycleState == ContractLifecycleState.Sunset);
            var retiredCount = items.Count(v => v.LifecycleState == ContractLifecycleState.Retired);

            var obsoleteCount = deprecatedCount + sunsetCount + retiredCount;
            var deprecatedRatio = totalVersions == 0 ? 0m : Math.Round(obsoleteCount * 100m / totalVersions, 1);

            var activeCount = items.Count(v =>
                v.LifecycleState == ContractLifecycleState.Approved ||
                v.LifecycleState == ContractLifecycleState.Locked);
            var activeRatio = totalVersions == 0 ? 0m : Math.Round(activeCount * 100m / totalVersions, 1);

            // Top deprecated/sunset contracts
            var topDeprecated = items
                .Where(v => v.LifecycleState == ContractLifecycleState.Deprecated
                         || v.LifecycleState == ContractLifecycleState.Sunset
                         || v.LifecycleState == ContractLifecycleState.Retired)
                .OrderBy(v => v.LifecycleState)
                .Take(request.TopDeprecatedCount)
                .Select(v => new DeprecatedContractEntry(
                    v.ApiAssetId,
                    v.SemVer,
                    v.Protocol.ToString(),
                    v.LifecycleState.ToString()))
                .ToList();

            var report = new Report(
                TotalVersions: totalVersions,
                DistinctContracts: distinctContracts,
                DeprecatedCount: deprecatedCount,
                SunsetCount: sunsetCount,
                RetiredCount: retiredCount,
                DeprecatedRatio: deprecatedRatio,
                ActiveRatio: activeRatio,
                ByLifecycleState: lifecycleGroups,
                ByProtocol: protocolGroups,
                TopDeprecatedContracts: topDeprecated);

            return Result<Report>.Success(report);
        }
    }
}
