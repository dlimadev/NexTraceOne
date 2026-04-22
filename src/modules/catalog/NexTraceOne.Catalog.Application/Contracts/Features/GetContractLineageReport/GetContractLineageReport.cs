using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractLineageReport;

/// <summary>
/// Feature: GetContractLineageReport — linhagem de versões de contratos do catálogo.
///
/// Para cada contrato analisado, produz:
/// - <c>LineageNode</c> por versão: autor, aprovador, datas, breaking changes, consumidores e RetentionDays
/// - <c>StabilityScore</c>: 1 - (breakingChanges / max(1, totalTransitions))
/// - <c>LineageStabilityBand</c>: Stable ≥0.9, Moderate ≥0.7, Volatile ≥0.5, HighlyVolatile &lt;0.5
/// - Identificação da versão com maior e menor tempo de retenção
/// - <c>GlobalStabilityScore</c>: média dos scores de todos os contratos analisados
/// - <c>TotalBreakingChanges</c>: agregação global de breaking changes no período
///
/// Orienta Architect e Auditor na análise de maturidade do processo de versionamento
/// e no cumprimento de requisitos de governança de API ao longo do tempo.
///
/// Wave AB.2 — GetContractLineageReport (Catalog Contracts).
/// </summary>
public static class GetContractLineageReport
{
    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Banda de estabilidade de linhagem de um contrato.</summary>
    public enum LineageStabilityBand
    {
        /// <summary>Score ≥ 0.9 — linhagem estável, poucos breaking changes.</summary>
        Stable,
        /// <summary>Score ≥ 0.7 — linhagem moderadamente estável.</summary>
        Moderate,
        /// <summary>Score ≥ 0.5 — linhagem volátil, atenção recomendada.</summary>
        Volatile,
        /// <summary>Score &lt; 0.5 — linhagem altamente instável, requer revisão.</summary>
        HighlyVolatile
    }

    /// <summary>Nó de linhagem representando uma versão de contrato.</summary>
    public sealed record LineageNode(
        string Version,
        string LifecycleState,
        string? Author,
        string? Approver,
        DateTimeOffset PublishedAt,
        DateTimeOffset? DeprecatedAt,
        int BreakingChangesFromPrev,
        int ConsumersAtDeprecation,
        /// <summary>Dias entre PublishedAt e DeprecatedAt; ou dias desde publicação se versão activa.</summary>
        int RetentionDays);

    /// <summary>Resumo de linhagem de um contrato com todas as suas versões analisadas.</summary>
    public sealed record ContractLineageSummary(
        string ContractId,
        string ContractName,
        string? Protocol,
        int TotalVersions,
        int TotalBreakingChanges,
        double StabilityScore,
        LineageStabilityBand StabilityBand,
        string? LongestRetentionVersion,
        int LongestRetentionDays,
        string? ShortestRetentionVersion,
        int ShortestRetentionDays,
        IReadOnlyList<LineageNode> Versions);

    /// <summary>Resultado do relatório de linhagem de contratos.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<ContractLineageSummary> Contracts,
        int TotalContractsAnalyzed,
        double GlobalStabilityScore,
        int TotalBreakingChanges);

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>ContractId</c>: quando definido, analisa apenas esse contrato; null = todos.</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (30–730, default 365).</para>
    /// <para><c>MaxVersionsPerContract</c>: número máximo de versões por contrato (1–100, default 50).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        string? ContractId = null,
        int LookbackDays = 365,
        int MaxVersionsPerContract = 50) : IQuery<Report>;

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(30, 730);
            RuleFor(q => q.MaxVersionsPerContract).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IContractVersionHistoryReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(IContractVersionHistoryReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;

            // Determina quais contratos processar
            IReadOnlyList<string> contractIds;
            if (!string.IsNullOrWhiteSpace(query.ContractId))
            {
                contractIds = [query.ContractId];
            }
            else
            {
                contractIds = await _reader.ListContractIdsAsync(query.TenantId, cancellationToken);
            }

            if (contractIds.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TenantId: query.TenantId,
                    Contracts: [],
                    TotalContractsAnalyzed: 0,
                    GlobalStabilityScore: 0.0,
                    TotalBreakingChanges: 0));
            }

            var summaries = new List<ContractLineageSummary>(contractIds.Count);

            foreach (var contractId in contractIds)
            {
                var versions = await _reader.ListByContractAsync(
                    query.TenantId, contractId, query.LookbackDays, cancellationToken);

                if (versions.Count == 0)
                    continue;

                // Aplica limite de versões
                var limitedVersions = versions
                    .OrderByDescending(v => v.PublishedAt)
                    .Take(query.MaxVersionsPerContract)
                    .OrderBy(v => v.PublishedAt)
                    .ToList();

                var summary = BuildContractSummary(limitedVersions, now);
                summaries.Add(summary);
            }

            double globalStability = summaries.Count > 0
                ? summaries.Average(s => s.StabilityScore)
                : 0.0;

            int totalBreaking = summaries.Sum(s => s.TotalBreakingChanges);

            return Result<Report>.Success(new Report(
                TenantId: query.TenantId,
                Contracts: summaries,
                TotalContractsAnalyzed: summaries.Count,
                GlobalStabilityScore: Math.Round(globalStability, 4),
                TotalBreakingChanges: totalBreaking));
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static ContractLineageSummary BuildContractSummary(
            IReadOnlyList<ContractVersionEntry> versions,
            DateTimeOffset now)
        {
            var lineageNodes = versions
                .Select(v => new LineageNode(
                    Version: v.Version,
                    LifecycleState: v.LifecycleState,
                    Author: v.AuthorName,
                    Approver: v.ApproverName,
                    PublishedAt: v.PublishedAt,
                    DeprecatedAt: v.DeprecatedAt,
                    BreakingChangesFromPrev: v.BreakingChangesFromPreviousVersion,
                    ConsumersAtDeprecation: v.ActiveConsumersAtDeprecation,
                    RetentionDays: ComputeRetentionDays(v, now)))
                .ToList();

            var first = versions[0];
            int totalBreaking = versions.Sum(v => v.BreakingChangesFromPreviousVersion);
            int totalTransitions = Math.Max(1, versions.Count - 1);
            double stabilityScore = Math.Round(
                1.0 - (double)totalBreaking / totalTransitions, 4);
            stabilityScore = Math.Max(0.0, Math.Min(1.0, stabilityScore));

            var longestNode = lineageNodes.OrderByDescending(n => n.RetentionDays).First();
            var shortestNode = lineageNodes.OrderBy(n => n.RetentionDays).First();

            return new ContractLineageSummary(
                ContractId: first.ContractId,
                ContractName: first.ContractName,
                Protocol: first.Protocol,
                TotalVersions: versions.Count,
                TotalBreakingChanges: totalBreaking,
                StabilityScore: stabilityScore,
                StabilityBand: ClassifyBand(stabilityScore),
                LongestRetentionVersion: longestNode.Version,
                LongestRetentionDays: longestNode.RetentionDays,
                ShortestRetentionVersion: shortestNode.Version,
                ShortestRetentionDays: shortestNode.RetentionDays,
                Versions: lineageNodes);
        }

        private static int ComputeRetentionDays(ContractVersionEntry version, DateTimeOffset now)
        {
            var endDate = version.DeprecatedAt ?? now;
            return Math.Max(0, (int)(endDate - version.PublishedAt).TotalDays);
        }

        private static LineageStabilityBand ClassifyBand(double score) => score switch
        {
            >= 0.9 => LineageStabilityBand.Stable,
            >= 0.7 => LineageStabilityBand.Moderate,
            >= 0.5 => LineageStabilityBand.Volatile,
            _ => LineageStabilityBand.HighlyVolatile
        };
    }
}
