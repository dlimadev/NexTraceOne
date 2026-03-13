using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Application.Features.CreateUser;

/// <summary>
/// Feature: CreateUser — cria um usuário local ou federado e o vincula a um tenant.
/// </summary>
public static class CreateUser
{
    /// <summary>Comando de criação de usuário.</summary>
    public sealed record Command(
        string Email,
        string FirstName,
        string LastName,
        string? Password,
        Guid TenantId,
        Guid RoleId,
        string? Provider = null,
        string? ExternalId = null) : ICommand<Guid>;

    /// <summary>Valida a entrada de criação de usuário.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.RoleId).NotEmpty();
        }
    }

    /// <summary>Handler que cria o usuário e o vínculo inicial no tenant.</summary>
    public sealed class Handler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITenantMembershipRepository membershipRepository,
        IDateTimeProvider dateTimeProvider,
        IPasswordHasher passwordHasher) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var email = Email.Create(request.Email);
            if (await userRepository.ExistsAsync(email, cancellationToken))
            {
                return IdentityErrors.EmailAlreadyExists(email.Value);
            }

            var role = await roleRepository.GetByIdAsync(RoleId.From(request.RoleId), cancellationToken);
            if (role is null)
            {
                return IdentityErrors.RoleNotFound(request.RoleId);
            }

            User user = string.IsNullOrWhiteSpace(request.Password)
                ? User.CreateFederated(
                    email,
                    FullName.Create(request.FirstName, request.LastName),
                    request.Provider ?? "federated",
                    request.ExternalId ?? email.Value)
                : User.CreateLocal(
                    email,
                    FullName.Create(request.FirstName, request.LastName),
                    HashedPassword.FromHash(passwordHasher.Hash(request.Password)));

            userRepository.Add(user);

            membershipRepository.Add(TenantMembership.Create(
                user.Id,
                TenantId.From(request.TenantId),
                role.Id,
                dateTimeProvider.UtcNow));

            return user.Id.Value;
        }
    }
}
