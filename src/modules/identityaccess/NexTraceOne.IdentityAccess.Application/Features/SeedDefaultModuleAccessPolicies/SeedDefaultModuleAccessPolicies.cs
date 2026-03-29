using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.SeedDefaultModuleAccessPolicies;

/// <summary>
/// Feature: SeedDefaultModuleAccessPolicies — popula a tabela iam_module_access_policies com os
/// mapeamentos padrão do <see cref="ModuleAccessPolicyCatalog"/> quando ainda não existem
/// registos de sistema (TenantId nulo) para os papéis pré-definidos.
///
/// Complementa o <see cref="SeedDefaultRolePermissions"/> fornecendo políticas ao nível
/// de módulo/página/ação para cada papel, viabilizando controlo granular de acesso
/// na interface do produto sem redeploy.
///
/// Objetivo de produto: permitir que o PlatformAdmin inicialize a base de dados com as
/// políticas de acesso padrão por módulo/página/ação para ambientes de desenvolvimento
/// e produção.
/// </summary>
public static class SeedDefaultModuleAccessPolicies
{
    /// <summary>Comando sem parâmetros — a seed é determinística a partir do catálogo.</summary>
    public sealed record Command : ICommand<Response>;

    /// <summary>Resposta com contagem de papéis e políticas criadas.</summary>
    public sealed record Response(int RolesSeeded, int TotalPoliciesCreated);

    /// <summary>Handler que popula políticas padrão do catálogo para papéis sem registos.</summary>
    public sealed class Handler(
        IRoleRepository roleRepository,
        IModuleAccessPolicyRepository moduleAccessPolicyRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var systemRoles = await roleRepository.GetSystemRolesAsync(cancellationToken);

            var rolesSeeded = 0;
            var totalPolicies = 0;

            foreach (var role in systemRoles)
            {
                var hasExisting = await moduleAccessPolicyRepository.HasPoliciesForRoleAsync(
                    role.Id, tenantId: null, cancellationToken);

                if (hasExisting)
                    continue;

                var catalogPolicies = ModuleAccessPolicyCatalog.GetPoliciesForRole(role.Name);
                if (catalogPolicies.Count == 0)
                    continue;

                var now = dateTimeProvider.UtcNow;

                var entities = catalogPolicies.Select(entry =>
                    ModuleAccessPolicy.Create(
                        role.Id,
                        tenantId: null,
                        entry.Module,
                        entry.Page,
                        entry.Action,
                        entry.IsAllowed,
                        now,
                        createdBy: "system-seed")).ToList();

                await moduleAccessPolicyRepository.AddRangeAsync(entities, cancellationToken);

                rolesSeeded++;
                totalPolicies += entities.Count;
            }

            return new Response(rolesSeeded, totalPolicies);
        }
    }
}
