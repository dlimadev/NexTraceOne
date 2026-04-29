using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Repositório de snapshots de widgets para cálculo de delta real (B-02).
/// </summary>
public interface IWidgetSnapshotRepository
{
    /// <summary>Retorna o snapshot mais recente anterior a <paramref name="before"/> para o widget indicado.</summary>
    Task<WidgetSnapshot?> GetLatestBeforeAsync(
        string tenantId,
        Guid dashboardId,
        string widgetId,
        DateTimeOffset before,
        CancellationToken ct = default);

    /// <summary>Retorna todos os snapshots do widget após <paramref name="since"/>.</summary>
    Task<IReadOnlyList<WidgetSnapshot>> ListSinceAsync(
        string tenantId,
        Guid dashboardId,
        string widgetId,
        DateTimeOffset since,
        CancellationToken ct = default);

    Task AddAsync(WidgetSnapshot snapshot, CancellationToken ct = default);
}
