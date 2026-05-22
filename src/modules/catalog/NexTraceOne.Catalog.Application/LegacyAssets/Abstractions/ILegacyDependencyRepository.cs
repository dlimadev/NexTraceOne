using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;

/// <summary>
/// Repositório de dependências entre ativos legacy.
/// Permite consultar relações de chamada, leitura e escrita entre programas COBOL,
/// transações, copybooks e outros ativos do mainframe.
/// </summary>
public interface ILegacyDependencyRepository
{
    Task<IReadOnlyList<LegacyDependency>> ListBySourceAsync(Guid sourceAssetId, CancellationToken ct);

    Task<IReadOnlyList<LegacyDependency>> ListByTargetAsync(Guid targetAssetId, CancellationToken ct);

    Task<IReadOnlyList<LegacyDependency>> ListBySourceTypeAsync(MainframeAssetType sourceType, CancellationToken ct);

    Task AddAsync(LegacyDependency dependency, CancellationToken ct);
}
