using Ardalis.GuardClauses;

using FluentValidation;
using System.Text.Json;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.CreateDelegation;

/// <summary>
/// Feature: CreateDelegation — cria uma delegação formal de permissões.
///
/// Regras de segurança:
/// - Delegante não pode delegar permissões que não possui.
/// - Permissões de PlatformAdmin não podem ser delegadas.
/// - Auto-delegação não é permitida.
/// - Toda ação do delegatário é registrada como "acting on behalf of [delegante]".
/// </summary>
public static class CreateDelegation
{
    /// <summary>Comando para criação de delegação formal.</summary>
    public sealed record Command(
        Guid DelegateeId,
        IReadOnlyList<string> Permissions,
        string Reason,
        DateTimeOffset ValidFrom,
        DateTimeOffset ValidUntil) : ICommand<Response>;

    /// <summary>Valida a entrada da criação de delegação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DelegateeId).NotEmpty();
            RuleFor(x => x.Permissions).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(2000);
            RuleFor(x => x.ValidUntil).GreaterThan(x => x.ValidFrom);
        }
    }

    /// <summary>Handler que cria a delegação formal.</summary>
    public sealed class Handler(
        IDelegationRepository delegationRepository,
        ITenantMembershipRepository membershipRepository,
        IRoleRepository roleRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        /// <summary>
        /// Permissões exclusivas de PlatformAdmin que não podem ser delegadas.
        /// Garante segregação de funções e previne escalação de privilégio via delegação.
        /// </summary>
        private static readonly HashSet<string> NonDelegablePermissions =
        [
            "identity:users:write",
            "identity:roles:assign",
            "identity:sessions:revoke",
            "platform:settings:write"
        ];

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (string.IsNullOrWhiteSpace(currentUser.Id))
                return IdentityErrors.NotAuthenticated();

            var grantorId = UserId.From(Guid.Parse(currentUser.Id));
            var delegateeId = UserId.From(request.DelegateeId);
            var tenantId = TenantId.From(currentTenant.Id);
            var now = dateTimeProvider.UtcNow;

            if (grantorId == delegateeId)
            {
                var deniedEvent = SecurityEvent.Create(
                    tenantId,
                    grantorId,
                    sessionId: null,
                    SecurityEventType.DelegationToSelfDenied,
                    $"Delegation to self denied for user '{grantorId.Value}'.",
                    riskScore: 55,
                    ipAddress: null,
                    userAgent: null,
                    metadataJson: JsonSerializer.Serialize(new
                    {
                        grantorId = grantorId.Value,
                        delegateeId = delegateeId.Value,
                        reason = "self-delegation-not-allowed"
                    }),
                    now);

                securityEventRepository.Add(deniedEvent);
                securityEventTracker.Track(deniedEvent);
                return IdentityErrors.DelegationToSelfNotAllowed();
            }

            // Verifica se alguma permissão é do escopo de administração de sistema
            if (request.Permissions.Any(p => NonDelegablePermissions.Contains(p)))
                return IdentityErrors.DelegationOfSystemAdminNotAllowed();

            // Verifica se o delegante possui as permissões que está tentando delegar
            var membership = await membershipRepository.GetByUserAndTenantAsync(
                grantorId, tenantId, cancellationToken);

            if (membership is null)
                return IdentityErrors.TenantMembershipNotFound(grantorId.Value, tenantId.Value);

            var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
            if (role is null)
                return IdentityErrors.RoleNotFound(membership.RoleId.Value);

            var grantorPermissions = Role.GetPermissionsForRole(role.Name);
            var grantorPermissionSet = new HashSet<string>(grantorPermissions);

            if (!request.Permissions.All(p => grantorPermissionSet.Contains(p)))
                return IdentityErrors.DelegationScopeExceedsGrantor();

            var delegation = Delegation.Create(
                grantorId,
                delegateeId,
                tenantId,
                request.Permissions,
                request.Reason,
                request.ValidFrom,
                request.ValidUntil,
                now);

            delegationRepository.Add(delegation);

            var securityEvent = SecurityEvent.Create(
                tenantId,
                grantorId,
                sessionId: null,
                SecurityEventType.DelegationCreated,
                $"Delegation created from {grantorId.Value} to {delegateeId.Value} for {request.Permissions.Count} permissions.",
                riskScore: 50,
                ipAddress: null,
                userAgent: null,
                metadataJson: JsonSerializer.Serialize(new
                {
                    delegationId = delegation.Id.Value,
                    grantorId = grantorId.Value,
                    delegateeId = delegateeId.Value,
                    permissionCount = request.Permissions.Count,
                    validFrom = delegation.ValidFrom,
                    validUntil = delegation.ValidUntil
                }),
                now);

            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);

            return new Response(delegation.Id.Value, delegation.ValidFrom, delegation.ValidUntil);
        }
    }

    /// <summary>Resposta da criação de delegação.</summary>
    public sealed record Response(
        Guid DelegationId,
        DateTimeOffset ValidFrom,
        DateTimeOffset ValidUntil);
}
