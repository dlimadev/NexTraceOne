namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de risco de feature flags.
/// Cruza <c>FeatureFlagRecord</c> com dados de incidentes e presença em produção.
/// Por omissão satisfeita por <c>NullFeatureFlagRiskReader</c> (honest-null).
/// Wave AS.2 — GetFeatureFlagRiskReport.
/// </summary>
public interface IFeatureFlagRiskReader
{
    Task<IReadOnlyList<FlagRiskEntry>> ListFlagRiskByTenantAsync(
        string tenantId,
        int staleFlagDays,
        int prodPresenceDays,
        int incidentWindowHours,
        CancellationToken ct);

    /// <summary>Entrada de risco por flag (agregada a nível de tenant).</summary>
    public sealed record FlagRiskEntry(
        string ServiceId,
        string ServiceName,
        string FlagKey,
        StalenessRisk StalenessRisk,
        OwnershipRisk OwnershipRisk,
        ProductionPresenceRisk ProductionPresenceRisk,
        bool IncidentCorrelated,
        IReadOnlyList<string> ScheduledRemovalOverdueKeys,
        DateTimeOffset? ScheduledRemovalDate,
        DateTimeOffset? LastToggledAt,
        DateTimeOffset CreatedAt);

    /// <summary>Grau de desactualização da flag.</summary>
    public enum StalenessRisk { Low, Medium, High }

    /// <summary>Grau de risco de ausência de owner.</summary>
    public enum OwnershipRisk { None, Low }

    /// <summary>Grau de presença da flag em produção.</summary>
    public enum ProductionPresenceRisk { Low, Medium, High }
}
