using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para execuções de pipeline de geração de código a partir de contratos.
/// </summary>
public interface IPipelineExecutionRepository
{
    /// <summary>Obtém uma execução de pipeline pelo seu identificador.</summary>
    Task<PipelineExecution?> GetByIdAsync(PipelineExecutionId id, CancellationToken cancellationToken);

    /// <summary>Lista execuções de pipeline, opcionalmente filtradas por API Asset.</summary>
    Task<IReadOnlyList<PipelineExecution>> ListAsync(Guid? apiAssetId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova execução de pipeline.</summary>
    Task AddAsync(PipelineExecution execution, CancellationToken cancellationToken);

    /// <summary>Atualiza uma execução de pipeline existente.</summary>
    Task UpdateAsync(PipelineExecution execution, CancellationToken cancellationToken);
}
