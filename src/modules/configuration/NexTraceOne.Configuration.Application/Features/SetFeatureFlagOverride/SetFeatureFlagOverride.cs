using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.SetFeatureFlagOverride;

/// <summary>
/// Feature: SetFeatureFlagOverride — creates or updates a feature flag value override for a specific scope.
/// Validates against the definition (allowed scopes, editability, active state).
/// </summary>
public static class SetFeatureFlagOverride
{
    /// <summary>Command to set a feature flag value for a specific scope.</summary>
    public sealed record Command(
        string Key,
        ConfigurationScope Scope,
        string? ScopeReferenceId,
        bool IsEnabled,
        string? ChangeReason) : ICommand<FeatureFlagEntryDto>;

    /// <summary>Validates the command parameters.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Key).NotEmpty().MaximumLength(256);
            RuleFor(x => x.ScopeReferenceId).MaximumLength(256);
            RuleFor(x => x.ChangeReason).MaximumLength(500);
        }
    }

    /// <summary>Handler that validates, persists the override, and invalidates cache.</summary>
    public sealed class Handler(
        IFeatureFlagRepository repository,
        IConfigurationCacheService cacheService,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, FeatureFlagEntryDto>
    {
        public async Task<Result<FeatureFlagEntryDto>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var definition = await repository.GetDefinitionByKeyAsync(request.Key, cancellationToken);
            if (definition is null)
                return Error.NotFound(
                    "FEATURE_FLAG_NOT_FOUND",
                    "Feature flag definition for key '{0}' not found.",
                    request.Key);

            if (!definition.IsActive)
                return Error.Business(
                    "FEATURE_FLAG_INACTIVE",
                    "Feature flag '{0}' is not active.",
                    request.Key);

            if (!definition.IsEditable)
                return Error.Business(
                    "FEATURE_FLAG_NOT_EDITABLE",
                    "Feature flag '{0}' is not editable.",
                    request.Key);

            if (!definition.AllowedScopes.Contains(request.Scope))
                return Error.Validation(
                    "FEATURE_FLAG_SCOPE_NOT_ALLOWED",
                    "Scope '{0}' is not allowed for feature flag '{1}'. Allowed scopes: {2}.",
                    request.Scope.ToString(),
                    request.Key,
                    string.Join(", ", definition.AllowedScopes));

            var userId = currentUser.Id;

            var existingEntry = await repository.GetEntryByKeyAndScopeAsync(
                request.Key,
                request.Scope,
                request.ScopeReferenceId,
                cancellationToken);

            FeatureFlagEntry entry;

            if (existingEntry is not null)
            {
                existingEntry.UpdateValue(request.IsEnabled, userId, request.ChangeReason);
                if (!existingEntry.IsActive)
                    existingEntry.Activate(userId, request.ChangeReason);
                await repository.UpdateEntryAsync(existingEntry, cancellationToken);
                entry = existingEntry;
            }
            else
            {
                entry = FeatureFlagEntry.Create(
                    definitionId: definition.Id,
                    key: request.Key,
                    scope: request.Scope,
                    isEnabled: request.IsEnabled,
                    createdBy: userId,
                    scopeReferenceId: request.ScopeReferenceId,
                    changeReason: request.ChangeReason);

                await repository.AddEntryAsync(entry, cancellationToken);
            }

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

            return new FeatureFlagEntryDto(
                Id: entry.Id.Value,
                Key: entry.Key,
                Scope: entry.Scope.ToString(),
                ScopeReferenceId: Guid.TryParse(entry.ScopeReferenceId, out var refId) ? refId : null,
                IsEnabled: entry.IsEnabled,
                IsActive: entry.IsActive,
                ChangeReason: entry.ChangeReason,
                UpdatedAt: entry.UpdatedAt ?? entry.CreatedAt,
                UpdatedBy: entry.UpdatedBy ?? entry.CreatedBy,
                RowVersion: entry.RowVersion);
        }
    }
}
