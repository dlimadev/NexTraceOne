using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Application.Abstractions;

/// <summary>Contrato de repositório para a entidade Release.</summary>
public interface IReleaseRepository
{
    /// <summary>Busca uma Release pelo seu identificador.</summary>
    Task<Release?> GetByIdAsync(ReleaseId id, CancellationToken cancellationToken = default);

    /// <summary>Busca releases de um ativo de API por versão.</summary>
    Task<Release?> GetByApiAssetAndVersionAsync(Guid apiAssetId, string version, string environment, CancellationToken cancellationToken = default);

    /// <summary>Lista releases de um ativo de API ordenadas por data de criação descendente.</summary>
    Task<IReadOnlyList<Release>> ListByApiAssetAsync(Guid apiAssetId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova Release ao repositório.</summary>
    void Add(Release release);
}
