using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.RevokeApiKey;

/// <summary>Feature: RevokeApiKey — revoga uma API Key, impedindo uso futuro.</summary>
public static class RevokeApiKey
{
    public sealed record Command(Guid ApiKeyId, Guid RequesterId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiKeyId).NotEmpty();
            RuleFor(x => x.RequesterId).NotEmpty();
        }
    }

    public sealed class Handler(
        IApiKeyRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var apiKey = await repository.GetByIdAsync(ApiKeyId.From(request.ApiKeyId), cancellationToken);

            if (apiKey is null)
                return DeveloperPortalErrors.ApiKeyNotFound(request.ApiKeyId.ToString());

            if (apiKey.OwnerId != request.RequesterId)
                return Error.Forbidden("API_KEY_ACCESS_DENIED", "You do not have permission to revoke this API key.");

            var revokeResult = apiKey.Revoke(request.RequesterId.ToString(), clock.UtcNow);
            if (revokeResult.IsFailure)
                return revokeResult.Error;

            repository.Update(apiKey);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(apiKey.Id.Value, apiKey.RevokedAt!.Value);
        }
    }

    public sealed record Response(Guid ApiKeyId, DateTimeOffset RevokedAt);
}
