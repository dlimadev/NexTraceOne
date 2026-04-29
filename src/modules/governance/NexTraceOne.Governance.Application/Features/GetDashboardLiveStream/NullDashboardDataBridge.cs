using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetDashboardLiveStream;

/// <summary>
/// Implementação nula do bridge — retorna lista vazia.
/// Registada quando nenhuma fonte de dados real está configurada.
/// O gerador SSE emite heartbeats com IsSimulated=true neste caso.
/// </summary>
internal sealed class NullDashboardDataBridge : IDashboardDataBridge
{
    public static readonly NullDashboardDataBridge Instance = new();

    public Task<IReadOnlyList<GetDashboardLiveStream.LiveEvent>> GetPendingEventsAsync(
        Guid dashboardId, string tenantId, IReadOnlyList<string>? widgetIds,
        DateTimeOffset since, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<GetDashboardLiveStream.LiveEvent>>([]);
}
