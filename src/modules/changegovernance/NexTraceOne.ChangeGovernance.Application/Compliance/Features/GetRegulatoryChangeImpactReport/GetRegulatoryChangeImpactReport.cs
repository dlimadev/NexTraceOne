using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetRegulatoryChangeImpactReport;

/// <summary>
/// Feature: GetRegulatoryChangeImpactReport — impacto de mudanças regulatórias sobre os serviços do tenant.
///
/// Input: <see cref="Command"/> com <c>StandardId</c>, <c>NewControlId</c>, <c>Scope</c>.
///
/// Resultado:
/// - <c>ImpactedServicesCount</c>         — número de serviços afectados
/// - <c>EstimatedRemediationEffort</c>    — High/Medium/Low + dias estimados
/// - <c>ServiceImpactList</c>             — serviços com <c>MitigationPath</c>
/// - <c>TenantRegulatoryReadinessScore</c> — score de prontidão regulatória do tenant
///
/// Endpoint: POST /compliance/regulatory-change-impact
/// Wave BB.3 — Compliance Automation &amp; Regulatory Reporting (ChangeGovernance/Catalog).
/// </summary>
public static class GetRegulatoryChangeImpactReport
{
    internal const decimal HighReadinessThreshold = 80m;
    internal const decimal MediumReadinessThreshold = 50m;

    // ── Command ────────────────────────────────────────────────────────────
    public sealed record Command(
        string TenantId,
        string StandardId,
        string NewControlId,
        string Scope) : ICommand<Report>;

    // ── Validator ──────────────────────────────────────────────────────────
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(c => c.StandardId).NotEmpty().MaximumLength(100);
            RuleFor(c => c.NewControlId).NotEmpty().MaximumLength(200);
            RuleFor(c => c.Scope).NotEmpty().MaximumLength(500);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Nível de esforço de remediação estimado.</summary>
    public enum RemediationEffortLevel { Low, Medium, High }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Impacto num serviço específico com caminho de mitigação.</summary>
    public sealed record ServiceImpactEntry(
        string ServiceId,
        string ServiceName,
        string ServiceTier,
        string TeamId,
        bool HasExistingControl,
        string? MitigationPath,
        RemediationEffortLevel EstimatedEffort,
        int EstimatedEffortDays);

    /// <summary>Resultado do relatório de impacto regulatório.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        string StandardId,
        string NewControlId,
        string Scope,
        int ImpactedServicesCount,
        int ServicesWithExistingControl,
        RemediationEffortLevel OverallRemediationEffort,
        int TotalEstimatedEffortDays,
        decimal TenantRegulatoryReadinessScore,
        IReadOnlyList<ServiceImpactEntry> ServiceImpactList);

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler(
        IRegulatoryChangeImpactReader reader,
        IDateTimeProvider clock) : ICommandHandler<Command, Report>
    {
        public async Task<Result<Report>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;

            var impacted = await reader.ListImpactedServicesAsync(
                request.TenantId, request.StandardId, request.NewControlId, request.Scope, cancellationToken);

            var readinessScore = await reader.GetTenantRegulatoryReadinessScoreAsync(
                request.TenantId, cancellationToken);

            if (impacted.Count == 0)
                return Result<Report>.Success(EmptyReport(now, request, readinessScore));

            var serviceImpacts = impacted
                .Select(s => new ServiceImpactEntry(
                    ServiceId: s.ServiceId,
                    ServiceName: s.ServiceName,
                    ServiceTier: s.ServiceTier,
                    TeamId: s.TeamId,
                    HasExistingControl: s.HasExistingControl,
                    MitigationPath: s.MitigationPath,
                    EstimatedEffort: ParseEffort(s.EstimatedEffortLevel),
                    EstimatedEffortDays: s.EstimatedEffortDays))
                .OrderByDescending(s => (int)s.EstimatedEffort)
                .ToList();

            int withExisting = impacted.Count(s => s.HasExistingControl);
            int totalDays = serviceImpacts.Sum(s => s.EstimatedEffortDays);
            var overallEffort = ClassifyOverallEffort(serviceImpacts);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: request.TenantId,
                StandardId: request.StandardId,
                NewControlId: request.NewControlId,
                Scope: request.Scope,
                ImpactedServicesCount: impacted.Count,
                ServicesWithExistingControl: withExisting,
                OverallRemediationEffort: overallEffort,
                TotalEstimatedEffortDays: totalDays,
                TenantRegulatoryReadinessScore: Math.Round(readinessScore, 1),
                ServiceImpactList: serviceImpacts));
        }

        private static RemediationEffortLevel ParseEffort(string level) => level switch
        {
            "High" => RemediationEffortLevel.High,
            "Medium" => RemediationEffortLevel.Medium,
            _ => RemediationEffortLevel.Low
        };

        private static RemediationEffortLevel ClassifyOverallEffort(
            IReadOnlyList<ServiceImpactEntry> entries)
        {
            if (entries.Any(e => e.EstimatedEffort == RemediationEffortLevel.High))
                return RemediationEffortLevel.High;
            if (entries.Any(e => e.EstimatedEffort == RemediationEffortLevel.Medium))
                return RemediationEffortLevel.Medium;
            return RemediationEffortLevel.Low;
        }

        private static Report EmptyReport(DateTimeOffset now, Command request, decimal readinessScore)
            => new(
                GeneratedAt: now,
                TenantId: request.TenantId,
                StandardId: request.StandardId,
                NewControlId: request.NewControlId,
                Scope: request.Scope,
                ImpactedServicesCount: 0,
                ServicesWithExistingControl: 0,
                OverallRemediationEffort: RemediationEffortLevel.Low,
                TotalEstimatedEffortDays: 0,
                TenantRegulatoryReadinessScore: Math.Round(readinessScore, 1),
                ServiceImpactList: []);
    }
}
