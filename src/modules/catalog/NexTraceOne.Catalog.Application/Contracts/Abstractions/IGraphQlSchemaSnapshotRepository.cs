using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para snapshots analisados de schemas GraphQL.
/// Permite persistência e consulta de snapshots por contrato e tenant.
/// Wave G.3 — GraphQL Schema Analysis.
/// </summary>
public interface IGraphQlSchemaSnapshotRepository
{
    /// <summary>Adiciona um novo snapshot ao repositório.</summary>
    void Add(GraphQlSchemaSnapshot snapshot);

    /// <summary>Obtém o snapshot mais recente para um dado ApiAsset.</summary>
    Task<GraphQlSchemaSnapshot?> GetLatestByApiAssetAsync(Guid apiAssetId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Lista todos os snapshots para um dado ApiAsset, ordenados do mais recente para o mais antigo.</summary>
    Task<IReadOnlyList<GraphQlSchemaSnapshot>> ListByApiAssetAsync(Guid apiAssetId, Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Obtém um snapshot pelo seu identificador único.</summary>
    Task<GraphQlSchemaSnapshot?> GetByIdAsync(GraphQlSchemaSnapshotId id, CancellationToken cancellationToken = default);
}
