using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Fábrica de conectores de fontes de dados externas.
/// Resolve o conector correcto com base no <see cref="ExternalDataSourceConnectorType"/>.
/// </summary>
public interface IDataSourceConnectorFactory
{
    /// <summary>
    /// Resolve o conector para o tipo especificado.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Lançado quando nenhum conector está registado para o tipo.
    /// </exception>
    IDataSourceConnector GetConnector(ExternalDataSourceConnectorType connectorType);

    /// <summary>
    /// Lista todos os tipos de conector disponíveis no sistema.
    /// </summary>
    IReadOnlyList<ExternalDataSourceConnectorType> GetAvailableConnectorTypes();
}
