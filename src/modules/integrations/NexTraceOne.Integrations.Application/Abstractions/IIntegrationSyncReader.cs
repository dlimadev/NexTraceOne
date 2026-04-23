namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Leitor analítico de dados de sincronização de integrações externas.
/// Suporta cálculo de SyncSuccessRate, DataFreshnessStatus e IntegrationHealthTier.
/// </summary>
public interface IIntegrationSyncReader
{
    /// <summary>Lista entradas de saúde de integrações activas para o tenant.</summary>
    Task<IReadOnlyList<IntegrationSyncEntry>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>Retorna histórico de 7 dias de tier de saúde por integração.</summary>
    Task<IReadOnlyList<IntegrationHealthHistoryEntry>> GetHealthHistoryAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>Dados de sincronização de uma integração activa.</summary>
    public sealed record IntegrationSyncEntry(
        string IntegrationId,
        string IntegrationName,
        string IntegrationType,
        DateTimeOffset? LastSyncAt,
        int TotalSyncsInWindow,
        int SuccessfulSyncsInWindow,
        string? LastErrorMessage,
        bool IsCritical,
        IReadOnlyList<string> AffectedFeatures);

    /// <summary>Snapshot de tier de saúde numa data.</summary>
    public sealed record IntegrationHealthHistoryEntry(
        string IntegrationId,
        string IntegrationName,
        int DaysAgo,
        string HealthTier);
}
