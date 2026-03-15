using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListPolicies;

/// <summary>
/// Feature: ListPolicies — catálogo de políticas de governança enterprise.
/// Retorna políticas configuradas com categoria, status, severidade e modo de aplicação.
/// </summary>
public static class ListPolicies
{
    /// <summary>Query para listar políticas. Permite filtragem por categoria e status.</summary>
    public sealed record Query(
        string? Category = null,
        string? Status = null) : IQuery<Response>;

    /// <summary>Handler que retorna o catálogo de políticas de governança.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var policies = new List<PolicyDto>
            {
                new("pol-001", "SVC-OWNER-REQ", "Service Owner Required",
                    "Every service must have a designated owner and team assignment",
                    PolicyCategory.ServiceGovernance, "Services", PolicyStatus.Active,
                    PolicySeverity.High, PolicyEnforcementMode.HardEnforce,
                    new[] { "Production", "Staging" }, 38, 4, DateTimeOffset.UtcNow.AddDays(-90)),
                new("pol-002", "CONTRACT-EXISTS", "Contract Definition Required",
                    "All externally consumed services must have a published contract",
                    PolicyCategory.ContractGovernance, "Contracts", PolicyStatus.Active,
                    PolicySeverity.High, PolicyEnforcementMode.SoftEnforce,
                    new[] { "Production" }, 42, 7, DateTimeOffset.UtcNow.AddDays(-85)),
                new("pol-003", "RUNBOOK-PRESENT", "Runbook Availability",
                    "Critical and high-criticality services must have an operational runbook",
                    PolicyCategory.OperationalReadiness, "Operations", PolicyStatus.Active,
                    PolicySeverity.Medium, PolicyEnforcementMode.SoftEnforce,
                    new[] { "Production" }, 28, 12, DateTimeOffset.UtcNow.AddDays(-60)),
                new("pol-004", "CHANGE-VALIDATION", "Change Validation Required",
                    "All production changes must pass validation and blast radius assessment",
                    PolicyCategory.ChangeGovernance, "Changes", PolicyStatus.Active,
                    PolicySeverity.Critical, PolicyEnforcementMode.HardEnforce,
                    new[] { "Production" }, 156, 3, DateTimeOffset.UtcNow.AddDays(-120)),
                new("pol-005", "AI-MODEL-POLICY", "AI Model Usage Policy",
                    "AI model usage must comply with approved model registry and access policies",
                    PolicyCategory.AiGovernance, "AI", PolicyStatus.Active,
                    PolicySeverity.High, PolicyEnforcementMode.HardEnforce,
                    new[] { "Production", "Staging", "Development" }, 89, 2, DateTimeOffset.UtcNow.AddDays(-45)),
                new("pol-006", "DOC-STANDARDS", "Documentation Standards",
                    "Services must have up-to-date technical documentation and API references",
                    PolicyCategory.DocumentationStandards, "Knowledge", PolicyStatus.Active,
                    PolicySeverity.Medium, PolicyEnforcementMode.Advisory,
                    new[] { "Production", "Staging" }, 42, 15, DateTimeOffset.UtcNow.AddDays(-70)),
                new("pol-007", "DEP-MAPPING", "Dependency Mapping Required",
                    "All services must have dependencies mapped in the service catalog",
                    PolicyCategory.ServiceGovernance, "Services", PolicyStatus.Active,
                    PolicySeverity.Medium, PolicyEnforcementMode.SoftEnforce,
                    new[] { "Production" }, 42, 5, DateTimeOffset.UtcNow.AddDays(-55)),
                new("pol-008", "INCIDENT-EVIDENCE", "Incident Mitigation Evidence",
                    "Incident resolution must include mitigation evidence and post-mortem when applicable",
                    PolicyCategory.OperationalReadiness, "Operations", PolicyStatus.Draft,
                    PolicySeverity.High, PolicyEnforcementMode.Advisory,
                    new[] { "Production" }, 0, 0, DateTimeOffset.UtcNow.AddDays(-10)),
                new("pol-009", "VERSION-CONTROL", "Contract Versioning Required",
                    "Contracts must follow semantic versioning with proper changelog",
                    PolicyCategory.ContractGovernance, "Contracts", PolicyStatus.Active,
                    PolicySeverity.Medium, PolicyEnforcementMode.SoftEnforce,
                    new[] { "Production", "Staging" }, 35, 8, DateTimeOffset.UtcNow.AddDays(-80)),
                new("pol-010", "SEC-REVIEW", "Security Review Required",
                    "Services handling sensitive data must pass security review before production deployment",
                    PolicyCategory.SecurityCompliance, "Security", PolicyStatus.Active,
                    PolicySeverity.Critical, PolicyEnforcementMode.HardEnforce,
                    new[] { "Production" }, 18, 1, DateTimeOffset.UtcNow.AddDays(-100))
            };

            // Apply filters
            IEnumerable<PolicyDto> filtered = policies;
            if (!string.IsNullOrEmpty(request.Category) &&
                Enum.TryParse<PolicyCategory>(request.Category, out var cat))
                filtered = filtered.Where(p => p.Category == cat);
            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<PolicyStatus>(request.Status, out var st))
                filtered = filtered.Where(p => p.Status == st);

            var list = filtered.ToList();

            var response = new Response(
                TotalPolicies: policies.Count,
                ActiveCount: policies.Count(p => p.Status == PolicyStatus.Active),
                DraftCount: policies.Count(p => p.Status == PolicyStatus.Draft),
                Policies: list);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com lista de políticas de governança.</summary>
    public sealed record Response(
        int TotalPolicies,
        int ActiveCount,
        int DraftCount,
        IReadOnlyList<PolicyDto> Policies);

    /// <summary>DTO de uma política de governança.</summary>
    public sealed record PolicyDto(
        string PolicyId,
        string Name,
        string DisplayName,
        string Description,
        PolicyCategory Category,
        string Scope,
        PolicyStatus Status,
        PolicySeverity Severity,
        PolicyEnforcementMode EnforcementMode,
        string[] EffectiveEnvironments,
        int AffectedAssetsCount,
        int ViolationCount,
        DateTimeOffset CreatedAt);
}
