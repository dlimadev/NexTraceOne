using System.Globalization;
using System.Text.Json;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.SetConfigurationValue;

/// <summary>
/// Feature: SetConfigurationValue — creates or updates a configuration entry.
/// Validates against the definition (allowed scopes, value type, editability, deprecation),
/// encrypts sensitive values, creates an audit trail entry, and invalidates cache.
/// </summary>
public static class SetConfigurationValue
{
    /// <summary>Command to set a configuration value for a specific scope.</summary>
    public sealed record Command(
        string Key,
        ConfigurationScope Scope,
        string? ScopeReferenceId,
        string Value,
        string? ChangeReason) : ICommand<ConfigurationEntryDto>;

    /// <summary>Validates the command parameters.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Key).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Value).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.ScopeReferenceId).MaximumLength(256);
            RuleFor(x => x.ChangeReason).MaximumLength(500);
        }
    }

    /// <summary>Handler that validates, persists, encrypts, audits, and invalidates cache.</summary>
    public sealed class Handler(
        IConfigurationDefinitionRepository definitionRepository,
        IConfigurationEntryRepository entryRepository,
        IConfigurationAuditRepository auditRepository,
        IConfigurationSecurityService securityService,
        IConfigurationCacheService cacheService,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, ConfigurationEntryDto>
    {
        public async Task<Result<ConfigurationEntryDto>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var definition = await definitionRepository.GetByKeyAsync(request.Key, cancellationToken);
            if (definition is null)
                return Error.NotFound(
                    "CONFIG_DEFINITION_NOT_FOUND",
                    "Configuration definition for key '{0}' not found.",
                    request.Key);

            if (!definition.IsEditable)
                return Error.Business(
                    "CONFIG_NOT_EDITABLE",
                    "Configuration '{0}' is not editable.",
                    request.Key);

            if (definition.IsDeprecated)
                return Error.Business(
                    "CONFIG_DEPRECATED",
                    "Configuration '{0}' is deprecated. {1}",
                    request.Key,
                    definition.DeprecatedMessage ?? "No new values should be set for this configuration.");

            if (!definition.AllowedScopes.Contains(request.Scope))
                return Error.Validation(
                    "CONFIG_SCOPE_NOT_ALLOWED",
                    "Scope '{0}' is not allowed for configuration '{1}'. Allowed scopes: {2}.",
                    request.Scope.ToString(),
                    request.Key,
                    string.Join(", ", definition.AllowedScopes));

            var valueTypeError = ValidateValueType(request.Value, definition.ValueType);
            if (valueTypeError is not null)
                return Error.Validation(
                    "CONFIG_VALUE_TYPE_INVALID",
                    "Value '{0}' is not valid for type '{1}'. {2}",
                    request.Value,
                    definition.ValueType.ToString(),
                    valueTypeError);

            var existingEntry = await entryRepository.GetByKeyAndScopeAsync(
                request.Key,
                request.Scope,
                request.ScopeReferenceId,
                cancellationToken);

            var valueToStore = definition.IsSensitive
                ? securityService.EncryptValue(request.Value)
                : request.Value;

            var userId = currentUser.Id;

            ConfigurationEntry entry;
            string auditAction;
            string? previousValue = null;
            int? previousVersion = null;

            if (existingEntry is not null)
            {
                previousValue = existingEntry.Value;
                previousVersion = existingEntry.Version;
                auditAction = "Updated";

                existingEntry.UpdateValue(
                    value: valueToStore,
                    structuredValueJson: null,
                    updatedBy: userId,
                    changeReason: request.ChangeReason,
                    isEncrypted: definition.IsSensitive);

                await entryRepository.UpdateAsync(existingEntry, cancellationToken);
                entry = existingEntry;
            }
            else
            {
                auditAction = "Created";

                entry = ConfigurationEntry.Create(
                    definitionId: definition.Id,
                    key: request.Key,
                    scope: request.Scope,
                    createdBy: userId,
                    scopeReferenceId: request.ScopeReferenceId,
                    value: valueToStore,
                    isSensitive: definition.IsSensitive,
                    isEncrypted: definition.IsSensitive,
                    changeReason: request.ChangeReason);

                await entryRepository.AddAsync(entry, cancellationToken);
            }

            var auditEntry = ConfigurationAuditEntry.Create(
                entryId: entry.Id,
                key: request.Key,
                scope: request.Scope,
                action: auditAction,
                newVersion: entry.Version,
                changedBy: userId,
                scopeReferenceId: request.ScopeReferenceId,
                previousValue: previousValue,
                newValue: valueToStore,
                previousVersion: previousVersion,
                changeReason: request.ChangeReason,
                isSensitive: definition.IsSensitive);

            await auditRepository.AddAsync(auditEntry, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            await cacheService.InvalidateAsync(request.Key, request.Scope, cancellationToken);

            var displayValue = definition.IsSensitive
                ? securityService.MaskValue(request.Value)
                : entry.Value;

            var dto = new ConfigurationEntryDto(
                Id: entry.Id.Value,
                DefinitionKey: entry.Key,
                Scope: entry.Scope.ToString(),
                ScopeReferenceId: Guid.TryParse(entry.ScopeReferenceId, out var refId) ? refId : null,
                Value: displayValue,
                IsActive: entry.IsActive,
                Version: entry.Version,
                ChangeReason: entry.ChangeReason,
                UpdatedAt: entry.UpdatedAt ?? entry.CreatedAt,
                UpdatedBy: entry.UpdatedBy ?? entry.CreatedBy);

            return dto;
        }

        /// <summary>
        /// Validates that the provided value is compatible with the expected ConfigurationValueType.
        /// Returns null if valid, or an error message string if invalid.
        /// </summary>
        private static string? ValidateValueType(string value, ConfigurationValueType valueType)
        {
            return valueType switch
            {
                ConfigurationValueType.Boolean when
                    !string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
                    => "Expected 'true' or 'false'.",

                ConfigurationValueType.Integer when
                    !long.TryParse(value, CultureInfo.InvariantCulture, out _)
                    => "Expected a valid integer number.",

                ConfigurationValueType.Decimal when
                    !decimal.TryParse(value, CultureInfo.InvariantCulture, out _)
                    => "Expected a valid decimal number.",

                ConfigurationValueType.Json when !IsValidJson(value)
                    => "Expected valid JSON.",

                _ => null
            };
        }

        private static bool IsValidJson(string value)
        {
            try
            {
                JsonDocument.Parse(value);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
