using System.Security.Cryptography;
using System.Text;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.CreatePlatformApiToken;

/// <summary>
/// Feature: CreatePlatformApiToken — cria um token de acesso de plataforma para agentes autónomos.
/// O valor real do token é gerado aqui e apresentado apenas uma vez.
/// Apenas o hash SHA-256 é persistido. Wave D.4 — Agent-to-Agent Protocol.
/// </summary>
public static class CreatePlatformApiToken
{
    public sealed record Command(
        string Name,
        PlatformApiTokenScope Scope,
        int? ExpiresInDays = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ExpiresInDays).InclusiveBetween(1, 3650).When(x => x.ExpiresInDays.HasValue);
        }
    }

    public sealed class Handler(
        IPlatformApiTokenRepository repository,
        IIdentityAccessUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // ── Gerar token seguro ──────────────────────────────────────────
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
            var tokenPrefix = rawToken[..8];

            var now = clock.UtcNow;
            var expiresAt = request.ExpiresInDays.HasValue ? now.AddDays(request.ExpiresInDays.Value) : (DateTimeOffset?)null;

            var token = PlatformApiToken.Create(
                tenantId: currentTenant.Id,
                name: request.Name,
                tokenHash: tokenHash,
                tokenPrefix: tokenPrefix,
                scope: request.Scope,
                createdBy: currentUser.Id,
                createdAt: now,
                expiresAt: expiresAt);

            await repository.AddAsync(token, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                TokenId: token.Id.Value,
                Name: token.Name,
                RawToken: rawToken,   // presented ONCE — never stored
                TokenPrefix: tokenPrefix,
                Scope: token.Scope,
                ExpiresAt: expiresAt,
                CreatedAt: now));
        }
    }

    public sealed record Response(
        Guid TokenId,
        string Name,
        string RawToken,
        string TokenPrefix,
        PlatformApiTokenScope Scope,
        DateTimeOffset? ExpiresAt,
        DateTimeOffset CreatedAt);
}
