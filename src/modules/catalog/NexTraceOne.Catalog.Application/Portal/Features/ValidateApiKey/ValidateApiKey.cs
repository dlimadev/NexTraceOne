using System.Security.Cryptography;
using System.Text;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.ValidateApiKey;

/// <summary>
/// Feature: ValidateApiKey — valida uma API Key raw e regista uso.
/// Este é um Command pois muta estado (RecordUsage).
/// </summary>
public static class ValidateApiKey
{
    public sealed record Command(string RawKey) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RawKey).NotEmpty();
        }
    }

    public sealed class Handler(
        IApiKeyRepository repository,
        IPortalUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(request.RawKey));
            var keyHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var apiKey = await repository.GetByHashAsync(keyHash, cancellationToken);

            if (apiKey is null)
                return Error.NotFound("API_KEY_INVALID", "API key is invalid.");

            if (!apiKey.IsActive)
                return DeveloperPortalErrors.ApiKeyAlreadyRevoked(apiKey.Id.Value.ToString());

            if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < clock.UtcNow)
                return DeveloperPortalErrors.ApiKeyExpired(apiKey.Id.Value.ToString());

            apiKey.RecordUsage(clock.UtcNow);
            repository.Update(apiKey);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(apiKey.Id.Value, apiKey.OwnerId, apiKey.ApiAssetId, true);
        }
    }

    public sealed record Response(Guid ApiKeyId, Guid OwnerId, Guid? ApiAssetId, bool IsValid);
}
