using System.Security.Cryptography;
using System.Text;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Application.Portal.Features.CreateApiKey;

/// <summary>
/// Feature: CreateApiKey — gera e armazena nova API Key para acesso programático ao portal.
/// O valor raw é retornado UMA única vez, nunca sendo armazenado.
/// </summary>
public static class CreateApiKey
{
    public sealed record Command(
        Guid OwnerId,
        Guid? ApiAssetId,
        string Name,
        string? Description,
        DateTimeOffset? ExpiresAt) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OwnerId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
            RuleFor(x => x.ExpiresAt)
                .Must(expiresAt => expiresAt == null || expiresAt > DateTimeOffset.UtcNow)
                .When(x => x.ExpiresAt.HasValue)
                .WithMessage("ExpiresAt must be a future date.");
        }
    }

    public sealed class Handler(
        IApiKeyRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        private const int KeyPrefixLength = 8;

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Generates a 32-byte (256-bit) cryptographically random key encoded as a 64-character hex string.
            var rawBytes = RandomNumberGenerator.GetBytes(32);
            var rawKey = Convert.ToHexString(rawBytes).ToLowerInvariant();

            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
            var keyHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var keyPrefix = rawKey[..KeyPrefixLength] + "...";

            var createResult = ApiKey.Create(
                request.OwnerId,
                request.ApiAssetId,
                request.Name,
                keyHash,
                keyPrefix,
                request.Description,
                request.ExpiresAt,
                clock.UtcNow);

            if (createResult.IsFailure)
                return createResult.Error;

            var apiKey = createResult.Value;
            repository.Add(apiKey);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                apiKey.Id.Value,
                rawKey,
                apiKey.KeyPrefix,
                apiKey.Name,
                apiKey.CreatedAt,
                apiKey.ExpiresAt);
        }
    }

    public sealed record Response(
        Guid ApiKeyId,
        string RawKey,
        string KeyPrefix,
        string Name,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ExpiresAt);
}
