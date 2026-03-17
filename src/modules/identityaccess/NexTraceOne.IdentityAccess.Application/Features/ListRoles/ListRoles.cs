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

    /// <summary>Handler que retorna todos os papéis pré-definidos.</summary>
    public sealed class Handler(IRoleRepository roleRepository) : IQueryHandler<Query, IReadOnlyList<RoleResponse>>
    {
        public async Task<Result<IReadOnlyList<RoleResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var roles = await roleRepository.GetSystemRolesAsync(cancellationToken);

            var result = roles.Select(r => new RoleResponse(
                r.Id.Value,
                r.Name,
                r.Description,
                r.IsSystem,
                Role.GetPermissionsForRole(r.Name))).ToList();

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
