namespace NexTraceOne.Configuration.Contracts.DTOs;

/// <summary>
/// Represents the resolved/effective value of a configuration key
/// after scope inheritance and default resolution.
/// </summary>
public sealed record EffectiveConfigurationDto(
    string Key,
    string? EffectiveValue,
    string ResolvedScope,
    Guid? ResolvedScopeReferenceId,
    bool IsInherited,
    bool IsDefault,
    string DefinitionKey,
    string ValueType,
    bool IsSensitive,
    int Version);
