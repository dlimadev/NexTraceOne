using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Repositório para persistência e consulta de RecoveryJob.
/// </summary>
public interface IRecoveryJobRepository
{
    /// <summary>Lista todos os jobs de recovery, ordenados do mais recente para o mais antigo.</summary>
    Task<IReadOnlyList<RecoveryJob>> ListAsync(int limit, CancellationToken ct);

    /// <summary>Obtém um job por ID.</summary>
    Task<RecoveryJob?> GetByIdAsync(RecoveryJobId id, CancellationToken ct);

    /// <summary>Persiste um novo job de recovery.</summary>
    Task AddAsync(RecoveryJob job, CancellationToken ct);

    /// <summary>Atualiza um job existente.</summary>
    void Update(RecoveryJob job);
}
