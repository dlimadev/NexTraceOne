namespace NexTraceOne.Configuration.Contracts.DTOs;

/// <summary>
/// Representa os metadados de uma feature flag registada na plataforma.
/// </summary>
public sealed record FeatureFlagDefinitionDto(
    Guid Id,
    string Key,
    string DisplayName,
    string? Description,
    bool DefaultEnabled,
    string[] AllowedScopes,
    Guid? ModuleId,
    bool IsActive,
    bool IsEditable);

/// <summary>
/// Representa uma substituição de valor de feature flag para um âmbito específico.
/// O <see cref="RowVersion"/> é o token de concorrência xmin do PostgreSQL — usado para
/// detetar modificações concorrentes e retornar HTTP 409 quando ocorre um lost-update.
/// </summary>
public sealed record FeatureFlagEntryDto(
    Guid Id,
    string Key,
    string Scope,
    Guid? ScopeReferenceId,
    bool IsEnabled,
    bool IsActive,
    string? ChangeReason,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy,
    uint RowVersion);

/// <summary>
/// Representa o valor efetivo resolvido de uma feature flag para um âmbito dado,
/// após aplicação da hierarquia Instance → Tenant → Environment.
/// </summary>
public sealed record EvaluatedFeatureFlagDto(
    string Key,
    bool IsEnabled,
    string ResolvedScope,
    Guid? ResolvedScopeReferenceId,
    bool IsInherited,
    bool IsDefault,
    string DisplayName,
    string? Description);
