using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.CreateRole;

/// <summary>
/// Feature: CreateRole — cria um papel customizado editável.
/// Papéis customizados permitem que cada tenant defina roles específicos
/// conforme suas necessidades de governança.
/// </summary>
public static class CreateRole
{
    /// <summary>Comando para criar um novo papel customizado.</summary>
    public sealed record Command(string Name, string Description) : ICommand<RoleResponse>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que cria o papel customizado no repositório.</summary>
    public sealed class Handler(
        IRoleRepository roleRepository,
        IIdentityAccessUnitOfWork unitOfWork) : ICommandHandler<Command, RoleResponse>
    {
        public async Task<Result<RoleResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await roleRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existing is not null)
                return Error.Conflict("Role.AlreadyExists", $"A role with name '{request.Name}' already exists.");

            var role = Role.CreateCustom(request.Name, request.Description);

            await roleRepository.AddAsync(role, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new RoleResponse(role.Id.Value, role.Name, role.Description, role.IsSystem);
        }
    }

    /// <summary>Resposta com dados do papel criado.</summary>
    public sealed record RoleResponse(Guid Id, string Name, string Description, bool IsSystem);
}
