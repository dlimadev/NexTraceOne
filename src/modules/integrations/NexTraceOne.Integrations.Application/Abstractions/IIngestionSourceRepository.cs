using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Interface do repositório de IngestionSources para o módulo Integrations.
/// Define operações CRUD e consultas para fontes de ingestão.
/// </summary>
public interface IIngestionSourceRepository
{
    /// <summary>Lista todas as fontes com filtros opcionais.</summary>
    Task<IReadOnlyList<IngestionSource>> ListAsync(
        IntegrationConnectorId? connectorId,
        SourceStatus? status,
        FreshnessStatus? freshnessStatus,
        CancellationToken ct);

    /// <summary>Lista fontes por conector.</summary>
    Task<IReadOnlyList<IngestionSource>> ListByConnectorIdAsync(
        IntegrationConnectorId connectorId,
        CancellationToken ct);

    /// <summary>Obtém uma fonte pelo seu identificador.</summary>
    Task<IngestionSource?> GetByIdAsync(IngestionSourceId id, CancellationToken ct);

    /// <summary>Obtém uma fonte pelo nome dentro de um conector.</summary>
    Task<IngestionSource?> GetByConnectorAndNameAsync(
        IntegrationConnectorId connectorId,
        string name,
        CancellationToken ct);

    /// <summary>Adiciona uma nova fonte ao repositório.</summary>
    Task AddAsync(IngestionSource source, CancellationToken ct);

    /// <summary>Atualiza uma fonte existente.</summary>
    Task UpdateAsync(IngestionSource source, CancellationToken ct);

    /// <summary>Conta fontes por freshness status.</summary>
    Task<int> CountByFreshnessStatusAsync(FreshnessStatus status, CancellationToken ct);
}
