namespace NexTraceOne.Catalog.Domain.Entities;

/// <summary>
/// Entidade de domínio que representa o estado actual de uma feature flag para um serviço.
/// Wave AS.1 — Feature Flag &amp; Experimentation Governance.
/// </summary>
public sealed class FeatureFlagRecord
{
    /// <summary>Tipo da feature flag.</summary>
    public enum FlagType { Release, Experiment, Permission, KillSwitch }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string ServiceId { get; private set; } = string.Empty;
    public string FlagKey { get; private set; } = string.Empty;
    public FlagType Type { get; private set; }
    public bool IsEnabled { get; private set; }
    public string? EnabledEnvironmentsJson { get; private set; }
    public string? OwnerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastToggledAt { get; private set; }
    public DateTimeOffset? ScheduledRemovalDate { get; private set; }

    private FeatureFlagRecord() { }

    /// <summary>
    /// Cria uma nova instância de <see cref="FeatureFlagRecord"/>.
    /// </summary>
    public static FeatureFlagRecord Create(
        string tenantId,
        string serviceId,
        string flagKey,
        FlagType flagType,
        bool isEnabled,
        string? enabledEnvironmentsJson,
        string? ownerId,
        DateTimeOffset? lastToggledAt,
        DateTimeOffset? scheduledRemovalDate,
        DateTimeOffset now)
    {
        return new FeatureFlagRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            FlagKey = flagKey,
            Type = flagType,
            IsEnabled = isEnabled,
            EnabledEnvironmentsJson = enabledEnvironmentsJson,
            OwnerId = ownerId,
            CreatedAt = now,
            LastToggledAt = lastToggledAt,
            ScheduledRemovalDate = scheduledRemovalDate
        };
    }

    /// <summary>
    /// Actualiza o estado actual da flag (idempotente por ServiceId+FlagKey).
    /// </summary>
    public void Upsert(
        bool isEnabled,
        string? enabledEnvironmentsJson,
        string? ownerId,
        DateTimeOffset? lastToggledAt,
        DateTimeOffset? scheduledRemovalDate,
        DateTimeOffset now)
    {
        IsEnabled = isEnabled;
        EnabledEnvironmentsJson = enabledEnvironmentsJson;
        OwnerId = ownerId;
        LastToggledAt = lastToggledAt;
        ScheduledRemovalDate = scheduledRemovalDate;
        _ = now; // reservado para UpdatedAt em migração futura
    }
}
