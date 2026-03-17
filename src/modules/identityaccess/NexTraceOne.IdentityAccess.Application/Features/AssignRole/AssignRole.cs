using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.AssignRole;

/// <summary>
/// Feature: AssignRole — cria ou atualiza o papel de um usuário dentro de um tenant.
///
/// Regras de negócio:
/// - Se não existir vínculo, cria um novo TenantMembership com o papel solicitado.
/// - Se já existir vínculo, atualiza o papel e reativa o vínculo caso esteja inativo.
/// - Todo role change gera um SecurityEvent auditável (exigência SOX/LGPD).
/// - O evento registra o papel anterior (quando aplicável) e o novo papel para rastreabilidade.
/// </summary>
public static class AssignRole
{
    /// <summary>Comando de atribuição de papel por tenant.</summary>
    public sealed record Command(Guid UserId, Guid TenantId, Guid RoleId) : ICommand;

    /// <summary>Valida a entrada de atribuição de papel.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.RoleId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que cria ou atualiza o vínculo do usuário com o tenant.
    /// Gera SecurityEvent para trilha de auditoria obrigatória de mudança de papel.
    /// </summary>
    public sealed class Handler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITenantMembershipRepository membershipRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var user = await userRepository.GetByIdAsync(UserId.From(request.UserId), cancellationToken);
            if (user is null)
            {
                return IdentityErrors.UserNotFound(request.UserId);
            }

            var role = await roleRepository.GetByIdAsync(RoleId.From(request.RoleId), cancellationToken);
            if (role is null)
            {
                return IdentityErrors.RoleNotFound(request.RoleId);
            }

            var membership = await membershipRepository.GetByUserAndTenantAsync(
                user.Id,
                TenantId.From(request.TenantId),
                cancellationToken);

            RoleId? previousRoleId = null;

            if (membership is null)
            {
                membershipRepository.Add(TenantMembership.Create(
                    user.Id,
                    TenantId.From(request.TenantId),
                    role.Id,
                    dateTimeProvider.UtcNow));
            }
            else
            {
                // Registra o papel anterior antes da mudança para enriquecer o evento de auditoria
                previousRoleId = membership.RoleId;
                membership.ChangeRole(role.Id);
                membership.Activate();
            }

            RecordRoleChangeEvent(request, user.Id, role, previousRoleId);

            return Unit.Value;
        }

        /// <summary>
        /// Registra evento de segurança para mudança de papel.
        /// Evento é classificado como risco moderado (score 20) por ser uma mudança
        /// de privilégio que pode elevar ou restringir o acesso do usuário.
        /// </summary>
        private void RecordRoleChangeEvent(
            Command request,
            UserId userId,
            Role newRole,
            RoleId? previousRoleId)
        {
            var tenantId = TenantId.From(request.TenantId);
            var isNewAssignment = previousRoleId is null;
            var eventType = SecurityEventType.RoleAssigned;

            var description = isNewAssignment
                ? $"Role '{newRole.Name}' assigned to user '{userId.Value}' in tenant '{request.TenantId}'."
                : $"Role changed from '{previousRoleId!.Value}' to '{newRole.Name}' for user '{userId.Value}' in tenant '{request.TenantId}'.";

            var metadataJson = isNewAssignment
                ? $"{{\"newRole\":\"{newRole.Name}\",\"tenantId\":\"{request.TenantId}\"}}"
                : $"{{\"previousRoleId\":\"{previousRoleId!.Value}\",\"newRole\":\"{newRole.Name}\",\"tenantId\":\"{request.TenantId}\"}}";

            var securityEvent = SecurityEvent.Create(
                tenantId,
                userId,
                sessionId: null,
                eventType,
                description,
                riskScore: 20,
                ipAddress: null,
                userAgent: null,
                metadataJson,
                dateTimeProvider.UtcNow);

            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);
        }
    }
}
