using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para schemas de Data Contracts com classificação PII e SLA de frescura.
/// CC-03.
/// </summary>
public interface IDataContractSchemaRepository
{
    /// <summary>Adiciona um novo schema de data contract.</summary>
    Task AddAsync(DataContractSchema schema, CancellationToken ct);

    /// <summary>Obtém o schema mais recente para um dado ApiAsset.</summary>
    Task<DataContractSchema?> GetLatestByApiAssetAsync(Guid apiAssetId, string tenantId, CancellationToken ct);

    /// <summary>Lista schemas por ApiAsset, ordenados do mais recente para o mais antigo.</summary>
    Task<(IReadOnlyList<DataContractSchema> Items, int TotalCount)> ListByApiAssetAsync(
        Guid apiAssetId, string tenantId, int page, int pageSize, CancellationToken ct);
}
