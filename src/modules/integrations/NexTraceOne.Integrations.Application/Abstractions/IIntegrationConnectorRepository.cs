using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Interface do repositório de IntegrationConnectors para o módulo Integrations.
/// Define operações CRUD e consultas para conectores de integração.
///
/// Extraído de Governance.Application em P2.1 — owner correto: módulo Integrations.
/// </summary>
public interface IIntegrationConnectorRepository
{
    /// <summary>Lista todos os conectores com filtros opcionais.</summary>
    Task<IReadOnlyList<IntegrationConnector>> ListAsync(
        ConnectorStatus? status,
        ConnectorHealth? health,
        string? connectorType,
        string? search,
        CancellationToken ct);

    /// <summary>Obtém um conector pelo seu identificador.</summary>
    Task<IntegrationConnector?> GetByIdAsync(IntegrationConnectorId id, CancellationToken ct);

    /// <summary>Obtém um conector pelo seu nome técnico.</summary>
    Task<IntegrationConnector?> GetByNameAsync(string name, CancellationToken ct);

    /// <summary>Adiciona um novo conector ao repositório.</summary>
    Task AddAsync(IntegrationConnector connector, CancellationToken ct);

    /// <summary>Atualiza um conector existente.</summary>
    Task UpdateAsync(IntegrationConnector connector, CancellationToken ct);

    /// <summary>Conta total de conectores por status.</summary>
    Task<int> CountByStatusAsync(ConnectorStatus status, CancellationToken ct);

    /// <summary>Conta total de conectores por health.</summary>
    Task<int> CountByHealthAsync(ConnectorHealth health, CancellationToken ct);
}
