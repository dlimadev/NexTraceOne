using System.Security.Cryptography;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Features.ForgotPassword;

/// <summary>
/// Feature: ForgotPassword — inicia o fluxo de recuperação de password.
/// Retorna sempre sucesso para prevenir enumeração de emails.
/// Gera token de reset e envia email quando o utilizador existe.
/// </summary>
public static class ForgotPassword
{
    public sealed record Command(string Email) : ICommand<Response>, IPublicRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        }
    }

    public sealed record Response(bool Accepted);

    internal sealed class Handler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IIdentityNotifier notifier,
        IIdentityAccessUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var email = Email.Create(request.Email);
            var user = await userRepository.GetByEmailAsync(email, cancellationToken);

            // always return accepted to prevent email enumeration
            if (user is null)
                return new Response(true);

            var now = clock.UtcNow;

            // invalidate any existing reset token for this user
            await tokenRepository.DeleteByUserIdAsync(user.Id, cancellationToken);

            var (rawToken, tokenHash) = GenerateToken();
            var token = PasswordResetToken.Create(user.Id, tokenHash, now);
            tokenRepository.Add(token);

            await unitOfWork.CommitAsync(cancellationToken);

            await notifier.SendPasswordResetAsync(
                user.Email.Value,
                user.FullName.FirstName,
                rawToken,
                cancellationToken);

            return new Response(true);
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
