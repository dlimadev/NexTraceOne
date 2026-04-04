using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.GetComplianceFrameworkSummary;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.ExportComplianceEvidences;

/// <summary>
/// Feature: ExportComplianceEvidences — exporta um pacote de evidências de compliance
/// agrupado por framework, política e recurso, pronto para revisão por auditores.
///
/// O pacote inclui:
///   - Lista de políticas avaliadas e seus resultados
///   - Detalhes e racionais de cada avaliação
///   - Metadata de auditoria (who, when, tenant)
///   - Estado de conformidade por categoria e framework
///   - Gaps identificados com severidade
///
/// Formato: lista estruturada de evidências com referência de exportação única (ExportRef).
///
/// Valor: auditorias que levavam semanas passam a ser contínuas e self-service —
/// o auditor obtém o pacote completo numa query.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ExportComplianceEvidences
{
    /// <summary>Query para exportar evidências de compliance.</summary>
    public sealed record Query(
        Guid TenantId,
        string? Framework,
        string? Category,
        DateTimeOffset? From,
        DateTimeOffset? To,
        bool IncludeCompliant = true) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Framework).MaximumLength(50).When(x => x.Framework is not null);
            RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
        }
    }

    /// <summary>Handler que compõe o pacote de evidências de compliance.</summary>
    public sealed class Handler(
        ICompliancePolicyRepository policyRepository,
        IComplianceResultRepository resultRepository,
        IAuditEventRepository auditEventRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var policies = await policyRepository.ListAsync(
                isActive: null,
                category: request.Category,
                cancellationToken);

            var tenantPolicies = policies
                .Where(p => p.TenantId == request.TenantId)
                .ToList();

            if (request.Framework is not null
                && GetComplianceFrameworkSummary.GetComplianceFrameworkSummary.FrameworkCategories.TryGetValue(
                    request.Framework, out var frameworkCategories))
            {
                tenantPolicies = tenantPolicies
                    .Where(p => frameworkCategories.Any(c => c.Equals(p.Category, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            var allResults = await resultRepository.ListAsync(
                null, null, null, cancellationToken);

            var tenantResults = allResults.Where(r => r.TenantId == request.TenantId).ToList();

            if (request.From.HasValue)
                tenantResults = tenantResults.Where(r => r.EvaluatedAt >= request.From.Value).ToList();
            if (request.To.HasValue)
                tenantResults = tenantResults.Where(r => r.EvaluatedAt <= request.To.Value).ToList();

            if (!request.IncludeCompliant)
                tenantResults = tenantResults
                    .Where(r => r.Outcome != ComplianceOutcome.Compliant)
                    .ToList();

            var evidences = new List<EvidenceItem>();

            foreach (var policy in tenantPolicies)
            {
                var policyResults = tenantResults
                    .Where(r => r.PolicyId == policy.Id)
                    .OrderByDescending(r => r.EvaluatedAt)
                    .ToList();

                foreach (var result in policyResults)
                {
                    evidences.Add(new EvidenceItem(
                        EvidenceId: result.Id.Value,
                        PolicyId: policy.Id.Value,
                        PolicyName: policy.DisplayName,
                        Category: policy.Category,
                        Severity: policy.Severity,
                        ResourceType: result.ResourceType,
                        ResourceId: result.ResourceId,
                        Outcome: result.Outcome,
                        Rationale: result.Details ?? "No rationale recorded.",
                        EvaluatedBy: result.EvaluatedBy,
                        EvaluatedAt: result.EvaluatedAt));
                }
            }

            var compliantCount = evidences.Count(e => e.Outcome == ComplianceOutcome.Compliant);
            var nonCompliantCount = evidences.Count(e => e.Outcome == ComplianceOutcome.NonCompliant);

            var exportRef = $"AUDIT-{request.TenantId.ToString("N")[..8].ToUpperInvariant()}-{DateTimeOffset.UtcNow:yyyyMMddHHmm}";

            return new Response(
                ExportRef: exportRef,
                TenantId: request.TenantId,
                Framework: request.Framework,
                GeneratedAt: DateTimeOffset.UtcNow,
                TotalEvidences: evidences.Count,
                Compliant: compliantCount,
                NonCompliant: nonCompliantCount,
                Evidences: evidences.AsReadOnly());
        }
    }

    /// <summary>Item de evidência de compliance exportado.</summary>
    public sealed record EvidenceItem(
        Guid EvidenceId,
        Guid PolicyId,
        string PolicyName,
        string Category,
        ComplianceSeverity Severity,
        string ResourceType,
        string ResourceId,
        ComplianceOutcome Outcome,
        string Rationale,
        string EvaluatedBy,
        DateTimeOffset EvaluatedAt);

    /// <summary>Resposta da exportação de evidências de compliance.</summary>
    public sealed record Response(
        string ExportRef,
        Guid TenantId,
        string? Framework,
        DateTimeOffset GeneratedAt,
        int TotalEvidences,
        int Compliant,
        int NonCompliant,
        IReadOnlyList<EvidenceItem> Evidences);
}
