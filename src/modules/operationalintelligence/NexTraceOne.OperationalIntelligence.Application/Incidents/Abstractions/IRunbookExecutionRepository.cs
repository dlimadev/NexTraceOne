using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Repositório de execuções de passos de runbook — contrato do domínio para persistência
/// e consulta de RunbookStepExecution. Separado do IRunbookRepository para preservar coesão.
/// </summary>
public interface IRunbookExecutionRepository
{
    /// <summary>Persiste uma nova execução de passo de runbook.</summary>
    Task AddAsync(RunbookStepExecution execution, CancellationToken ct);

    /// <summary>Retorna todas as execuções registadas para um runbook específico.</summary>
    Task<IReadOnlyList<RunbookStepExecution>> GetByRunbookAsync(Guid runbookId, CancellationToken ct);
}
