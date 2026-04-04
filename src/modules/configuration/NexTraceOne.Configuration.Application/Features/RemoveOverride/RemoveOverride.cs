
using FluentValidation;

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
