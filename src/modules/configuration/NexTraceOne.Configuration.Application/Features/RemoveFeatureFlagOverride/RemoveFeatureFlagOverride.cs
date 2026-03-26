
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.RemoveFeatureFlagOverride;

/// <summary>
/// Feature: RemoveFeatureFlagOverride — removes a feature flag scope override.
/// After removal the scope inherits from parent scopes or falls back to the default.
/// </summary>
public static class RemoveFeatureFlagOverride
{
    /// <summary>Command to remove a feature flag override for a specific scope.</summary>
    public sealed record Command(
        string Key,
        ConfigurationScope Scope,
        string? ScopeReferenceId,
        string? ChangeReason) : ICommand<bool>;

    /// <summary>Handler that deactivates the entry or removes it from persistence.</summary>
    public sealed class Handler(
        IFeatureFlagRepository repository,
        IConfigurationCacheService cacheService,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, bool>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entry = await repository.GetEntryByKeyAndScopeAsync(
                request.Key,
                request.Scope,
                request.ScopeReferenceId,
                cancellationToken);

            if (entry is null)
                return Error.NotFound(
                    "FEATURE_FLAG_ENTRY_NOT_FOUND",
                    "No feature flag override found for key '{0}' in scope '{1}'.",
                    request.Key,
                    request.Scope.ToString());

            await repository.DeleteEntryAsync(entry, cancellationToken);

            try
            {
                await unitOfWork.CommitAsync(cancellationToken);
            }
            catch (NexTraceOne.BuildingBlocks.Application.Abstractions.ConcurrencyException)
            {
                return Error.Conflict(
                    "FEATURE_FLAG_CONCURRENCY_CONFLICT",
                    "The feature flag entry '{0}' was modified by another process. Please reload and try again.",
                    request.Key);
            }

            await cacheService.InvalidateAsync(request.Key, request.Scope, cancellationToken);

            return true;
        }
    }
}
