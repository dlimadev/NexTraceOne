using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.RevokePlatformApiToken;

/// <summary>
/// Feature: RevokePlatformApiToken — revoga um token de acesso de plataforma.
/// Wave D.4 — Agent-to-Agent Protocol.
/// </summary>
public static class RevokePlatformApiToken
{
    public sealed record Command(Guid TokenId, string Reason) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TokenId).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        }
    }

    public sealed class Handler(
        IPlatformApiTokenRepository repository,
        IIdentityAccessUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var token = await repository.GetByIdAsync(
                PlatformApiTokenId.From(request.TokenId), cancellationToken);
            if (token is null)
                return Error.NotFound("platform_api_token.not_found", $"Token {request.TokenId} not found.");

            var now = clock.UtcNow;
            if (!token.IsActive(now))
                return Error.Business("platform_api_token.already_inactive", "Token is already revoked or expired.");

            token.Revoke(request.Reason, now);
            await repository.UpdateAsync(token, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(request.TokenId, now));
        }
    }

    public sealed record Response(Guid TokenId, DateTimeOffset RevokedAt);
}
