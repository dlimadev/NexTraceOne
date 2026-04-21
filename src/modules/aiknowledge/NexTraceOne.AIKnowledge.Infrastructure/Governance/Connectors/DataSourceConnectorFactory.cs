using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Connectors;

/// <summary>
/// Fábrica de conectores de fontes de dados externas.
/// Resolve o conector correcto via DI com base no <see cref="ExternalDataSourceConnectorType"/>.
/// </summary>
internal sealed class DataSourceConnectorFactory(IEnumerable<IDataSourceConnector> connectors)
    : IDataSourceConnectorFactory
{
    private readonly Dictionary<ExternalDataSourceConnectorType, IDataSourceConnector> _connectors
        = connectors.ToDictionary(c => c.ConnectorType);

    public IDataSourceConnector GetConnector(ExternalDataSourceConnectorType connectorType)
    {
        if (_connectors.TryGetValue(connectorType, out var connector))
            return connector;

        throw new InvalidOperationException(
            $"No connector registered for type '{connectorType}'. " +
            $"Available types: {string.Join(", ", _connectors.Keys)}.");
    }

    public IReadOnlyList<ExternalDataSourceConnectorType> GetAvailableConnectorTypes()
        => [.. _connectors.Keys];
}
