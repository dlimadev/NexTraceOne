using Ardalis.GuardClauses;
using FluentValidation;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.DeleteRole;

/// <summary>
/// Feature: DeleteRole — remove um papel customizado.
/// Papéis de sistema (IsSystem=true) não podem ser removidos.
/// </summary>
public static class DeleteRole
{
    /// <summary>Comando para remover um papel customizado.</summary>
    public sealed record Command(Guid RoleId) : ICommand<Unit>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RoleId).NotEmpty();
        }
    }

    /// <summary>Handler que remove o papel do repositório.</summary>
    public sealed class Handler(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Unit>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var role = await roleRepository.GetByIdAsync(
                Domain.Entities.RoleId.From(request.RoleId), cancellationToken);

            if (role is null)
                return Error.NotFound("Role.NotFound", $"Role with id '{request.RoleId}' not found.");

            if (role.IsSystem)
                return Error.Validation("Role.SystemRoleProtected", "System roles cannot be deleted.");

            await roleRepository.RemoveAsync(role, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
