using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para WidgetSnapshot.</summary>
public sealed record WidgetSnapshotId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Snapshot do estado de um widget num instante específico.
/// Usado para calcular deltas reais entre capturas sucessivas (B-02).
/// </summary>
public sealed class WidgetSnapshot : Entity<WidgetSnapshotId>
{
    private WidgetSnapshot() { }

    public string TenantId { get; private init; } = string.Empty;
    public Guid DashboardId { get; private init; }
    public string WidgetId { get; private init; } = string.Empty;

    /// <summary>Hash SHA-256 do DataJson para detecção rápida de mudanças.</summary>
    public string DataHash { get; private set; } = string.Empty;

    /// <summary>Payload JSON serializado do estado do widget.</summary>
    public string DataJson { get; private set; } = string.Empty;

    public DateTimeOffset CapturedAt { get; private init; }

    public static WidgetSnapshot Create(
        string tenantId,
        Guid dashboardId,
        string widgetId,
        string dataHash,
        string dataJson,
        DateTimeOffset capturedAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(widgetId);
        Guard.Against.NullOrWhiteSpace(dataHash);

        return new WidgetSnapshot
        {
            Id = new WidgetSnapshotId(Guid.NewGuid()),
            TenantId = tenantId,
            DashboardId = dashboardId,
            WidgetId = widgetId,
            DataHash = dataHash,
            DataJson = dataJson,
            CapturedAt = capturedAt
        };
    }
}
