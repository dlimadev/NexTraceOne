using System.Security.Cryptography;

using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Features.SignUpTenant;

/// <summary>
/// Feature: SignUpTenant — cadastro self-service público de um novo tenant.
/// Cria tenant + licença Trial (14 dias) + usuário administrador + token de
/// ativação enviado por e-mail. A senha é definida pelo próprio usuário no
/// fluxo de ativação (<see cref="ActivateAccount.ActivateAccount"/>).
/// </summary>
public static class SignUpTenant
{
    /// <summary>Host units incluídos no trial self-service.</summary>
    private const int TrialIncludedHostUnits = 5;

    /// <summary>Comando público de cadastro de tenant + administrador.</summary>
    public sealed record Command(
        string CompanyName,
        string Slug,
        string Email,
        string FirstName,
        string LastName) : ICommand<Response>, IPublicRequest;

    /// <summary>Valida a entrada do cadastro self-service.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(128)
                .Matches("^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$")
                .WithMessage("Slug must be lowercase alphanumeric with optional hyphens.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Resposta do cadastro com o tenant criado.</summary>
    public sealed record Response(Guid TenantId, string Slug, bool ActivationEmailSent);

    /// <summary>
    /// Handler que compõe o provisionamento completo do tenant self-service.
    /// Não usa Send aninhado: comandos internos não são IPublicRequest e seriam
    /// rejeitados pelo TenantIsolationBehavior neste contexto sem tenant.
    /// </summary>
    internal sealed class Handler(
        ITenantRepository tenantRepository,
        ITenantLicenseRepository licenseRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITenantMembershipRepository membershipRepository,
        IAccountActivationTokenRepository tokenRepository,
        IIdentityNotifier notifier,
        IPasswordHasher passwordHasher,
        IIdentityAccessUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var now = clock.UtcNow;

            var email = Email.Create(request.Email);
            if (await userRepository.ExistsAsync(email, cancellationToken))
                return Error.Conflict("signup.emailInUse", "An account with this email already exists.");

            var slug = request.Slug.ToLowerInvariant();
            if (await tenantRepository.SlugExistsAsync(slug, cancellationToken))
                return Error.Conflict("signup.slugInUse", $"Workspace '{slug}' is already in use.");

            var adminRole = await roleRepository.GetByNameAsync(Role.PlatformAdmin, cancellationToken);
            if (adminRole is null)
                return Error.Business("signup.rolesNotSeeded", "Default roles are not provisioned yet.");

            var tenant = Tenant.Create(request.CompanyName, slug, now);
            tenantRepository.Add(tenant);

            var license = TenantLicense.Provision(
                tenant.Id.Value,
                TenantPlan.Trial,
                TrialIncludedHostUnits,
                now,
                now.AddDays(14),
                now);
            licenseRepository.Add(license);

            // Usuário local com senha aleatória não utilizável — a senha real é
            // definida pelo próprio usuário no fluxo de ativação por e-mail.
            var placeholderPassword = HashedPassword.FromHash(
                passwordHasher.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
            var user = User.CreateLocal(
                email,
                FullName.Create(request.FirstName, request.LastName),
                placeholderPassword,
                now);
            userRepository.Add(user);

            membershipRepository.Add(TenantMembership.Create(user.Id, tenant.Id, adminRole.Id, now));

            var (rawToken, tokenHash) = GenerateToken();
            tokenRepository.Add(AccountActivationToken.Create(user.Id, tokenHash, now));

            await unitOfWork.CommitAsync(cancellationToken);

            await notifier.SendAccountActivationAsync(
                user.Email.Value,
                user.FullName.FirstName,
                rawToken,
                cancellationToken);

            return new Response(tenant.Id.Value, tenant.Slug, true);
        }

        private static (string Raw, string Hash) GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var raw = Convert.ToBase64String(bytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('='); // URL-safe
            var hash = Convert.ToBase64String(
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
            return (raw, hash);
        }
    }
}
