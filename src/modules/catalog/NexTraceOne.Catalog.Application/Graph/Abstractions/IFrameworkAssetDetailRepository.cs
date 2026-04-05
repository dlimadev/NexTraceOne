using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>Repositório para detalhes de Framework/SDK.</summary>
public interface IFrameworkAssetDetailRepository
{
    /// <summary>Obtém o detalhe de framework associado ao serviço.</summary>
    Task<FrameworkAssetDetail?> GetByServiceAssetIdAsync(ServiceAssetId serviceAssetId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo detalhe de framework para persistência.</summary>
    void Add(FrameworkAssetDetail detail);
}
