using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListApprovalPolicies;

/// <summary>
/// Feature: ListApprovalPolicies — lista políticas de aprovação activas do tenant.
///
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ListApprovalPolicies
{
    /// <summary>Query para listar políticas de aprovação.</summary>
    public sealed record Query(
        string? EnvironmentId = null,
        Guid? ServiceId = null) : IQuery<IReadOnlyList<PolicyDto>>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.EnvironmentId).MaximumLength(200).When(x => x.EnvironmentId is not null);
        }
    }

    /// <summary>Handler que retorna as políticas activas do tenant.</summary>
    public sealed class Handler(
        IReleaseApprovalPolicyRepository policyRepository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, IReadOnlyList<PolicyDto>>
    {
        public async Task<Result<IReadOnlyList<PolicyDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var policies = request.EnvironmentId is not null || request.ServiceId.HasValue
                ? await policyRepository.ListByEnvironmentAndServiceAsync(
                    currentTenant.TenantId, request.EnvironmentId, request.ServiceId, cancellationToken)
                : await policyRepository.ListActiveAsync(currentTenant.TenantId, cancellationToken);

            return Result.Ok<IReadOnlyList<PolicyDto>>(
                policies.Select(p => new PolicyDto(
                    p.Id.Value,
                    p.Name,
                    p.EnvironmentId,
                    p.ServiceId,
                    p.ServiceTag,
                    p.RequiresApproval,
                    p.ApprovalType,
                    p.ExternalWebhookUrl,
                    p.MinApprovers,
                    p.ExpirationHours,
                    p.RequireEvidencePack,
                    p.RequireChecklistCompletion,
                    p.MinRiskScoreForManualApproval,
                    p.Priority,
                    p.IsActive,
                    p.CreatedAt,
                    p.CreatedBy)).ToList());
        }
    }

    /// <summary>DTO de política de aprovação para uso no frontend.</summary>
    public sealed record PolicyDto(
        Guid Id,
        string Name,
        string? EnvironmentId,
        Guid? ServiceId,
        string? ServiceTag,
        bool RequiresApproval,
        string ApprovalType,
        string? ExternalWebhookUrl,
        int MinApprovers,
        int ExpirationHours,
        bool RequireEvidencePack,
        bool RequireChecklistCompletion,
        int? MinRiskScoreForManualApproval,
        int Priority,
        bool IsActive,
        DateTimeOffset CreatedAt,
        string CreatedBy);
}
