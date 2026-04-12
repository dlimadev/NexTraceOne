using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de execuções de pipeline de geração de código a partir de contratos.
/// Persiste e consulta execuções para rastreabilidade do Contract-to-Code Pipeline.
/// </summary>
internal sealed class PipelineExecutionRepository(ContractsDbContext context)
    : IPipelineExecutionRepository
{
    /// <inheritdoc />
    public async Task<PipelineExecution?> GetByIdAsync(PipelineExecutionId id, CancellationToken cancellationToken)
        => await context.PipelineExecutions
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PipelineExecution>> ListAsync(Guid? apiAssetId, CancellationToken cancellationToken)
    {
        var query = context.PipelineExecutions.AsNoTracking();

        if (apiAssetId.HasValue)
            query = query.Where(x => x.ApiAssetId == apiAssetId.Value);

        return await query
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(PipelineExecution execution, CancellationToken cancellationToken)
        => await context.PipelineExecutions.AddAsync(execution, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(PipelineExecution execution, CancellationToken cancellationToken)
    {
        context.PipelineExecutions.Update(execution);
        return Task.CompletedTask;
    }
}
