using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.GetEffectiveCompliancePolicy;

/// <summary>
/// Feature: GetEffectiveCompliancePolicy — resolve a política de compliance efetiva
/// seguindo a cascata: Service → Team → Environment → Organization.
/// Retorna a primeira política ativa encontrada no âmbito mais específico.
/// Estrutura VSA: Query + Handler + Response em arquivo único.
/// </summary>
public static class GetEffectiveCompliancePolicy
{
    /// <summary>Query para resolução da política de compliance efetiva por cascata.</summary>
    public sealed record Query(
        string? ServiceId,
        string? TeamId,
        string? EnvironmentName) : IQuery<Response>;

    /// <summary>
    /// Handler que resolve a política de compliance efetiva seguindo a cascata de âmbito.
    /// Tenta primeiro o âmbito Service, depois Team, Environment e finalmente Organization.
    /// Retorna a primeira política ativa encontrada.
    /// </summary>
    public sealed class Handler(
        IContractCompliancePolicyRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var tenantId = currentTenant.Id.ToString();

            // Cascata: Service → Team → Environment → Organization
            if (!string.IsNullOrWhiteSpace(request.ServiceId))
            {
                var policy = await repository.GetByScopeAsync(
                    tenantId, PolicyScope.Service, request.ServiceId, cancellationToken);

                if (policy is not null && policy.IsActive)
                    return MapToResponse(policy, "Service");
            }

            if (!string.IsNullOrWhiteSpace(request.TeamId))
            {
                var policy = await repository.GetByScopeAsync(
                    tenantId, PolicyScope.Team, request.TeamId, cancellationToken);

                if (policy is not null && policy.IsActive)
                    return MapToResponse(policy, "Team");
            }

            if (!string.IsNullOrWhiteSpace(request.EnvironmentName))
            {
                var policy = await repository.GetByScopeAsync(
                    tenantId, PolicyScope.Environment, request.EnvironmentName, cancellationToken);

                if (policy is not null && policy.IsActive)
                    return MapToResponse(policy, "Environment");
            }

            // Fallback: Organization scope (ScopeId = null)
            {
                var policy = await repository.GetByScopeAsync(
                    tenantId, PolicyScope.Organization, null, cancellationToken);

                if (policy is not null && policy.IsActive)
                    return MapToResponse(policy, "Organization");
            }

            return new Response(
                null, null, null, null, null, null,
                null, null, null, null, null, null,
                null, null, null, null, null, null,
                null, null, null, null, null, null,
                "None",
                "No active compliance policy found for the requested scope cascade.");
        }

        private static Response MapToResponse(
            Domain.Entities.ContractCompliancePolicy policy,
            string resolvedScope)
        {
            return new Response(
                policy.Id.Value,
                policy.Name,
                policy.Description,
                policy.Scope.ToString(),
                policy.ScopeId,
                policy.VerificationMode.ToString(),
                policy.VerificationApproach.ToString(),
                policy.OnBreakingChange.ToString(),
                policy.OnNonBreakingChange.ToString(),
                policy.OnNewEndpoint.ToString(),
                policy.OnRemovedEndpoint.ToString(),
                policy.OnMissingContract.ToString(),
                policy.OnContractNotApproved.ToString(),
                policy.AutoGenerateChangelog,
                policy.RequireChangelogApproval,
                policy.EnforceCdct,
                policy.CdctFailureAction.ToString(),
                policy.EnableRuntimeDriftDetection,
                policy.DriftDetectionIntervalMinutes,
                policy.DriftThresholdForAlert,
                policy.DriftThresholdForIncident,
                policy.NotifyOnVerificationFailure,
                policy.NotifyOnBreakingChange,
                policy.NotifyOnDriftDetected,
                resolvedScope,
                $"Effective policy resolved from {resolvedScope} scope.");
        }
    }

    /// <summary>Resposta da resolução de política de compliance efetiva.</summary>
    public sealed record Response(
        Guid? PolicyId,
        string? Name,
        string? Description,
        string? Scope,
        string? ScopeId,
        string? VerificationMode,
        string? VerificationApproach,
        string? OnBreakingChange,
        string? OnNonBreakingChange,
        string? OnNewEndpoint,
        string? OnRemovedEndpoint,
        string? OnMissingContract,
        string? OnContractNotApproved,
        bool? AutoGenerateChangelog,
        bool? RequireChangelogApproval,
        bool? EnforceCdct,
        string? CdctFailureAction,
        bool? EnableRuntimeDriftDetection,
        int? DriftDetectionIntervalMinutes,
        decimal? DriftThresholdForAlert,
        decimal? DriftThresholdForIncident,
        bool? NotifyOnVerificationFailure,
        bool? NotifyOnBreakingChange,
        bool? NotifyOnDriftDetected,
        string ResolvedScope,
        string Message);
}
