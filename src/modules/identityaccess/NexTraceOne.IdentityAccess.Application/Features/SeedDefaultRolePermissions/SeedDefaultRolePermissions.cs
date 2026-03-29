using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.SeedDefaultRolePermissions;

/// <summary>
/// Feature: SeedDefaultRolePermissions — popula a tabela iam_role_permissions com os
/// mapeamentos padrão do <see cref="RolePermissionCatalog"/> quando ainda não existem
/// registos de sistema (TenantId nulo) para os papéis pré-definidos.
///
/// Objetivo de produto: permitir que o PlatformAdmin inicialize a base de dados com as
/// permissões padrão, viabilizando posterior personalização por tenant sem perder a
/// referência do catálogo estático como fallback.
/// </summary>
public static class SeedDefaultRolePermissions
{
    /// <summary>Comando sem parâmetros — a seed é determinística a partir do catálogo.</summary>
    public sealed record Command : ICommand<Response>;

    /// <summary>Resposta com contagem de papéis e permissões criadas.</summary>
    public sealed record Response(int RolesSeeded, int TotalPermissionsCreated);

    /// <summary>Handler que popula mapeamentos padrão do catálogo para papéis sem registos.</summary>
    public sealed class Handler(
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var systemRoles = await roleRepository.GetSystemRolesAsync(cancellationToken);

            var rolesSeeded = 0;
            var totalPermissions = 0;

            foreach (var role in systemRoles)
            {
                var hasExisting = await rolePermissionRepository.HasMappingsForRoleAsync(
                    role.Id, tenantId: null, cancellationToken);

                if (hasExisting)
                    continue;

                var catalogPermissions = RolePermissionCatalog.GetPermissionsForRole(role.Name);
                if (catalogPermissions.Count == 0)
                    continue;

                var now = dateTimeProvider.UtcNow;

                var entities = catalogPermissions.Select(permissionCode =>
                    RolePermission.Create(
                        RolePermissionId.New(),
                        role.Id,
                        permissionCode,
                        tenantId: null,
                        now,
                        grantedBy: "system-seed")).ToList();

                await rolePermissionRepository.AddRangeAsync(entities, cancellationToken);

                rolesSeeded++;
                totalPermissions += entities.Count;
            }

            return new Response(rolesSeeded, totalPermissions);
        }
    }
}
