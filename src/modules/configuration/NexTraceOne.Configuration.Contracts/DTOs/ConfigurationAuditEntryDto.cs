namespace NexTraceOne.Configuration.Contracts.DTOs;

/// <summary>
/// Represents an audit trail record for a configuration change.
/// Sensitive values are masked before being returned in this DTO.
/// </summary>
public sealed record ConfigurationAuditEntryDto(
    string Key,
    string Scope,
    string? ScopeReferenceId,
    string Action,
    string? PreviousValue,
    string? NewValue,
    int? PreviousVersion,
    int NewVersion,
    string ChangedBy,
    DateTimeOffset ChangedAt,
    string? ChangeReason,
    bool IsSensitive);
