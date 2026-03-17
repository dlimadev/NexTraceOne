using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.ListDelegations;

/// <summary>
/// Feature: ListDelegations — lista delegações ativas no tenant atual.
/// Inclui informações de delegante, delegatário, permissões e vigência.
/// </summary>
public static class ListDelegations
{
    /// <summary>Consulta de delegações ativas.</summary>
    public sealed record Query() : IQuery<IReadOnlyList<DelegationResponse>>;

    /// <summary>Handler que retorna delegações ativas do tenant.</summary>
    public sealed class Handler(
        IDelegationRepository delegationRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, IReadOnlyList<DelegationResponse>>
    {
        public async Task<Result<IReadOnlyList<DelegationResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var tenantId = TenantId.From(currentTenant.Id);
            var now = dateTimeProvider.UtcNow;

            var delegations = await delegationRepository.ListActiveByTenantAsync(tenantId, now, cancellationToken);

            var responses = delegations
                .Select(d => new DelegationResponse(
                    d.Id.Value,
                    d.GrantorId.Value,
                    d.DelegateeId.Value,
                    d.DelegatedPermissions.ToList(),
                    d.Reason,
                    d.Status.ToString(),
                    d.ValidFrom,
                    d.ValidUntil,
                    d.CreatedAt))
                .ToList();

            return responses;
        }
    }

    /// <summary>Representação de uma delegação para a API.</summary>
    public sealed record DelegationResponse(
        Guid Id,
        Guid GrantorId,
        Guid DelegateeId,
        IReadOnlyList<string> Permissions,
        string Reason,
        string Status,
        DateTimeOffset ValidFrom,
        DateTimeOffset ValidUntil,
        DateTimeOffset CreatedAt);
}
