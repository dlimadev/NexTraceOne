using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Features.GetPromotionGateComplianceReport;

/// <summary>
/// Feature: GetPromotionGateComplianceReport — relatório de conformidade de promotion gates.
///
/// Agrega avaliações de gates de promoção e produz:
/// - total de avaliações (passed/failed/overridden)
/// - taxa de aprovação global e por tipo de gate
/// - contagem de overrides (bypass justificado)
/// - ranking dos gates que mais falham
///
/// Serve como fonte de verdade do rigor do processo de promoção entre ambientes.
/// Orientado para Architect, Tech Lead e Platform Admin personas.
///
/// Wave O.3 — Promotion Gate Compliance Report (ChangeGovernance Promotion).
/// </summary>
public static class GetPromotionGateComplianceReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant.</para>
    /// <para><c>LookbackDays</c>: período de análise (1–365, default 30).</para>
    /// <para><c>TopFailingGatesCount</c>: máximo de gates que mais falham a listar (1–50, default 10).</para>
    /// </summary>
    public sealed record Query(
        Guid TenantId,
        int LookbackDays = 30,
        int TopFailingGatesCount = 10) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Conformidade de avaliações por tipo de gate.</summary>
    public sealed record GateTypeComplianceEntry(
        string GateType,
        int TotalEvaluations,
        int PassedCount,
        int FailedCount,
        int OverriddenCount,
        decimal PassRate);

    /// <summary>Gate que mais falha no período.</summary>
    public sealed record TopFailingGateEntry(
        Guid GateId,
        string GateType,
        int FailedCount,
        int TotalEvaluations,
        decimal FailRate);

    /// <summary>Relatório de conformidade de promotion gates.</summary>
    public sealed record Report(
        Guid TenantId,
        DateTimeOffset From,
        DateTimeOffset To,
        int LookbackDays,
        int TotalEvaluations,
        int TotalPassed,
        int TotalFailed,
        int TotalOverridden,
        decimal GlobalPassRate,
        IReadOnlyList<GateTypeComplianceEntry> ByGateType,
        IReadOnlyList<TopFailingGateEntry> TopFailingGates,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(x => x.TopFailingGatesCount).InclusiveBetween(1, 50);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IPromotionGateRepository gateRepository,
        IGateEvaluationRepository evaluationRepository,
        IDeploymentEnvironmentRepository environmentRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var from = now.AddDays(-request.LookbackDays);

            // Fetch all active environments (environments are not scoped per tenant in promotion module)
            var environments = await environmentRepository.ListActiveAsync(cancellationToken);

            // Collect all gates across all environments
            var allGates = new List<Domain.Promotion.Entities.PromotionGate>();
            foreach (var env in environments)
            {
                var gates = await gateRepository.ListByEnvironmentIdAsync(env.Id, cancellationToken);
                allGates.AddRange(gates);
            }

            // For each gate, fetch evaluations and filter by period
            var allEvaluations = new List<Domain.Promotion.Entities.GateEvaluation>();
            foreach (var gate in allGates)
            {
                var evals = await evaluationRepository.ListByGateIdAsync(gate.Id, cancellationToken);
                allEvaluations.AddRange(evals.Where(e => e.EvaluatedAt >= from && e.EvaluatedAt <= now));
            }

            // Build a lookup: gate id → gate
            var gateById = allGates.ToDictionary(g => g.Id.Value);

            // Overridden = evaluation with OverrideJustification set that passed
            var totalEvals = allEvaluations.Count;
            var totalPassed = allEvaluations.Count(e => e.Passed && e.OverrideJustification is null);
            var totalOverridden = allEvaluations.Count(e => e.Passed && e.OverrideJustification is not null);
            var totalFailed = allEvaluations.Count(e => !e.Passed);
            var globalPassRate = totalEvals == 0 ? 0m :
                Math.Round((totalPassed + totalOverridden) * 100m / totalEvals, 1);

            // By gate type
            var byGateType = allEvaluations
                .GroupBy(e => gateById.TryGetValue(e.PromotionGateId.Value, out var g) ? g.GateType : "Unknown")
                .OrderByDescending(gr => gr.Count())
                .Select(gr =>
                {
                    var passed = gr.Count(e => e.Passed && e.OverrideJustification is null);
                    var overridden = gr.Count(e => e.Passed && e.OverrideJustification is not null);
                    var failed = gr.Count(e => !e.Passed);
                    var total = gr.Count();
                    return new GateTypeComplianceEntry(
                        GateType: gr.Key,
                        TotalEvaluations: total,
                        PassedCount: passed,
                        FailedCount: failed,
                        OverriddenCount: overridden,
                        PassRate: total == 0 ? 0m : Math.Round((passed + overridden) * 100m / total, 1));
                })
                .ToList();

            // Top failing gates
            var topFailing = allEvaluations
                .GroupBy(e => e.PromotionGateId.Value)
                .Select(gr =>
                {
                    var failed = gr.Count(e => !e.Passed);
                    var total = gr.Count();
                    var gateType = gateById.TryGetValue(gr.Key, out var g) ? g.GateType : "Unknown";
                    return new TopFailingGateEntry(
                        GateId: gr.Key,
                        GateType: gateType,
                        FailedCount: failed,
                        TotalEvaluations: total,
                        FailRate: total == 0 ? 0m : Math.Round(failed * 100m / total, 1));
                })
                .Where(x => x.FailedCount > 0)
                .OrderByDescending(x => x.FailedCount)
                .Take(request.TopFailingGatesCount)
                .ToList();

            var report = new Report(
                TenantId: request.TenantId,
                From: from,
                To: now,
                LookbackDays: request.LookbackDays,
                TotalEvaluations: totalEvals,
                TotalPassed: totalPassed,
                TotalFailed: totalFailed,
                TotalOverridden: totalOverridden,
                GlobalPassRate: globalPassRate,
                ByGateType: byGateType,
                TopFailingGates: topFailing,
                GeneratedAt: now);

            return Result<Report>.Success(report);
        }
    }
}
