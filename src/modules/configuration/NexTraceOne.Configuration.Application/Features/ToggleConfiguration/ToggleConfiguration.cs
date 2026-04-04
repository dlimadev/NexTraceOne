
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.ToggleConfiguration;

/// <summary>
/// Feature: ToggleConfiguration — activates or deactivates a configuration entry.
/// Creates an audit trail entry and invalidates the cache.
/// </summary>
public static class ToggleConfiguration
{
    /// <summary>Command to activate or deactivate a configuration entry.</summary>
    public sealed record Command(
        string Key,
        ConfigurationScope Scope,
        string? ScopeReferenceId,
        bool Activate,
        string? ChangeReason) : ICommand<bool>;

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

    /// <summary>Handler that toggles the entry state, audits, and invalidates cache.</summary>
    public sealed class Handler(
        IConfigurationEntryRepository entryRepository,
        IConfigurationAuditRepository auditRepository,
        IConfigurationCacheService cacheService,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, bool>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entry = await entryRepository.GetByKeyAndScopeAsync(
                request.Key,
                request.Scope,
                request.ScopeReferenceId,
                cancellationToken);

            if (entry is null)
                return Error.NotFound(
                    "CONFIG_ENTRY_NOT_FOUND",
                    "No configuration entry found for key '{0}' in scope '{1}'.",
                    request.Key,
                    request.Scope.ToString());

            if (entry.IsActive == request.Activate)
                return Error.Conflict(
                    "CONFIG_ALREADY_IN_STATE",
                    "Configuration '{0}' is already {1}.",
                    request.Key,
                    request.Activate ? "active" : "inactive");

            var userId = currentUser.Id;
            var previousVersion = entry.Version;

            if (request.Activate)
                entry.Activate(userId, request.ChangeReason);
            else
                entry.Deactivate(userId, request.ChangeReason);

            await entryRepository.UpdateAsync(entry, cancellationToken);

            var auditEntry = ConfigurationAuditEntry.Create(
                entryId: entry.Id,
                key: entry.Key,
                scope: entry.Scope,
                action: request.Activate ? "Activated" : "Deactivated",
                newVersion: entry.Version,
                changedBy: userId,
                scopeReferenceId: entry.ScopeReferenceId,
                previousValue: null,
                newValue: null,
                previousVersion: previousVersion,
                changeReason: request.ChangeReason,
                isSensitive: entry.IsSensitive);

            await auditRepository.AddAsync(auditEntry, cancellationToken);

            try
            {
                await unitOfWork.CommitAsync(cancellationToken);
            }
            catch (NexTraceOne.BuildingBlocks.Application.Abstractions.ConcurrencyException)
            {
                return Error.Conflict(
                    "CONFIG_CONCURRENCY_CONFLICT",
                    "The configuration entry '{0}' was modified by another process. Please reload and try again.",
                    request.Key);
            }

            await cacheService.InvalidateAsync(request.Key, request.Scope, cancellationToken);

            return true;
        }
    }
}
