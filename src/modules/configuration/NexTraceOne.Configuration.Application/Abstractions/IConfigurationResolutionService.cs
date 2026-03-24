using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Service abstraction for resolving effective configuration values.
/// Applies hierarchical scope inheritance and default resolution
/// to produce the final value for a given key and scope.
/// Implementation provided by the Infrastructure layer.
/// </summary>
public interface IConfigurationResolutionService
{
    /// <summary>
    /// Resolves the effective value for a single configuration key within the specified scope.
    /// Returns null if no value is found at any level.
    /// </summary>
    Task<EffectiveConfigurationDto?> ResolveEffectiveValueAsync(
        string key,
        ConfigurationScope scope,
        string? scopeReferenceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves all effective configuration values for the specified scope.
    /// Applies inheritance from parent scopes and default values.
    /// </summary>
    Task<List<EffectiveConfigurationDto>> ResolveAllEffectiveAsync(
        ConfigurationScope scope,
        string? scopeReferenceId,
        CancellationToken cancellationToken);
}
