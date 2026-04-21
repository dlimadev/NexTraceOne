using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Contrato de persistência para fontes de dados externas.
/// Suporta CRUD completo e consultas filtradas por tipo e estado.
/// </summary>
public interface IExternalDataSourceRepository
{
    /// <summary>Retorna todas as fontes, com filtros opcionais por tipo e estado activo.</summary>
    Task<IReadOnlyList<ExternalDataSource>> ListAsync(
        ExternalDataSourceConnectorType? connectorType,
        bool? isActive,
        CancellationToken ct);

    /// <summary>Retorna uma fonte pelo seu identificador, ou null se não existir.</summary>
    Task<ExternalDataSource?> GetByIdAsync(ExternalDataSourceId id, CancellationToken ct);

    /// <summary>Verifica se já existe uma fonte com o nome especificado.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct);

    /// <summary>Adiciona uma nova fonte de dados.</summary>
    Task AddAsync(ExternalDataSource source, CancellationToken ct);

    /// <summary>
    /// Lista fontes elegíveis para sincronização automática:
    /// activas, com SyncIntervalMinutes &gt; 0, e cujo próximo sync já expirou.
    /// </summary>
    Task<IReadOnlyList<ExternalDataSource>> ListDueForSyncAsync(
        DateTimeOffset now,
        CancellationToken ct);
}
