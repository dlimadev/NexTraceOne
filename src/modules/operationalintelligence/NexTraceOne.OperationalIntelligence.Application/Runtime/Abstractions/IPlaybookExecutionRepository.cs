using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>Contrato do repositório para execuções de playbook.</summary>
public interface IPlaybookExecutionRepository
{
    /// <summary>Obtém uma execução pelo identificador.</summary>
    Task<PlaybookExecution?> GetByIdAsync(PlaybookExecutionId id, CancellationToken cancellationToken);

    /// <summary>Lista execuções de um playbook específico.</summary>
    Task<IReadOnlyList<PlaybookExecution>> ListByPlaybookAsync(Guid playbookId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova execução.</summary>
    Task AddAsync(PlaybookExecution execution, CancellationToken cancellationToken);
}
