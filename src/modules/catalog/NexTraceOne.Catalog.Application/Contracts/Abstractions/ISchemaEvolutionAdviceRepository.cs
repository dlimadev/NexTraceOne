using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para análises de evolução de schema de contratos.
/// </summary>
public interface ISchemaEvolutionAdviceRepository
{
    /// <summary>Obtém uma análise de evolução de schema por identificador.</summary>
    Task<SchemaEvolutionAdvice?> GetByIdAsync(SchemaEvolutionAdviceId id, CancellationToken cancellationToken);

    /// <summary>Obtém a análise mais recente de um contrato (API Asset).</summary>
    Task<SchemaEvolutionAdvice?> GetLatestByApiAssetAsync(Guid apiAssetId, CancellationToken cancellationToken);

    /// <summary>Lista análises de evolução de schema, opcionalmente filtradas por API Asset.</summary>
    Task<IReadOnlyList<SchemaEvolutionAdvice>> ListByApiAssetAsync(Guid? apiAssetId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova análise de evolução de schema.</summary>
    Task AddAsync(SchemaEvolutionAdvice advice, CancellationToken cancellationToken);
}
