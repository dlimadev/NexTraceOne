using NexTraceOne.CommercialCatalog.Domain.Entities;

namespace NexTraceOne.CommercialCatalog.Application.Abstractions;

/// <summary>
/// Repositório de pacotes de funcionalidades do subdomínio CommercialCatalog.
/// Operações de leitura e escrita para o aggregate FeaturePack.
/// </summary>
public interface IFeaturePackRepository
{
    /// <summary>Obtém um pacote pelo identificador, incluindo seus itens.</summary>
    Task<FeaturePack?> GetByIdAsync(FeaturePackId id, CancellationToken cancellationToken = default);

    /// <summary>Obtém um pacote pelo código único, incluindo seus itens.</summary>
    Task<FeaturePack?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista pacotes com filtro opcional por estado ativo.
    /// Se activeOnly for null, retorna todos os pacotes.
    /// Inclui os itens de cada pacote na projeção.
    /// </summary>
    Task<IReadOnlyList<FeaturePack>> ListAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo pacote para persistência.</summary>
    Task AddAsync(FeaturePack featurePack, CancellationToken cancellationToken = default);
}
