namespace NexTraceOne.BuildingBlocks.Application.Widgets;

/// <summary>
/// Widget SDK — contrato estável para tipos de widget do NexTraceOne.
/// Todos os widgets (built-in e de terceiros) devem implementar esta interface.
/// Wave V3.2 — Query-driven Widgets &amp; Widget SDK.
/// </summary>
public interface IWidgetKind
{
    /// <summary>Identificador único do widget (ex: "query-widget", "dora-metrics").</summary>
    string Key { get; }

    /// <summary>JSON Schema (draft 7) descrevendo a configuração aceite pelo widget.</summary>
    string Schema { get; }

    /// <summary>
    /// Hint de renderização padrão: "table" | "line" | "bar" | "area" | "stat" | "heatmap".
    /// O frontend pode sobrepor este hint por instância de widget.
    /// </summary>
    string DefaultRenderHint { get; }
}
