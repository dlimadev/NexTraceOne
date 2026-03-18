using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPolicy;

/// <summary>
/// Feature: GetPolicy — detalhe de uma política de governança.
/// </summary>
public static class GetPolicy
{
    /// <summary>Query para obter detalhes de uma política.</summary>
    public sealed record Query(string PolicyId) : IQuery<Response>;

    /// <summary>
    /// Handler que retorna detalhe de uma política.
    /// Nesta etapa, políticas enterprise são materializadas a partir de Governance Packs,
    /// incluindo rollouts, waivers e (quando existir) bindings de regras em versões do pack.
    /// </summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernancePackVersionRepository versionRepository,
        IGovernanceWaiverRepository waiverRepository,
        IGovernanceRolloutRecordRepository rolloutRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            GovernancePack? pack;
            if (Guid.TryParse(request.PolicyId, out var packGuid))
            {
                pack = await packRepository.GetByIdAsync(new GovernancePackId(packGuid), cancellationToken);
            }
            else
            {
                // Fallback: permite lookup por nome técnico do pack
                pack = await packRepository.GetByNameAsync(request.PolicyId, cancellationToken);
            }

            if (pack is null)
                return Error.NotFound("POLICY_NOT_FOUND", "Policy '{0}' not found.", request.PolicyId);

            var version = await versionRepository.GetLatestByPackIdAsync(pack.Id, cancellationToken);
            var rollouts = await rolloutRepository.ListAsync(
                packId: pack.Id,
                scopeType: null,
                scopeValue: null,
                status: null,
                ct: cancellationToken);
            var waivers = await waiverRepository.ListAsync(pack.Id, status: null, ct: cancellationToken);

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

            var distinctScopes = rollouts
                .Select(r => (r.ScopeType, r.Scope))
                .Distinct()
                .ToList();

            var scopeLabel = distinctScopes.Count == 0
                ? "Unassigned"
                : string.Join(", ", distinctScopes.Select(s => s.ScopeType).Distinct().OrderBy(t => t).Select(t => t.ToString()));

            var latestRollout = rollouts.OrderByDescending(r => r.InitiatedAt).FirstOrDefault();
            PolicyEnforcementMode? enforcementMode = latestRollout?.EnforcementMode switch
            {
                EnforcementMode.Advisory => PolicyEnforcementMode.Advisory,
                EnforcementMode.Required => PolicyEnforcementMode.SoftEnforce,
                EnforcementMode.Blocking => PolicyEnforcementMode.HardEnforce,
                null => null,
                _ => null
            };

            var activeWaiversCount = waivers.Count(w => w.Status is WaiverStatus.Pending or WaiverStatus.Approved);

            var appliesTo = distinctScopes
                .Select(s => $"{s.ScopeType}: {s.Scope}")
                .ToArray();

            var ruleBindings = version?.Rules
                .Select(r => new PolicyRuleBindingDto(
                    RuleId: r.RuleId,
                    RuleName: r.RuleName,
                    Description: r.Description,
                    Category: r.Category,
                    DefaultEnforcementMode: r.DefaultEnforcementMode,
                    IsRequired: r.IsRequired))
                .ToList() ?? [];

            var rolloutDtos = rollouts
                .OrderByDescending(r => r.InitiatedAt)
                .Select(r => new PolicyRolloutDto(
                    RolloutId: r.Id.Value.ToString(),
                    ScopeType: r.ScopeType,
                    ScopeValue: r.Scope,
                    EnforcementMode: r.EnforcementMode,
                    Status: r.Status,
                    InitiatedBy: r.InitiatedBy,
                    InitiatedAt: r.InitiatedAt,
                    CompletedAt: r.CompletedAt))
                .ToList();

            var waiverDtos = waivers
                .OrderByDescending(w => w.RequestedAt)
                .Select(w => new PolicyWaiverDto(
                    WaiverId: w.Id.Value.ToString(),
                    RuleId: w.RuleId,
                    ScopeType: w.ScopeType,
                    ScopeValue: w.Scope,
                    Status: w.Status,
                    RequestedBy: w.RequestedBy,
                    RequestedAt: w.RequestedAt,
                    ReviewedBy: w.ReviewedBy,
                    ReviewedAt: w.ReviewedAt,
                    ExpiresAt: w.ExpiresAt,
                    Justification: w.Justification))
                .ToList();

            var policy = new PolicyDetailDto(
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
                AppliesTo: appliesTo,
                AffectedAssetsCount: distinctScopes.Count,
                WaiverCount: activeWaiversCount,
                CurrentVersion: pack.CurrentVersion,
                LastRolloutAt: latestRollout?.InitiatedAt,
                CreatedAt: pack.CreatedAt,
                UpdatedAt: pack.UpdatedAt,
                Rules: ruleBindings,
                Rollouts: rolloutDtos,
                Waivers: waiverDtos);

            return Result<Response>.Success(new Response(policy));
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
        PolicySeverity? Severity,
        PolicyEnforcementMode? EnforcementMode,
        string[] EffectiveEnvironments,
        string[] AppliesTo,
        int AffectedAssetsCount,
        int WaiverCount,
        string? CurrentVersion,
        DateTimeOffset? LastRolloutAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyList<PolicyRuleBindingDto> Rules,
        IReadOnlyList<PolicyRolloutDto> Rollouts,
        IReadOnlyList<PolicyWaiverDto> Waivers);

    public sealed record PolicyRuleBindingDto(
        string RuleId,
        string RuleName,
        string? Description,
        GovernanceRuleCategory Category,
        EnforcementMode DefaultEnforcementMode,
        bool IsRequired);

    public sealed record PolicyRolloutDto(
        string RolloutId,
        GovernanceScopeType ScopeType,
        string ScopeValue,
        EnforcementMode EnforcementMode,
        RolloutStatus Status,
        string InitiatedBy,
        DateTimeOffset InitiatedAt,
        DateTimeOffset? CompletedAt);

    public sealed record PolicyWaiverDto(
        string WaiverId,
        string? RuleId,
        GovernanceScopeType ScopeType,
        string ScopeValue,
        WaiverStatus Status,
        string RequestedBy,
        DateTimeOffset RequestedAt,
        string? ReviewedBy,
        DateTimeOffset? ReviewedAt,
        DateTimeOffset? ExpiresAt,
        string Justification);
}
