using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para execuções de playbook.
/// Isolamento total: acessa apenas RuntimeIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class PlaybookExecutionRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<PlaybookExecution, PlaybookExecutionId>(context), IPlaybookExecutionRepository
{
    /// <summary>Obtém uma execução pelo identificador.</summary>
    public override async Task<PlaybookExecution?> GetByIdAsync(PlaybookExecutionId id, CancellationToken ct = default)
        => await context.PlaybookExecutions
            .SingleOrDefaultAsync(e => e.Id == id, ct);

    /// <summary>Lista execuções de um playbook específico, ordenadas por data de início descendente.</summary>
    public async Task<IReadOnlyList<PlaybookExecution>> ListByPlaybookAsync(Guid playbookId, CancellationToken cancellationToken)
        => await context.PlaybookExecutions
            .Where(e => e.PlaybookId == playbookId)
            .OrderByDescending(e => e.StartedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <summary>Adiciona uma nova execução.</summary>
    public async Task AddAsync(PlaybookExecution execution, CancellationToken cancellationToken)
        => await context.PlaybookExecutions.AddAsync(execution, cancellationToken);
}
