using NexTraceOne.Catalog.Domain.SourceOfTruth.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;

namespace NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;

/// <summary>
/// Abstração do repositório de referências vinculadas no Source of Truth.
/// </summary>
public interface ILinkedReferenceRepository
{
    /// <summary>Adiciona uma nova referência vinculada.</summary>
    void Add(LinkedReference reference);

    /// <summary>Busca uma referência pelo identificador.</summary>
    Task<LinkedReference?> GetByIdAsync(LinkedReferenceId id, CancellationToken ct = default);

    /// <summary>Lista referências vinculadas a um ativo específico.</summary>
    Task<IReadOnlyList<LinkedReference>> ListByAssetAsync(
        Guid assetId,
        LinkedAssetType assetType,
        CancellationToken ct = default);

    /// <summary>Lista referências de um tipo específico para um ativo.</summary>
    Task<IReadOnlyList<LinkedReference>> ListByAssetAndTypeAsync(
        Guid assetId,
        LinkedAssetType assetType,
        LinkedReferenceType referenceType,
        CancellationToken ct = default);

    /// <summary>Pesquisa referências por texto em título e descrição.</summary>
    Task<IReadOnlyList<LinkedReference>> SearchAsync(
        string searchTerm,
        LinkedReferenceType? referenceType,
        CancellationToken ct = default);
}
