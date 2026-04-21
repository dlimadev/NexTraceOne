using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Application.Features.ListPolicyDefinitions;

/// <summary>
/// Feature: ListPolicyDefinitions — lista sumários de definições de políticas para um tenant.
/// Suporta filtro por tipo e por estado activo.
/// Wave D.3 — No-code Policy Studio.
/// </summary>
public static class ListPolicyDefinitions
{
    public sealed record Query(
        string TenantId,
        int? PolicyType = null,
        bool EnabledOnly = false) : IQuery<Response>;

    public sealed record PolicySummary(
        Guid PolicyDefinitionId,
        string Name,
        PolicyDefinitionType PolicyType,
        bool IsEnabled,
        int Version,
        string AppliesTo,
        string? EnvironmentFilter);

    public sealed record Response(IReadOnlyList<PolicySummary> Policies);

    public sealed class Handler(
        IPolicyDefinitionRepository policyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            PolicyDefinitionType? typeFilter = request.PolicyType.HasValue
                ? (PolicyDefinitionType)request.PolicyType.Value
                : null;

            var policies = await policyRepository.ListByTenantAsync(request.TenantId, typeFilter, cancellationToken);

            var filtered = request.EnabledOnly
                ? policies.Where(p => p.IsEnabled).ToList()
                : policies.ToList();

            var summaries = filtered
                .Select(p => new PolicySummary(
                    p.Id.Value,
                    p.Name,
                    p.PolicyType,
                    p.IsEnabled,
                    p.Version,
                    p.AppliesTo,
                    p.EnvironmentFilter))
                .ToList();

            return Result<Response>.Success(new Response(summaries));
        }
    }
}
