using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.UpdateRole;

/// <summary>
/// Feature: UpdateRole — atualiza nome e descrição de um papel customizado.
/// Papéis de sistema (IsSystem=true) não podem ser editados.
/// </summary>
public static class UpdateRole
{
    /// <summary>Comando para atualizar um papel existente.</summary>
    public sealed record Command(Guid RoleId, string Name, string Description) : ICommand<RoleResponse>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RoleId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que atualiza o papel customizado.</summary>
    public sealed class Handler(
        IRoleRepository roleRepository,
        IIdentityAccessUnitOfWork unitOfWork) : ICommandHandler<Command, RoleResponse>
    {
        public async Task<Result<RoleResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var role = await roleRepository.GetByIdAsync(
                Domain.Entities.RoleId.From(request.RoleId), cancellationToken);

            if (role is null)
                return Error.NotFound("Role.NotFound", $"Role with id '{request.RoleId}' not found.");

            if (role.IsSystem)
                return Error.Validation("Role.SystemRoleReadOnly", "System roles cannot be modified.");

            // Check name uniqueness (excluding current role)
            var existingByName = await roleRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existingByName is not null && existingByName.Id != role.Id)
                return Error.Conflict("Role.NameAlreadyExists", $"A role with name '{request.Name}' already exists.");

            role.Update(request.Name, request.Description);
            await unitOfWork.CommitAsync(cancellationToken);

            return new RoleResponse(role.Id.Value, role.Name, role.Description, role.IsSystem);
        }
    }

    /// <summary>Resposta com dados do papel atualizado.</summary>
    public sealed record RoleResponse(Guid Id, string Name, string Description, bool IsSystem);
}
