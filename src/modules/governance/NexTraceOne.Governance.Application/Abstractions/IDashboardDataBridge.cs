using NexTraceOne.Governance.Application.Features.GetDashboardLiveStream;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Bridge entre o SSE live-stream e as fontes de dados reais do dashboard.
/// Implementações retornam eventos reais (IsSimulated=false) quando disponíveis.
/// A implementação nula retorna lista vazia, causando heartbeat honest-gap.
/// </summary>
public interface IDashboardDataBridge
{
    /// <summary>
    /// Retorna eventos pendentes para o dashboard desde <paramref name="since"/>.
    /// Retorna lista vazia se não houver novos dados.
    /// </summary>
    Task<IReadOnlyList<GetDashboardLiveStream.LiveEvent>> GetPendingEventsAsync(
        Guid dashboardId,
        string tenantId,
        IReadOnlyList<string>? widgetIds,
        DateTimeOffset since,
        CancellationToken ct = default);
}
