using NexTraceOne.Catalog.Domain.ProductAnalytics.Entities;

namespace NexTraceOne.Catalog.Application.ProductAnalytics.Abstractions;

/// <summary>
/// Port para envio de eventos de analytics para o storage analítico (Elastic ou ClickHouse).
/// Isola a camada Application do detalhe de infraestrutura (IAnalyticsWriter).
/// Implementado em Infrastructure via IAnalyticsWriter do BuildingBlocks.
/// </summary>
public interface IAnalyticsEventForwarder
{
    /// <summary>
    /// Encaminha um evento de analytics recém-registado para o storage analítico configurado.
    /// Falhas são suprimidas por padrão (Analytics:SuppressWriteErrors = true).
    /// </summary>
    Task ForwardAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default);
}
