using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Features.ActivateAccount;

/// <summary>
/// Feature: ActivateAccount — valida token de activação, activa conta e define password inicial.
/// </summary>
public static class ActivateAccount
{
    public sealed record Command(string Token, string Password) : ICommand<Response>, IPublicRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        }
    }

    public sealed record Response(bool Activated);

    internal sealed class Handler(
        IAccountActivationTokenRepository tokenRepository,
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
                return Error.Validation("account.activation.token_invalid",
                    "The activation token is invalid or has expired.");

            var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
            if (user is null)
                return Error.Validation("account.activation.token_invalid",
                    "The activation token is invalid or has expired.");

            var hashedPassword = HashedPassword.FromHash(passwordHasher.Hash(request.Password));
            user.SetPassword(hashedPassword);
            user.Activate();
            token.MarkUsed(now);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(true);
        }

        private static string HashToken(string raw) =>
            Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
    }
}
