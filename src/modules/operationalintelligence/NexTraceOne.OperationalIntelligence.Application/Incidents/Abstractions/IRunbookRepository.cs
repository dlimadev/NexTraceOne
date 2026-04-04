using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Repositório de runbooks operacionais — contrato do domínio para persistência
/// e consulta de RunbookRecord. Separa runbooks da interface geral IIncidentStore.
/// </summary>
public interface IRunbookRepository
{
    /// <summary>
    /// Retorna todos os runbooks, com filtros opcionais por serviço, tipo de incidente
    /// e pesquisa textual no título/descrição. Ordenados por data de publicação descendente.
    /// </summary>
    Task<IReadOnlyList<RunbookRecord>> ListAsync(
        string? linkedService,
        string? linkedIncidentType,
        string? search,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna o runbook com o identificador indicado, ou null se não existir.</summary>
    Task<RunbookRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persiste um novo runbook e confirma a transação.</summary>
    Task AddAsync(RunbookRecord runbook, CancellationToken cancellationToken = default);

    /// <summary>Atualiza um runbook existente e confirma a transação.</summary>
    Task UpdateAsync(RunbookRecord runbook, CancellationToken cancellationToken = default);
}
