using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para snapshots analisados de schemas Protobuf.
/// Permite persistência e consulta de snapshots por contrato e tenant.
/// Wave H.1 — Protobuf Schema Analysis.
/// </summary>
public interface IProtobufSchemaSnapshotRepository
{
    /// <summary>Adiciona um novo snapshot ao repositório.</summary>
    void Add(ProtobufSchemaSnapshot snapshot);

    /// <summary>Obtém o snapshot mais recente para um dado ApiAsset.</summary>
    Task<ProtobufSchemaSnapshot?> GetLatestByApiAssetAsync(Guid apiAssetId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Lista todos os snapshots para um dado ApiAsset, ordenados do mais recente para o mais antigo.</summary>
    Task<IReadOnlyList<ProtobufSchemaSnapshot>> ListByApiAssetAsync(Guid apiAssetId, Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Obtém um snapshot pelo seu identificador único.</summary>
    Task<ProtobufSchemaSnapshot?> GetByIdAsync(ProtobufSchemaSnapshotId id, CancellationToken cancellationToken = default);
}
