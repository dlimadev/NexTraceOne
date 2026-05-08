using System.Security.Cryptography;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Features.RequestAccountActivation;

/// <summary>
/// Feature: RequestAccountActivation — gera token de activação e envia email.
/// Invocado após criação de utilizador pelo admin.
/// </summary>
public static class RequestAccountActivation
{
    public sealed record Command(string Email) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        }
    }

    public sealed record Response(bool Sent);

    internal sealed class Handler(
        IUserRepository userRepository,
        IAccountActivationTokenRepository tokenRepository,
        IIdentityNotifier notifier,
        IIdentityAccessUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var email = Email.Create(request.Email);
            var user = await userRepository.GetByEmailAsync(email, cancellationToken);
            if (user is null)
                return new Response(false); // prevent email enumeration

            var now = clock.UtcNow;

            // invalidate any existing token for this user
            await tokenRepository.DeleteByUserIdAsync(user.Id, cancellationToken);

            var (rawToken, tokenHash) = GenerateToken();
            var token = AccountActivationToken.Create(user.Id, tokenHash, now);
            tokenRepository.Add(token);

            await unitOfWork.CommitAsync(cancellationToken);

            await notifier.SendAccountActivationAsync(
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
