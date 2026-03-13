using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using LocalLoginFeature = NexTraceOne.Identity.Application.Features.LocalLogin.LocalLogin;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Application.Features.FederatedLogin;

/// <summary>
/// Feature: FederatedLogin — autentica um usuário federado com provider externo.
///
/// Delegação de responsabilidades:
/// - Criação de sessão → <see cref="ILoginSessionCreator"/> (SRP/DIP).
/// - Resolução de membership → <see cref="ILoginResponseBuilder"/> (DRY/DIP).
/// - Construção de resposta → <see cref="ILoginResponseBuilder"/> (DRY/DIP).
///
/// Refatoração: eliminada duplicação de criação inline de sessão e refresh token,
/// agora delegada para ILoginSessionCreator como os demais handlers de autenticação.
/// Reduzido de 7 para 5 dependências diretas via serviços injetáveis.
/// </summary>
public static class FederatedLogin
{
    /// <summary>Comando de autenticação federada.</summary>
    public sealed record Command(
        string Provider,
        string ExternalId,
        string Email,
        string Name,
        string? IpAddress = null,
        string? UserAgent = null) : ICommand<LocalLoginFeature.LoginResponse>, IPublicRequest;

    /// <summary>Valida a entrada do login federado.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Provider).NotEmpty();
            RuleFor(x => x.ExternalId).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(201);
        }
    }

    /// <summary>
    /// Handler que autentica ou provisiona um usuário federado.
    ///
    /// Orquestra o fluxo de autenticação federada delegando responsabilidades
    /// específicas para serviços injetados via DI:
    /// - ILoginSessionCreator para criação de sessão (DIP).
    /// - ILoginResponseBuilder para resolução de membership e construção de resposta (DIP).
    /// </summary>
    public sealed class Handler(
        IUserRepository userRepository,
        ITenantMembershipRepository membershipRepository,
        IRoleRepository roleRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentTenant currentTenant,
        ILoginSessionCreator sessionCreator,
        ILoginResponseBuilder responseBuilder) : ICommandHandler<Command, LocalLoginFeature.LoginResponse>
    {
        public async Task<Result<LocalLoginFeature.LoginResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var user = await userRepository.GetByFederatedIdentityAsync(
                request.Provider,
                request.ExternalId,
                cancellationToken);

            user ??= await userRepository.GetByEmailAsync(Email.Create(request.Email), cancellationToken);

            if (user is null)
            {
                user = User.CreateFederated(
                    Email.Create(request.Email),
                    FullName.FromDisplayName(request.Name),
                    request.Provider,
                    request.ExternalId);
                userRepository.Add(user);
            }
            else
            {
                user.LinkFederatedIdentity(request.Provider, request.ExternalId);
            }

            var membership = await responseBuilder.ResolveMembershipAsync(user.Id, cancellationToken);

            if (membership is null && currentTenant.Id != Guid.Empty)
            {
                var viewerRole = await roleRepository.GetByNameAsync(Role.Viewer, cancellationToken);
                if (viewerRole is null)
                {
                    return IdentityErrors.RoleNotFound(Guid.Empty);
                }

                membership = TenantMembership.Create(
                    user.Id,
                    TenantId.From(currentTenant.Id),
                    viewerRole.Id,
                    dateTimeProvider.UtcNow);

                membershipRepository.Add(membership);
            }

            if (membership is null)
            {
                return IdentityErrors.TenantMembershipNotFound(user.Id.Value, currentTenant.Id);
            }

            var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
            if (role is null)
            {
                return IdentityErrors.RoleNotFound(membership.RoleId.Value);
            }

            user.RegisterSuccessfulLogin(dateTimeProvider.UtcNow);

            // Delega criação de sessão para ILoginSessionCreator (SRP/DIP)
            // Elimina duplicação: anteriormente criava Session e RefreshToken inline
            var (_, refreshToken) = sessionCreator.CreateSession(
                user.Id, request.IpAddress, request.UserAgent);

            return responseBuilder.CreateLoginResponse(user, membership, role, refreshToken);
        }
    }
}
