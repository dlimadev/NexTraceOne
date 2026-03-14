using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.ListMyTenants;

/// <summary>
/// Feature: ListMyTenants — lista os tenants disponíveis para o usuário autenticado.
///
/// Utilizada após autenticação para permitir seleção amigável de tenant.
/// Retorna nome, slug e status de cada tenant vinculado via membership ativa.
/// Se o usuário tiver apenas um tenant ativo, o frontend pode resolver automaticamente.
/// </summary>
public static class ListMyTenants
{
    /// <summary>Query sem parâmetros — os tenants são resolvidos pelo usuário atual.</summary>
    public sealed record Query : IQuery<IReadOnlyList<TenantInfo>>;

    /// <summary>Informações amigáveis de um tenant para exibição no frontend.</summary>
    public sealed record TenantInfo(
        Guid Id,
        string Name,
        string Slug,
        bool IsActive,
        string RoleName);

    /// <summary>Handler que resolve os tenants do usuário autenticado.</summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        ITenantMembershipRepository membershipRepository,
        ITenantRepository tenantRepository,
        IRoleRepository roleRepository) : IQueryHandler<Query, IReadOnlyList<TenantInfo>>
    {
        public async Task<Result<IReadOnlyList<TenantInfo>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (string.IsNullOrWhiteSpace(currentUser.Id))
                return IdentityErrors.NotAuthenticated();

            var userId = UserId.From(Guid.Parse(currentUser.Id));

            var memberships = await membershipRepository.ListByUserAsync(userId, cancellationToken);
            var activeMemberships = memberships.Where(m => m.IsActive).ToList();

            if (activeMemberships.Count == 0)
                return Result<IReadOnlyList<TenantInfo>>.Success(Array.Empty<TenantInfo>());

            var tenantIds = activeMemberships.Select(m => m.TenantId).Distinct().ToList();
            var tenants = await tenantRepository.GetByIdsAsync(tenantIds, cancellationToken);

            var roleIds = activeMemberships.Select(m => m.RoleId).Distinct().ToList();
            var roles = await roleRepository.GetByIdsAsync(roleIds, cancellationToken);

            var result = activeMemberships
                .Where(m => tenants.ContainsKey(m.TenantId))
                .Select(m =>
                {
                    var tenant = tenants[m.TenantId];
                    var roleName = roles.TryGetValue(m.RoleId, out var role) ? role.Name : "Unknown";
                    return new TenantInfo(
                        tenant.Id.Value,
                        tenant.Name,
                        tenant.Slug,
                        tenant.IsActive,
                        roleName);
                })
                .ToList();

            return Result<IReadOnlyList<TenantInfo>>.Success(result);
        }
    }
}
