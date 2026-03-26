namespace NexTraceOne.Configuration.Contracts.DTOs;

/// <summary>
/// Represents a stored configuration value bound to a specific scope.
/// The <see cref="Value"/> is masked when <c>IsSensitive</c> is true on the definition.
/// The <see cref="RowVersion"/> is the PostgreSQL xmin concurrency token — used to detect
/// concurrent modifications and return HTTP 409 Conflict when a lost-update is detected.
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
    string? UpdatedBy,
    uint RowVersion);
