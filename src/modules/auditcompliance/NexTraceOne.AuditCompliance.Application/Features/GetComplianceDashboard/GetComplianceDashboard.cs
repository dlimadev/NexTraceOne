using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.GetComplianceDashboard;

/// <summary>
/// Feature: GetComplianceDashboard — agrega o estado de compliance por framework, serviço,
/// categoria e período para uma vista executiva/auditor de conformidade contínua.
///
/// Responde às questões:
///   - Qual o estado geral de conformidade do tenant?
///   - Quais frameworks estão em Red/Amber/Green?
///   - Quais categorias têm mais non-compliance?
///   - Qual a tendência de conformidade (melhoria ou deterioração)?
///
/// Valor: dashboard de compliance contínuo que substitui auditorias periódicas manuais.
/// Persona primária: Auditor e Executive.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetComplianceDashboard
{
    private static readonly string[] SupportedFrameworks = ["SOC2", "ISO27001", "LGPD", "GDPR", "PCI-DSS"];

    /// <summary>Query para obter o dashboard de compliance.</summary>
    public sealed record Query(
        Guid TenantId,
        DateTimeOffset? From,
        DateTimeOffset? To) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que agrega o estado de compliance para o dashboard.</summary>
    public sealed class Handler(
        ICompliancePolicyRepository policyRepository,
        IComplianceResultRepository resultRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var policies = await policyRepository.ListAsync(
                isActive: true, category: null, cancellationToken);

            var tenantPolicies = policies
                .Where(p => p.TenantId == request.TenantId)
                .ToList();

            var allResults = await resultRepository.ListAsync(
                policyId: null, campaignId: null, outcome: null, cancellationToken);

            var tenantResults = allResults
                .Where(r => r.TenantId == request.TenantId)
                .ToList();

            if (request.From.HasValue)
                tenantResults = tenantResults.Where(r => r.EvaluatedAt >= request.From.Value).ToList();
            if (request.To.HasValue)
                tenantResults = tenantResults.Where(r => r.EvaluatedAt <= request.To.Value).ToList();

            var latestByPolicy = tenantResults
                .GroupBy(r => r.PolicyId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.EvaluatedAt).First());

            var categoryGroups = tenantPolicies
                .GroupBy(p => p.Category)
                .Select(g =>
                {
                    var categoryResults = g
                        .Select(p => latestByPolicy.TryGetValue(p.Id, out var r) ? r.Outcome : ComplianceOutcome.NotApplicable)
                        .ToList();

                    var compliant = categoryResults.Count(o => o == ComplianceOutcome.Compliant);
                    var nonCompliant = categoryResults.Count(o => o == ComplianceOutcome.NonCompliant);
                    var partial = categoryResults.Count(o => o == ComplianceOutcome.PartiallyCompliant);

                    var score = categoryResults.Count == 0 ? 100m
                        : Math.Round((decimal)(compliant + partial * 0.5m) / categoryResults.Count * 100, 1);

                    return new CategoryStatus(
                        Category: g.Key,
                        TotalPolicies: g.Count(),
                        Compliant: compliant,
                        NonCompliant: nonCompliant,
                        PartiallyCompliant: partial,
                        Score: score,
                        Status: nonCompliant > 0 && g.Any(p => p.Severity == ComplianceSeverity.Critical
                            && latestByPolicy.TryGetValue(p.Id, out var lr) && lr.Outcome == ComplianceOutcome.NonCompliant)
                            ? "Red"
                            : nonCompliant > 0 ? "Amber" : "Green");
                })
                .OrderBy(c => c.Status == "Red" ? 0 : c.Status == "Amber" ? 1 : 2)
                .ToList();

            var overallCompliant = latestByPolicy.Values.Count(r => r.Outcome == ComplianceOutcome.Compliant);
            var overallNonCompliant = latestByPolicy.Values.Count(r => r.Outcome == ComplianceOutcome.NonCompliant);
            var overallPartial = latestByPolicy.Values.Count(r => r.Outcome == ComplianceOutcome.PartiallyCompliant);
            var totalEvaluated = latestByPolicy.Count;

            var overallScore = totalEvaluated == 0 ? 100m
                : Math.Round((decimal)(overallCompliant + overallPartial * 0.5m) / totalEvaluated * 100, 1);

            var overallStatus = overallNonCompliant > 0 && latestByPolicy.Values.Any(r =>
                r.Outcome == ComplianceOutcome.NonCompliant &&
                tenantPolicies.FirstOrDefault(p => p.Id == r.PolicyId)?.Severity == ComplianceSeverity.Critical) != false
                && latestByPolicy.Values.Any(r => r.Outcome == ComplianceOutcome.NonCompliant &&
                    tenantPolicies.FirstOrDefault(p => p.Id == r.PolicyId)?.Severity == ComplianceSeverity.Critical)
                ? "Red"
                : overallNonCompliant > 0 ? "Amber" : "Green";

            var criticalGaps = tenantPolicies
                .Where(p => p.Severity == ComplianceSeverity.Critical
                    && latestByPolicy.TryGetValue(p.Id, out var r)
                    && r.Outcome == ComplianceOutcome.NonCompliant)
                .Select(p => new PolicyGap(p.Id.Value, p.DisplayName, p.Category, p.Severity))
                .ToList();

            return new Response(
                TenantId: request.TenantId,
                GeneratedAt: DateTimeOffset.UtcNow,
                OverallStatus: overallStatus,
                OverallScore: overallScore,
                TotalPolicies: tenantPolicies.Count,
                TotalEvaluated: totalEvaluated,
                TotalUnevaluated: tenantPolicies.Count - totalEvaluated,
                Compliant: overallCompliant,
                NonCompliant: overallNonCompliant,
                PartiallyCompliant: overallPartial,
                CriticalGaps: criticalGaps.AsReadOnly(),
                CategoryBreakdown: categoryGroups.AsReadOnly());
        }
    }

    /// <summary>Estado de conformidade por categoria.</summary>
    public sealed record CategoryStatus(
        string Category,
        int TotalPolicies,
        int Compliant,
        int NonCompliant,
        int PartiallyCompliant,
        decimal Score,
        string Status);

    /// <summary>Política com gap crítico.</summary>
    public sealed record PolicyGap(
        Guid PolicyId,
        string PolicyName,
        string Category,
        ComplianceSeverity Severity);

    /// <summary>Resposta do dashboard de compliance.</summary>
    public sealed record Response(
        Guid TenantId,
        DateTimeOffset GeneratedAt,
        string OverallStatus,
        decimal OverallScore,
        int TotalPolicies,
        int TotalEvaluated,
        int TotalUnevaluated,
        int Compliant,
        int NonCompliant,
        int PartiallyCompliant,
        IReadOnlyList<PolicyGap> CriticalGaps,
        IReadOnlyList<CategoryStatus> CategoryBreakdown);
}
