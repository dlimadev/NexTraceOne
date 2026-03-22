using Ardalis.GuardClauses;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.ListCompliancePolicies;

/// <summary>
/// Feature: ListCompliancePolicies — lista políticas de compliance com filtros opcionais.
/// </summary>
public static class ListCompliancePolicies
{
    /// <summary>Query de listagem de políticas de compliance.</summary>
    public sealed record Query(bool? IsActive, string? Category) : IQuery<Response>;

    /// <summary>Handler que lista as políticas de compliance.</summary>
    public sealed class Handler(ICompliancePolicyRepository compliancePolicyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policies = await compliancePolicyRepository.ListAsync(request.IsActive, request.Category, cancellationToken);

            var items = policies
                .Select(p => new CompliancePolicyItem(
                    p.Id.Value, p.Name, p.DisplayName, p.Category,
                    p.Severity, p.IsActive, p.CreatedAt))
                .ToArray();

            return new Response(items);
        }
    }

    /// <summary>Resposta da listagem de políticas.</summary>
    public sealed record Response(IReadOnlyList<CompliancePolicyItem> Items);

    /// <summary>Item de política de compliance.</summary>
    public sealed record CompliancePolicyItem(
        Guid PolicyId, string Name, string DisplayName, string Category,
        ComplianceSeverity Severity, bool IsActive, DateTimeOffset CreatedAt);
}
