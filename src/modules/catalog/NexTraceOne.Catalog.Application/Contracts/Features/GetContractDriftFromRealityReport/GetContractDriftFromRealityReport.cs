using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractDriftFromRealityReport;

/// <summary>
/// Feature: GetContractDriftFromRealityReport — divergência entre contratos registados e comportamento real em runtime.
///
/// Detecta:
/// - <c>UndocumentedCalls</c> — endpoints chamados em runtime sem documentação no contrato (ghost endpoints)
/// - <c>UnusedDocumentedOps</c> — operações documentadas sem chamadas no período
/// - <c>ParameterMismatches</c> — parâmetros observados em runtime não documentados
///
/// Classifica por <c>RealityDriftTier</c>:
/// - <c>Aligned</c>          — 0 UndocumentedCalls + ≤10% UnusedOps
/// - <c>MinorDrift</c>        — ≤10% UndocumentedCalls rate
/// - <c>SignificantDrift</c>  — ≤30% UndocumentedCalls rate
/// - <c>Misaligned</c>        — >30% UndocumentedCalls rate
///
/// Wave AM.2 — Auto-Cataloging &amp; Service Discovery Intelligence (Catalog Contracts).
/// </summary>
public static class GetContractDriftFromRealityReport
{
    internal const int DefaultLookbackDays = 30;
    internal const int DefaultUnusedOpsStagnationDays = 30;

    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal MisalignedThreshold = 30m;
    private const decimal SignificantDriftThreshold = 10m;
    private const decimal MinorDriftUnusedOpsThreshold = 10m;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        int UnusedOpsStagnationDays = DefaultUnusedOpsStagnationDays) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 90);
            RuleFor(x => x.UnusedOpsStagnationDays).InclusiveBetween(1, 365);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum RealityDriftTier { Aligned, MinorDrift, SignificantDrift, Misaligned }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record AutoDocumentationHint(
        string ContractId,
        string UndocumentedOperation,
        string SuggestedOpenApiSnippet);

    public sealed record ContractDriftRow(
        string ContractId,
        string ContractName,
        string ServiceName,
        int DocumentedOperationCount,
        int ObservedOperationCount,
        IReadOnlyList<string> UndocumentedCalls,
        int UnusedDocumentedOpCount,
        IReadOnlyList<string> ParameterMismatches,
        decimal UndocumentedCallRate,
        decimal UnusedOpsRate,
        RealityDriftTier DriftTier);

    public sealed record Report(
        string TenantId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        int TotalContractsAnalyzed,
        int AlignedCount,
        int MinorDriftCount,
        int SignificantDriftCount,
        int MisalignedCount,
        decimal TenantContractRealityScore,
        IReadOnlyList<ContractDriftRow> ByContract,
        IReadOnlyList<ContractDriftRow> TopDriftingContracts,
        IReadOnlyList<AutoDocumentationHint> AutoDocumentationHints,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IContractDriftReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.LookbackDays);

            var observations = await reader.ListByTenantAsync(
                request.TenantId, request.LookbackDays, request.UnusedOpsStagnationDays, cancellationToken);

            var rows = observations.Select(obs =>
            {
                var undocRate = obs.DocumentedOperations.Count == 0
                    ? (obs.UndocumentedCalls.Count > 0 ? 100m : 0m)
                    : Math.Round((decimal)obs.UndocumentedCalls.Count / (obs.DocumentedOperations.Count + obs.UndocumentedCalls.Count) * 100m, 2);

                var unusedRate = obs.DocumentedOperations.Count == 0 ? 0m
                    : Math.Round((decimal)obs.UnusedDocumentedOps.Count / obs.DocumentedOperations.Count * 100m, 2);

                var tier = undocRate > MisalignedThreshold ? RealityDriftTier.Misaligned
                    : undocRate > SignificantDriftThreshold ? RealityDriftTier.SignificantDrift
                    : undocRate > 0 || unusedRate > MinorDriftUnusedOpsThreshold ? RealityDriftTier.MinorDrift
                    : RealityDriftTier.Aligned;

                return new ContractDriftRow(
                    obs.ContractId, obs.ContractName, obs.ServiceName,
                    obs.DocumentedOperations.Count, obs.ObservedOperations.Count,
                    obs.UndocumentedCalls, obs.UnusedDocumentedOps.Count,
                    obs.ParameterMismatches,
                    undocRate, unusedRate, tier);
            }).ToList();

            var aligned = rows.Count(r => r.DriftTier == RealityDriftTier.Aligned);
            var minor = rows.Count(r => r.DriftTier == RealityDriftTier.MinorDrift);
            var significant = rows.Count(r => r.DriftTier == RealityDriftTier.SignificantDrift);
            var misaligned = rows.Count(r => r.DriftTier == RealityDriftTier.Misaligned);

            var realityScore = rows.Count == 0 ? 100m
                : Math.Round((decimal)(aligned + minor) / rows.Count * 100m, 2);

            var topDrifting = rows
                .OrderByDescending(r => r.UndocumentedCalls.Count + r.UnusedDocumentedOpCount)
                .Take(10)
                .ToList();

            var hints = observations
                .SelectMany(obs => obs.UndocumentedCalls.Select(call =>
                    new AutoDocumentationHint(
                        obs.ContractId, call,
                        $"# Add to {obs.ContractName}:\npaths:\n  {call}:\n    get:\n      summary: Auto-detected endpoint\n      responses:\n        '200': {{description: OK}}")))
                .Take(20)
                .ToList();

            return Result<Report>.Success(new Report(
                request.TenantId, since, now,
                rows.Count, aligned, minor, significant, misaligned,
                realityScore, rows, topDrifting, hints, now));
        }
    }
}
