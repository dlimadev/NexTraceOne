using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.AssignRole;

/// <summary>
/// Feature: AssignRole — cria ou atualiza o papel de um usuário dentro de um tenant.
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

    /// <summary>Handler que cria ou atualiza o vínculo do usuário com o tenant.</summary>
    public sealed class Handler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITenantMembershipRepository membershipRepository,
        NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
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
                membership.ChangeRole(role.Id);
                membership.Activate();
            }

            return Unit.Value;
        }
    }
}
