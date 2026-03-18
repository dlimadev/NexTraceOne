using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
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

    /// <summary>
    /// Handler que retorna o catálogo de políticas de governança.
    /// Nesta etapa, políticas enterprise são materializadas a partir de dados reais de Governance Packs,
    /// rollouts e waivers persistidos no módulo Governance.
    /// </summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernanceWaiverRepository waiverRepository,
        IGovernanceRolloutRecordRepository rolloutRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            PolicyCategory? categoryFilter = null;
            if (!string.IsNullOrWhiteSpace(request.Category) &&
                Enum.TryParse<PolicyCategory>(request.Category, ignoreCase: true, out var cat))
                categoryFilter = cat;

            PolicyStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<PolicyStatus>(request.Status, ignoreCase: true, out var st))
                statusFilter = st;

            var packs = await packRepository.ListAsync(category: null, status: null, ct: cancellationToken);
            var waivers = await waiverRepository.ListAsync(packId: null, status: null, ct: cancellationToken);
            var rollouts = await rolloutRepository.ListAsync(
                packId: null,
                scopeType: null,
                scopeValue: null,
                status: null,
                ct: cancellationToken);

            var waiversByPack = waivers
                .GroupBy(w => w.PackId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rolloutsByPack = rollouts
                .GroupBy(r => r.PackId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var policies = packs
                .Select(p => ToPolicyDto(p, waiversByPack, rolloutsByPack))
                .Where(p => categoryFilter is null || p.Category == categoryFilter)
                .Where(p => statusFilter is null || p.Status == statusFilter)
                .OrderBy(p => p.DisplayName)
                .ToList();

            // Totais devem refletir o universo (sem filtros)
            var allPolicyDtos = packs
                .Select(p => ToPolicyDto(p, waiversByPack, rolloutsByPack))
                .ToList();

            var response = new Response(
                TotalPolicies: allPolicyDtos.Count,
                ActiveCount: allPolicyDtos.Count(p => p.Status == PolicyStatus.Active),
                DraftCount: allPolicyDtos.Count(p => p.Status == PolicyStatus.Draft),
                Policies: policies);

            return Result<Response>.Success(response);
        }

        private static PolicyDto ToPolicyDto(
            GovernancePack pack,
            IReadOnlyDictionary<GovernancePackId, List<GovernanceWaiver>> waiversByPack,
            IReadOnlyDictionary<GovernancePackId, List<GovernanceRolloutRecord>> rolloutsByPack)
        {
            waiversByPack.TryGetValue(pack.Id, out var waivers);
            rolloutsByPack.TryGetValue(pack.Id, out var rollouts);

            var activeWaivers = (waivers ?? [])
                .Count(w => w.Status is WaiverStatus.Pending or WaiverStatus.Approved);

            var distinctScopes = (rollouts ?? [])
                .Select(r => (r.ScopeType, r.Scope))
                .Distinct()
                .Count();

            var scopeLabel = (rollouts ?? []).Count == 0
                ? "Unassigned"
                : string.Join(", ", (rollouts ?? [])
                    .Select(r => r.ScopeType)
                    .Distinct()
                    .OrderBy(t => t)
                    .Select(t => t.ToString()));

            var policyStatus = pack.Status switch
            {
                GovernancePackStatus.Published => PolicyStatus.Active,
                GovernancePackStatus.Draft => PolicyStatus.Draft,
                GovernancePackStatus.Deprecated => PolicyStatus.Deprecated,
                GovernancePackStatus.Archived => PolicyStatus.Inactive,
                _ => PolicyStatus.Inactive
            };

            var policyCategory = pack.Category switch
            {
                GovernanceRuleCategory.Contracts => PolicyCategory.ContractGovernance,
                GovernanceRuleCategory.SourceOfTruth => PolicyCategory.ServiceGovernance,
                GovernanceRuleCategory.Changes => PolicyCategory.ChangeGovernance,
                GovernanceRuleCategory.Incidents => PolicyCategory.OperationalReadiness,
                GovernanceRuleCategory.AIGovernance => PolicyCategory.AiGovernance,
                GovernanceRuleCategory.Reliability => PolicyCategory.OperationalReadiness,
                GovernanceRuleCategory.Operations => PolicyCategory.OperationalReadiness,
                _ => PolicyCategory.ServiceGovernance
            };

            // Enforcement/severity são derivados apenas quando houver dados reais persistidos.
            var latestRollout = (rollouts ?? []).OrderByDescending(r => r.InitiatedAt).FirstOrDefault();
            PolicyEnforcementMode? enforcementMode = latestRollout?.EnforcementMode switch
            {
                EnforcementMode.Advisory => PolicyEnforcementMode.Advisory,
                EnforcementMode.Required => PolicyEnforcementMode.SoftEnforce,
                EnforcementMode.Blocking => PolicyEnforcementMode.HardEnforce,
                null => null,
                _ => null
            };

            return new PolicyDto(
                PolicyId: pack.Id.Value.ToString(),
                Name: pack.Name,
                DisplayName: pack.DisplayName,
                Description: pack.Description ?? string.Empty,
                Category: policyCategory,
                Scope: scopeLabel,
                Status: policyStatus,
                Severity: null,
                EnforcementMode: enforcementMode,
                EffectiveEnvironments: [],
                AffectedAssetsCount: distinctScopes,
                ViolationCount: activeWaivers,
                CreatedAt: pack.CreatedAt);
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
        PolicySeverity? Severity,
        PolicyEnforcementMode? EnforcementMode,
        string[] EffectiveEnvironments,
        int AffectedAssetsCount,
        int ViolationCount,
        DateTimeOffset CreatedAt);
}
