using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.GetEvidencePackCoverageReport;

/// <summary>
/// Feature: GetEvidencePackCoverageReport — relatório de cobertura de Evidence Packs por releases.
///
/// Cruza releases num período com evidence packs existentes e produz:
/// - percentagem de releases com evidence pack associado
/// - percentagem de packs assinados (integridade HMAC)
/// - breakdown por ambiente: releases com/sem evidence pack
/// - lista de releases sem cobertura (sem evidence pack associado)
/// - percentagem de packs completos (CompletenessPercentage ≥ threshold)
///
/// Serve Auditor, Platform Admin e Compliance Officer como prova de governança
/// de mudanças em produção. Alimenta critérios de promoção e compliance reports.
///
/// Wave N.3 — Evidence Pack Coverage Report (ChangeGovernance Workflow).
/// </summary>
public static class GetEvidencePackCoverageReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>Environment</c>: filtro de ambiente (opcional — null = todos).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–365, default 30).</para>
    /// <para><c>MaxUncoveredReleases</c>: máximo de releases sem cobertura a listar (1–100, default 20).</para>
    /// <para><c>CompletenessThreshold</c>: mínimo de completude para considerar pack completo (0–100, default 80).</para>
    /// </summary>
    public sealed record Query(
        Guid TenantId,
        string? Environment = null,
        int LookbackDays = 30,
        int MaxUncoveredReleases = 20,
        decimal CompletenessThreshold = 80m) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Breakdown de cobertura por ambiente.</summary>
    public sealed record EnvironmentCoverageEntry(
        string Environment,
        int TotalReleases,
        int ReleasesWithPack,
        int ReleasesWithoutPack,
        decimal CoveragePercent);

    /// <summary>Release sem evidence pack associado.</summary>
    public sealed record UncoveredReleaseEntry(
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        string Version,
        DateTimeOffset CreatedAt);

    /// <summary>Resultado do relatório de cobertura de evidence packs.</summary>
    public sealed record Report(
        int TotalReleases,
        int ReleasesWithPack,
        int ReleasesWithoutPack,
        decimal CoveragePercent,
        decimal SignedPackPercent,
        decimal CompletePackPercent,
        IReadOnlyList<EnvironmentCoverageEntry> ByEnvironment,
        IReadOnlyList<UncoveredReleaseEntry> UncoveredReleases,
        Guid TenantId,
        int LookbackDays,
        DateTimeOffset From,
        DateTimeOffset To,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.MaxUncoveredReleases).InclusiveBetween(1, 100);
            RuleFor(q => q.CompletenessThreshold).InclusiveBetween(0m, 100m);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IEvidencePackRepository evidencePackRepository,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);

            var to = clock.UtcNow;
            var from = to.AddDays(-query.LookbackDays);

            // Load releases in range
            var releases = await releaseRepository.ListInRangeAsync(
                from, to, query.Environment, query.TenantId, cancellationToken);

            if (releases.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TotalReleases: 0,
                    ReleasesWithPack: 0,
                    ReleasesWithoutPack: 0,
                    CoveragePercent: 0m,
                    SignedPackPercent: 0m,
                    CompletePackPercent: 0m,
                    ByEnvironment: [],
                    UncoveredReleases: [],
                    TenantId: query.TenantId,
                    LookbackDays: query.LookbackDays,
                    From: from,
                    To: to,
                    GeneratedAt: clock.UtcNow));
            }

            // Batch-fetch evidence packs for all releases
            var releaseIds = releases.Select(r => r.Id.Value).ToList();
            var packs = await evidencePackRepository.ListByReleaseIdsAsync(releaseIds, cancellationToken);

            var packByReleaseId = packs.ToDictionary(p => p.ReleaseId);

            var total = releases.Count;
            var releasesWithPack = releases.Count(r => packByReleaseId.ContainsKey(r.Id.Value));
            var releasesWithoutPack = total - releasesWithPack;
            var coveragePercent = Math.Round((decimal)releasesWithPack / total * 100m, 1);

            // Signed and complete pack rates (relative to packs, not releases)
            var totalPacks = packs.Count;
            var signedPackPercent = totalPacks == 0
                ? 0m
                : Math.Round((decimal)packs.Count(p => p.IsIntegritySigned) / totalPacks * 100m, 1);

            var completePackPercent = totalPacks == 0
                ? 0m
                : Math.Round(
                    (decimal)packs.Count(p => p.CompletenessPercentage >= query.CompletenessThreshold) / totalPacks * 100m,
                    1);

            // Breakdown by environment
            var byEnv = releases
                .GroupBy(r => r.Environment)
                .Select(g =>
                {
                    var envTotal = g.Count();
                    var withPack = g.Count(r => packByReleaseId.ContainsKey(r.Id.Value));
                    return new EnvironmentCoverageEntry(
                        Environment: g.Key,
                        TotalReleases: envTotal,
                        ReleasesWithPack: withPack,
                        ReleasesWithoutPack: envTotal - withPack,
                        CoveragePercent: Math.Round((decimal)withPack / envTotal * 100m, 1));
                })
                .OrderBy(e => e.Environment)
                .ToList();

            // Top uncovered releases (oldest first, sorted by creation date)
            var uncovered = releases
                .Where(r => !packByReleaseId.ContainsKey(r.Id.Value))
                .OrderBy(r => r.CreatedAt)
                .Take(query.MaxUncoveredReleases)
                .Select(r => new UncoveredReleaseEntry(
                    ReleaseId: r.Id.Value,
                    ServiceName: r.ServiceName,
                    Environment: r.Environment,
                    Version: r.Version,
                    CreatedAt: r.CreatedAt))
                .ToList();

            return Result<Report>.Success(new Report(
                TotalReleases: total,
                ReleasesWithPack: releasesWithPack,
                ReleasesWithoutPack: releasesWithoutPack,
                CoveragePercent: coveragePercent,
                SignedPackPercent: signedPackPercent,
                CompletePackPercent: completePackPercent,
                ByEnvironment: byEnv,
                UncoveredReleases: uncovered,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                From: from,
                To: to,
                GeneratedAt: clock.UtcNow));
        }
    }
}
