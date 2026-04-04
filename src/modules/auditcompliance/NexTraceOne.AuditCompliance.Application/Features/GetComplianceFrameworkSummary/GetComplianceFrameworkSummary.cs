using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.GetComplianceFrameworkSummary;

/// <summary>
/// Feature: GetComplianceFrameworkSummary — retorna o estado de conformidade
/// para um framework regulatório específico (SOC 2, ISO 27001, LGPD/GDPR, PCI-DSS).
///
/// Para cada framework, agrupa as políticas ativas por categoria e calcula:
///   - Total de controlos definidos
///   - Controlos compliant vs non-compliant vs not-evaluated
///   - Score de compliance do framework (%)
///   - Estado geral: Green / Amber / Red
///
/// Valor: visibilidade executiva de conformidade regulatória por framework, substituindo
/// auditorias manuais periódicas por estado contínuo e consultável em tempo real.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetComplianceFrameworkSummary
{
    /// <summary>Frameworks regulatórios suportados.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> FrameworkCategories =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["SOC2"] = ["Security", "Availability", "ProcessingIntegrity", "Confidentiality", "Privacy"],
            ["ISO27001"] = ["InformationSecurity", "RiskManagement", "AccessControl", "Cryptography", "PhysicalSecurity"],
            ["LGPD"] = ["DataProtection", "ConsentManagement", "DataSubjectRights", "DataRetention", "BreachNotification"],
            ["GDPR"] = ["DataProtection", "ConsentManagement", "DataSubjectRights", "DataRetention", "BreachNotification"],
            ["PCI-DSS"] = ["NetworkSecurity", "CardholderDataProtection", "VulnerabilityManagement", "AccessControl", "Monitoring"]
        };

    /// <summary>Query para obter o resumo de conformidade de um framework.</summary>
    public sealed record Query(
        string Framework,
        Guid TenantId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Framework)
                .NotEmpty()
                .MaximumLength(50)
                .Must(f => FrameworkCategories.ContainsKey(f))
                .WithMessage("Framework must be one of: SOC2, ISO27001, LGPD, GDPR, PCI-DSS.");
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que agrega resultados de compliance por framework.</summary>
    public sealed class Handler(
        ICompliancePolicyRepository policyRepository,
        IComplianceResultRepository resultRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var frameworkCategories = FrameworkCategories[request.Framework];

            var policies = await policyRepository.ListAsync(
                isActive: true, category: null, cancellationToken);

            var frameworkPolicies = policies
                .Where(p => p.TenantId == request.TenantId
                         && frameworkCategories.Any(c => c.Equals(p.Category, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var categoryBreakdowns = new List<CategoryBreakdown>();
            var allResults = new List<(ComplianceOutcome Outcome, ComplianceSeverity Severity)>();

            foreach (var category in frameworkCategories)
            {
                var categoryPolicies = frameworkPolicies
                    .Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var categoryResults = new List<(ComplianceOutcome Outcome, ComplianceSeverity Severity)>();
                foreach (var policy in categoryPolicies)
                {
                    var results = await resultRepository.ListAsync(
                        policy.Id, null, null, cancellationToken);

                    var latestOutcome = results
                        .OrderByDescending(r => r.EvaluatedAt)
                        .FirstOrDefault()?.Outcome ?? ComplianceOutcome.NotApplicable;

                    categoryResults.Add((latestOutcome, policy.Severity));
                    allResults.Add((latestOutcome, policy.Severity));
                }

                var compliant = categoryResults.Count(r => r.Outcome == ComplianceOutcome.Compliant);
                var nonCompliant = categoryResults.Count(r => r.Outcome == ComplianceOutcome.NonCompliant);
                var partial = categoryResults.Count(r => r.Outcome == ComplianceOutcome.PartiallyCompliant);
                var notEval = categoryResults.Count(r => r.Outcome == ComplianceOutcome.NotApplicable);

                categoryBreakdowns.Add(new CategoryBreakdown(
                    Category: category,
                    TotalControls: categoryResults.Count,
                    Compliant: compliant,
                    NonCompliant: nonCompliant,
                    PartiallyCompliant: partial,
                    NotEvaluated: notEval,
                    ComplianceScore: categoryResults.Count == 0
                        ? 100m
                        : Math.Round((decimal)(compliant + partial * 0.5m) / categoryResults.Count * 100, 1)));
            }

            var totalCompliant = allResults.Count(r => r.Outcome == ComplianceOutcome.Compliant);
            var totalNonCompliant = allResults.Count(r => r.Outcome == ComplianceOutcome.NonCompliant);
            var totalPartial = allResults.Count(r => r.Outcome == ComplianceOutcome.PartiallyCompliant);
            var overallScore = allResults.Count == 0
                ? 100m
                : Math.Round((decimal)(totalCompliant + totalPartial * 0.5m) / allResults.Count * 100, 1);

            var hasCriticalNonCompliant = allResults.Any(r =>
                r.Outcome == ComplianceOutcome.NonCompliant && r.Severity == ComplianceSeverity.Critical);

            var status = hasCriticalNonCompliant ? "Red"
                : totalNonCompliant > 0 ? "Amber"
                : "Green";

            return new Response(
                Framework: request.Framework,
                TenantId: request.TenantId,
                OverallStatus: status,
                OverallScore: overallScore,
                TotalControls: allResults.Count,
                Compliant: totalCompliant,
                NonCompliant: totalNonCompliant,
                PartiallyCompliant: totalPartial,
                NotEvaluated: allResults.Count(r => r.Outcome == ComplianceOutcome.NotApplicable),
                CategoryBreakdowns: categoryBreakdowns.AsReadOnly());
        }
    }

    /// <summary>Detalhe de conformidade por categoria do framework.</summary>
    public sealed record CategoryBreakdown(
        string Category,
        int TotalControls,
        int Compliant,
        int NonCompliant,
        int PartiallyCompliant,
        int NotEvaluated,
        decimal ComplianceScore);

    /// <summary>Resposta do resumo de conformidade por framework.</summary>
    public sealed record Response(
        string Framework,
        Guid TenantId,
        string OverallStatus,
        decimal OverallScore,
        int TotalControls,
        int Compliant,
        int NonCompliant,
        int PartiallyCompliant,
        int NotEvaluated,
        IReadOnlyList<CategoryBreakdown> CategoryBreakdowns);
}
