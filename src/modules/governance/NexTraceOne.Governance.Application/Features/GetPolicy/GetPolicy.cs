using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPolicy;

/// <summary>
/// Feature: GetPolicy — detalhe de uma política de governança.
/// </summary>
public static class GetPolicy
{
    /// <summary>Query para obter detalhes de uma política.</summary>
    public sealed record Query(string PolicyId) : IQuery<Response>;

    /// <summary>Handler que retorna detalhe de uma política.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // MVP: retorna política estática baseada no ID solicitado
            var policy = new PolicyDetailDto(
                PolicyId: request.PolicyId,
                Name: "SVC-OWNER-REQ",
                DisplayName: "Service Owner Required",
                Description: "Every service must have a designated owner and team assignment. This ensures accountability and clear escalation paths.",
                Category: PolicyCategory.ServiceGovernance,
                Scope: "Services",
                Status: PolicyStatus.Active,
                Severity: PolicySeverity.High,
                EnforcementMode: PolicyEnforcementMode.HardEnforce,
                EffectiveEnvironments: new[] { "Production", "Staging" },
                AppliesTo: new[] { "All services in Production", "All services in Staging" },
                AffectedAssetsCount: 38,
                ViolationCount: 4,
                LastEvaluatedAt: DateTimeOffset.UtcNow.AddHours(-2),
                CreatedAt: DateTimeOffset.UtcNow.AddDays(-90),
                UpdatedAt: DateTimeOffset.UtcNow.AddDays(-5),
                Violations: new List<PolicyViolationDto>
                {
                    new("svc-legacy-adapter", "Legacy Adapter", "No owner assigned", DateTimeOffset.UtcNow.AddDays(-15)),
                    new("svc-batch-processor", "Batch Processor", "Owner left the organization, not reassigned", DateTimeOffset.UtcNow.AddDays(-8)),
                    new("svc-temp-worker", "Temp Worker", "Service created without owner assignment", DateTimeOffset.UtcNow.AddDays(-3)),
                    new("svc-data-import", "Data Import", "Owner team dissolved", DateTimeOffset.UtcNow.AddDays(-1))
                });

            return Task.FromResult(Result<Response>.Success(new Response(policy)));
        }
    }

    /// <summary>Resposta com detalhe completo da política.</summary>
    public sealed record Response(PolicyDetailDto Policy);

    /// <summary>DTO de detalhe de política de governança.</summary>
    public sealed record PolicyDetailDto(
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
        string[] AppliesTo,
        int AffectedAssetsCount,
        int ViolationCount,
        DateTimeOffset LastEvaluatedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyList<PolicyViolationDto> Violations);

    /// <summary>DTO de violação de política.</summary>
    public sealed record PolicyViolationDto(
        string ServiceId,
        string ServiceName,
        string Reason,
        DateTimeOffset DetectedAt);
}
