using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Features.ResetPassword;

/// <summary>
/// Feature: ResetPassword — valida token de reset e actualiza a password do utilizador.
/// </summary>
public static class ResetPassword
{
    public sealed record Command(string Token, string NewPassword) : ICommand<Response>, IPublicRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        }
    }

    public sealed record Response(bool Success);

    internal sealed class Handler(
        IPasswordResetTokenRepository tokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IIdentityAccessUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var tokenHash = HashToken(request.Token);

            var token = await tokenRepository.FindByHashAsync(tokenHash, cancellationToken);

            if (token is null || !token.IsValid(now))
                return Error.Validation("password.reset.token_invalid",
                    "The password reset token is invalid or has expired.");

            var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
            if (user is null)
                return Error.Validation("password.reset.token_invalid",
                    "The password reset token is invalid or has expired.");

            var newHash = HashedPassword.FromHash(passwordHasher.Hash(request.NewPassword));
            user.SetPassword(newHash);
            token.MarkUsed(now);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(true);
        }

        private static string HashToken(string raw) =>
            Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
    }
}
