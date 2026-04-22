using NexTraceOne.Catalog.Application.Graph.Abstractions;

namespace NexTraceOne.Catalog.Application.Graph.Services;

/// <summary>
/// Implementação nula de <see cref="IKnowledgeRelationReader"/> com dados de teste codificados.
///
/// Devolve três serviços de exemplo com relações pré-definidas para validar o comportamento
/// do handler <c>GetKnowledgeRelationGraph</c> sem necessidade de base de dados.
/// Substituir por implementação real baseada em EF/PostgreSQL em produção.
///
/// Wave AB.1 — GetKnowledgeRelationGraph.
/// </summary>
public sealed class NullKnowledgeRelationReader : IKnowledgeRelationReader
{
    private static readonly DateTimeOffset BaseDate = new(2025, 10, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public Task<IReadOnlyList<ServiceRelationEntry>> ListServiceRelationsAsync(
        string tenantId,
        CancellationToken ct)
    {
        IReadOnlyList<ServiceRelationEntry> entries =
        [
            new ServiceRelationEntry(
                ServiceName: "order-service",
                TeamName: "team-payments",
                DependsOnServices: ["inventory-service", "notification-service"],
                PublishedContracts: ["order-api-v1", "order-api-v2"],
                ConsumedContracts: ["inventory-api-v1"],
                AssociatedRunbooks: ["runbook-order-failure", "runbook-payment-timeout"],
                AssociatedIncidentTypes: ["OrderProcessingFailure", "PaymentTimeout"],
                LastReleaseAt: BaseDate.AddDays(-10),
                LastIncidentAt: BaseDate.AddDays(-5)),

            new ServiceRelationEntry(
                ServiceName: "inventory-service",
                TeamName: "team-logistics",
                DependsOnServices: ["warehouse-service"],
                PublishedContracts: ["inventory-api-v1"],
                ConsumedContracts: [],
                AssociatedRunbooks: ["runbook-stock-sync"],
                AssociatedIncidentTypes: ["StockSyncFailure"],
                LastReleaseAt: BaseDate.AddDays(-30),
                LastIncidentAt: BaseDate.AddDays(-15)),

            new ServiceRelationEntry(
                ServiceName: "notification-service",
                TeamName: "team-platform",
                DependsOnServices: [],
                PublishedContracts: ["notification-api-v1"],
                ConsumedContracts: [],
                AssociatedRunbooks: [],
                AssociatedIncidentTypes: ["EmailDeliveryFailure"],
                LastReleaseAt: BaseDate.AddDays(-60),
                LastIncidentAt: null),
        ];

        return Task.FromResult(entries);
    }
}
