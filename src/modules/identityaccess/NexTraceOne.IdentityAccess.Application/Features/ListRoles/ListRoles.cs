using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.ListRoles;

/// <summary>
/// Feature: ListRoles — lista todos os papéis de sistema disponíveis.
/// </summary>
public static class ListRoles
{
    /// <summary>Query para listar papéis do sistema.</summary>
    public sealed record Query : IQuery<IReadOnlyList<RoleResponse>>;

    /// <summary>Handler que retorna todos os papéis (sistema + customizados).</summary>
    public sealed class Handler(
        IRoleRepository roleRepository,
        IPermissionResolver permissionResolver) : IQueryHandler<Query, IReadOnlyList<RoleResponse>>
    {
        public async Task<Result<IReadOnlyList<RoleResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var roles = await roleRepository.GetAllAsync(cancellationToken);

            var result = new List<RoleResponse>(roles.Count);
            foreach (var r in roles)
            {
                var permissions = await permissionResolver.ResolvePermissionsAsync(
                    r.Id, r.Name, tenantId: null, cancellationToken);

                result.Add(new RoleResponse(
                    r.Id.Value,
                    r.Name,
                    r.Description,
                    r.IsSystem,
                    permissions));
            }

            return result;
        }
    }

    /// <summary>Resumo de um papel com suas permissões associadas.</summary>
    public sealed record RoleResponse(
        Guid Id,
        string Name,
        string Description,
        bool IsSystem,
        IReadOnlyList<string> Permissions);
}
