using NexTraceOne.Integrations.Application.Abstractions;

namespace NexTraceOne.Integrations.Application;

/// <summary>
/// Implementação nula de <see cref="IIntegrationSyncReader"/>.
/// Utilizada quando não existe fonte de dados de sincronização de integrações configurada.
/// </summary>
public sealed class NullIntegrationSyncReader : IIntegrationSyncReader
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<IIntegrationSyncReader.IntegrationSyncEntry>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IIntegrationSyncReader.IntegrationSyncEntry>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<IIntegrationSyncReader.IntegrationHealthHistoryEntry>> GetHealthHistoryAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IIntegrationSyncReader.IntegrationHealthHistoryEntry>>([]);
}
