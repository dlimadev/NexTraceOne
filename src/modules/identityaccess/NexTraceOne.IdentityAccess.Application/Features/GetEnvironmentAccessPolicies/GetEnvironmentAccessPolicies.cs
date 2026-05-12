using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Application.Features.GetEnvironmentAccessPolicies;

/// <summary>Feature: GetEnvironmentAccessPolicies — lista políticas activas do tenant.</summary>
public static class GetEnvironmentAccessPolicies
{
    /// <summary>Query para listar políticas de acesso por ambiente do tenant actual.</summary>
    public sealed record Query : IQuery<IReadOnlyList<PolicyDto>>;

    /// <summary>DTO com os dados de uma política de acesso por ambiente.</summary>
    public sealed record PolicyDto(
        Guid Id,
        string PolicyName,
        IReadOnlyList<string> Environments,
        IReadOnlyList<string> AllowedRoles,
        IReadOnlyList<string> RequireJitForRoles,
        string? JitApprovalRequiredFrom,
        bool IsActive);

    /// <summary>Handler que lista as políticas activas do tenant.</summary>
    internal sealed class Handler(
        IEnvironmentAccessPolicyRepository repository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, IReadOnlyList<PolicyDto>>
    {
        public async Task<Result<IReadOnlyList<PolicyDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policies = await repository.ListByTenantAsync(currentTenant.Id, cancellationToken);

            var dtos = policies.Select(p => new PolicyDto(
                p.Id.Value,
                p.PolicyName,
                p.Environments,
                p.AllowedRoles,
                p.RequireJitForRoles,
                p.JitApprovalRequiredFrom,
                p.IsActive)).ToList();

            return dtos;
        }
    }
}
