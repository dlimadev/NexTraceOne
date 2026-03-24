namespace NexTraceOne.Configuration.Contracts.DTOs;

/// <summary>
/// Represents a stored configuration value bound to a specific scope.
/// The <see cref="Value"/> is masked when <c>IsSensitive</c> is true on the definition.
/// </summary>
public sealed record ConfigurationEntryDto(
    Guid Id,
    string DefinitionKey,
    string Scope,
    Guid? ScopeReferenceId,
    string? Value,
    bool IsActive,
    int Version,
    string? ChangeReason,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy);
