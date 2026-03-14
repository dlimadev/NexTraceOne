using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Application.Features.ListJitAccessRequests;

/// <summary>
/// Feature: ListJitAccessRequests — lista solicitações JIT pendentes de aprovação no tenant.
/// Permite que aprovadores vejam as solicitações que aguardam decisão.
/// </summary>
public static class ListJitAccessRequests
{
    /// <summary>Consulta de solicitações JIT pendentes.</summary>
    public sealed record Query() : IQuery<IReadOnlyList<JitAccessResponse>>;

    /// <summary>Handler que retorna solicitações JIT pendentes do tenant.</summary>
    public sealed class Handler(
        IJitAccessRepository jitAccessRepository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, IReadOnlyList<JitAccessResponse>>
    {
        public async Task<Result<IReadOnlyList<JitAccessResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var tenantId = TenantId.From(currentTenant.Id);
            var pending = await jitAccessRepository.ListPendingByTenantAsync(tenantId, cancellationToken);

            var responses = pending
                .Select(x => new JitAccessResponse(
                    x.Id.Value,
                    x.RequestedBy.Value,
                    x.PermissionCode,
                    x.Scope,
                    x.Justification,
                    x.Status.ToString(),
                    x.RequestedAt,
                    x.ApprovalDeadline,
                    x.GrantedFrom,
                    x.GrantedUntil))
                .ToList();

            return responses;
        }
    }

    /// <summary>Representação de uma solicitação JIT para a API.</summary>
    public sealed record JitAccessResponse(
        Guid Id,
        Guid RequestedBy,
        string PermissionCode,
        string Scope,
        string Justification,
        string Status,
        DateTimeOffset RequestedAt,
        DateTimeOffset ApprovalDeadline,
        DateTimeOffset? GrantedFrom,
        DateTimeOffset? GrantedUntil);
}
