using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetCrossRankedBenchmark;

/// <summary>
/// Feature: GetCrossRankedBenchmark — retorna o ranking percentil de um tenant
/// em relação ao peer set anonimizado de outros tenants participantes.
///
/// Privacidade obrigatória: NUNCA retorna dados individuais de outros tenants.
/// Apenas agrega e calcula percentis — peer set precisa ter tamanho mínimo configurável.
///
/// Métricas DORA cobertas: deployment frequency, lead time, change failure rate, MTTR.
/// Wave D.2 — Cross-tenant Benchmarks anonimizados.
/// </summary>
public static class GetCrossRankedBenchmark
{
    /// <summary>Tamanho mínimo do peer set para exibir ranking (proteção de privacidade).</summary>
    public const int DefaultMinPeerSetSize = 5;

    public sealed record Query(
        string TenantId,
        int Days = 90) : IQuery<Response>;

    public sealed record Response(
        string TenantId,
        int PeriodDays,
        // Médias do próprio tenant
        decimal TenantDeployFreq,
        decimal TenantLeadTime,
        decimal TenantFailureRate,
        decimal TenantMttr,
        decimal TenantMaturity,
        // Peer set
        int PeerSetSize,
        // Percentis (0-100) — null quando peer set insuficiente
        decimal? DeployFreqPercentile,
        decimal? LeadTimePercentile,
        decimal? FailureRatePercentile,
        decimal? MttrPercentile,
        decimal? MaturityPercentile,
        bool InsufficientPeers);

    public sealed class Handler(
        IBenchmarkSnapshotRepository snapshotRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.OutOfRange(request.Days, nameof(request.Days), 1, 730);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.Days);

            var tenantSnapshots = await snapshotRepository.ListByTenantAsync(request.TenantId, since, cancellationToken);
            var peerSnapshots = await snapshotRepository.ListAnonymizedAsync(since, cancellationToken);

            // Peer set exclui snapshots do próprio tenant (já cobertos pela visão do tenant)
            var peers = peerSnapshots
                .Where(s => !string.Equals(s.TenantId, request.TenantId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var tenantDeployFreq = tenantSnapshots.Count > 0
                ? tenantSnapshots.Average(s => (double)s.DeploymentFrequencyPerWeek)
                : 0.0;
            var tenantLeadTime = tenantSnapshots.Count > 0
                ? tenantSnapshots.Average(s => (double)s.LeadTimeForChangesHours)
                : 0.0;
            var tenantFailureRate = tenantSnapshots.Count > 0
                ? tenantSnapshots.Average(s => (double)s.ChangeFailureRatePercent)
                : 0.0;
            var tenantMttr = tenantSnapshots.Count > 0
                ? tenantSnapshots.Average(s => (double)s.MeanTimeToRestoreHours)
                : 0.0;
            var tenantMaturity = tenantSnapshots.Count > 0
                ? tenantSnapshots.Average(s => (double)s.MaturityScore)
                : 0.0;

            var insufficientPeers = peers.Count < DefaultMinPeerSetSize;

            decimal? deployFreqPct = null;
            decimal? leadTimePct = null;
            decimal? failureRatePct = null;
            decimal? mttrPct = null;
            decimal? maturityPct = null;

            if (!insufficientPeers)
            {
                // Para métricas onde mais alto é melhor (deploy freq, maturity): percentil = % de peers que estão abaixo
                // Para métricas onde mais baixo é melhor (lead time, failure rate, mttr): percentil = % de peers que estão acima (ou seja, tenant está melhor)
                deployFreqPct = ComputePercentileHigherIsBetter(tenantDeployFreq, peers.Select(p => (double)p.DeploymentFrequencyPerWeek));
                leadTimePct = ComputePercentileLowerIsBetter(tenantLeadTime, peers.Select(p => (double)p.LeadTimeForChangesHours));
                failureRatePct = ComputePercentileLowerIsBetter(tenantFailureRate, peers.Select(p => (double)p.ChangeFailureRatePercent));
                mttrPct = ComputePercentileLowerIsBetter(tenantMttr, peers.Select(p => (double)p.MeanTimeToRestoreHours));
                maturityPct = ComputePercentileHigherIsBetter(tenantMaturity, peers.Select(p => (double)p.MaturityScore));
            }

            return Result<Response>.Success(new Response(
                TenantId: request.TenantId,
                PeriodDays: request.Days,
                TenantDeployFreq: (decimal)tenantDeployFreq,
                TenantLeadTime: (decimal)tenantLeadTime,
                TenantFailureRate: (decimal)tenantFailureRate,
                TenantMttr: (decimal)tenantMttr,
                TenantMaturity: (decimal)tenantMaturity,
                PeerSetSize: peers.Count,
                DeployFreqPercentile: deployFreqPct,
                LeadTimePercentile: leadTimePct,
                FailureRatePercentile: failureRatePct,
                MttrPercentile: mttrPct,
                MaturityPercentile: maturityPct,
                InsufficientPeers: insufficientPeers));
        }

        private static decimal ComputePercentileHigherIsBetter(double tenantValue, IEnumerable<double> peerValues)
        {
            var peers = peerValues.ToList();
            if (peers.Count == 0) return 50m;
            var countBelow = peers.Count(p => p < tenantValue);
            return Math.Round((decimal)countBelow / peers.Count * 100m, 1);
        }

        private static decimal ComputePercentileLowerIsBetter(double tenantValue, IEnumerable<double> peerValues)
        {
            var peers = peerValues.ToList();
            if (peers.Count == 0) return 50m;
            var countAbove = peers.Count(p => p > tenantValue);
            return Math.Round((decimal)countAbove / peers.Count * 100m, 1);
        }
    }
}
