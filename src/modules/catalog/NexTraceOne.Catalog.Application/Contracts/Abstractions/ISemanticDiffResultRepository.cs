using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para resultados de diff semântico assistido por IA entre versões de contrato.
/// </summary>
public interface ISemanticDiffResultRepository
{
    /// <summary>Obtém um resultado de diff semântico por identificador.</summary>
    Task<SemanticDiffResult?> GetByIdAsync(SemanticDiffResultId id, CancellationToken cancellationToken);

    /// <summary>Obtém o resultado de diff semântico para um par específico de versões.</summary>
    Task<SemanticDiffResult?> GetByVersionPairAsync(string contractVersionFromId, string contractVersionToId, CancellationToken cancellationToken);

    /// <summary>Lista resultados de diff semântico que envolvam uma versão de contrato (como origem ou destino).</summary>
    Task<IReadOnlyList<SemanticDiffResult>> ListByContractVersionAsync(string contractVersionId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo resultado de diff semântico.</summary>
    Task AddAsync(SemanticDiffResult result, CancellationToken cancellationToken);
}
