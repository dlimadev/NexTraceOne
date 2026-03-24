using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.RemoveOverride;

/// <summary>
/// Feature: RemoveOverride — removes a scope-level configuration override.
/// Creates an audit trail entry and invalidates the cache.
/// </summary>
public static class RemoveOverride
{
    /// <summary>Command to remove a configuration override for a specific scope.</summary>
    public sealed record Command(
        string Key,
        ConfigurationScope Scope,
        string? ScopeReferenceId,
        string? ChangeReason) : ICommand<bool>;

    /// <summary>Handler that finds and removes the entry, then audits and invalidates cache.</summary>
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

            var auditEntry = ConfigurationAuditEntry.Create(
                entryId: entry.Id,
                key: entry.Key,
                scope: entry.Scope,
                action: "Removed",
                newVersion: entry.Version,
                changedBy: currentUser.Id,
                scopeReferenceId: entry.ScopeReferenceId,
                previousValue: entry.Value,
                newValue: null,
                previousVersion: entry.Version,
                changeReason: request.ChangeReason,
                isSensitive: entry.IsSensitive);

            await auditRepository.AddAsync(auditEntry, cancellationToken);
            await entryRepository.DeleteAsync(entry, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            await cacheService.InvalidateAsync(request.Key, request.Scope, cancellationToken);

            return true;
        }
    }
}
