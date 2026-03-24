using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.Configuration.Contracts.IntegrationEvents;

/// <summary>
/// Integration events emitted by the Configuration module
/// when configuration values change state.
/// </summary>
public static class ConfigurationIntegrationEvents
{
    private const string ModuleName = "Configuration";

    /// <summary>
    /// Raised when a configuration value is created or updated.
    /// </summary>
    public sealed record ConfigurationValueChanged(
        string Key,
        string Scope,
        Guid? ScopeReferenceId,
        string? PreviousValue,
        string? NewValue,
        string? ChangedBy)
        : IntegrationEventBase(ModuleName);

    /// <summary>
    /// Raised when a configuration value is activated.
    /// </summary>
    public sealed record ConfigurationValueActivated(
        string Key,
        string Scope,
        Guid? ScopeReferenceId)
        : IntegrationEventBase(ModuleName);

    /// <summary>
    /// Raised when a configuration value is deactivated.
    /// </summary>
    public sealed record ConfigurationValueDeactivated(
        string Key,
        string Scope,
        Guid? ScopeReferenceId)
        : IntegrationEventBase(ModuleName);
}
