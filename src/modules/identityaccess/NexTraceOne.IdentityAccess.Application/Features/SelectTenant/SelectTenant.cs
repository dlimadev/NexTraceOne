using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.SelectTenant;

/// <summary>
/// Feature: SelectTenant — seleciona o tenant ativo após autenticação.
///
/// Fluxo de 2 fases:
/// 1. Usuário autentica (LocalLogin) → JWT pré-seleção com o primeiro tenant ativo.
/// 2. Se tiver múltiplos tenants, chama SelectTenant para trocar o contexto.
///
/// Emite um novo JWT com o tenant selecionado, preservando a sessão existente.
/// Valida que o usuário possui membership ativa no tenant escolhido.
/// </summary>
public static class SelectTenant
{
    /// <summary>Comando para seleção de tenant pelo usuário autenticado.</summary>
    public sealed record Command(Guid TenantId) : ICommand<Response>;

    /// <summary>Resposta com o novo token emitido para o tenant selecionado.</summary>
    public sealed record Response(
        string AccessToken,
        int ExpiresIn,
        Guid TenantId,
        string TenantName,
        string RoleName,
        IReadOnlyList<string> Permissions);

    /// <summary>Valida a entrada da seleção de tenant.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que processa a seleção de tenant e reemite JWT.</summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        ITenantMembershipRepository membershipRepository,
        ITenantRepository tenantRepository,
        IRoleRepository roleRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPermissionResolver permissionResolver,
        ITenantLicenseRepository licenseRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (string.IsNullOrWhiteSpace(currentUser.Id))
                return IdentityErrors.NotAuthenticated();

            var userId = UserId.From(Guid.Parse(currentUser.Id));
            var tenantId = TenantId.From(request.TenantId);

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null || !user.IsActive)
                return IdentityErrors.NotAuthenticated();

            var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant is null || !tenant.IsActive)
                return IdentityErrors.TenantNotFound(request.TenantId);

            var membership = await membershipRepository.GetByUserAndTenantAsync(
                userId, tenantId, cancellationToken);

            if (membership is null || !membership.IsActive)
                return IdentityErrors.TenantMembershipNotFound(userId.Value, tenantId.Value);

            var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
            if (role is null)
                return IdentityErrors.RoleNotFound(membership.RoleId.Value);

            var permissions = await permissionResolver.ResolvePermissionsAsync(
                role.Id, role.Name, tenantId, cancellationToken);

            // SaaS-01: resolve capabilities from tenant license; Enterprise fallback for unlicensed tenants.
            var license = await licenseRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken);
            var capabilities = license?.GetCapabilities() ?? TenantCapabilities.ForPlan(TenantPlan.Enterprise);

            var accessToken = jwtTokenGenerator.GenerateAccessToken(
                user, tenantId, [membership.RoleId], permissions, capabilities);

            return new Response(
                accessToken,
                jwtTokenGenerator.AccessTokenLifetimeSeconds,
                tenant.Id.Value,
                tenant.Name,
                role.Name,
                permissions);
        }
    }
}
