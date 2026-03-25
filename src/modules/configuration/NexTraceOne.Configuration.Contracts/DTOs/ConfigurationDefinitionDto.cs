namespace NexTraceOne.Configuration.Contracts.DTOs;

/// <summary>
/// Represents the schema/definition of a configuration key,
/// including allowed scopes, validation rules, deprecation status and UI metadata.
/// </summary>
public sealed record ConfigurationDefinitionDto(
    string Key,
    string DisplayName,
    string? Description,
    string Category,
    string[] AllowedScopes,
    string? DefaultValue,
    string ValueType,
    bool IsSensitive,
    bool IsEditable,
    bool IsInheritable,
    string? ValidationRules,
    string? UiEditorType,
    int SortOrder,
    bool IsDeprecated,
    string? DeprecatedMessage);
