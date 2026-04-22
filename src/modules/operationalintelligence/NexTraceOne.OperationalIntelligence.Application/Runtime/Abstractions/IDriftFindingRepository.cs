using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade DriftFinding.
/// Provê operações de leitura e escrita para findings de drift detectados entre baselines e snapshots.
/// </summary>
public interface IDriftFindingRepository
{
    /// <summary>Busca um drift finding pelo seu identificador.</summary>
    Task<DriftFinding?> GetByIdAsync(DriftFindingId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista drift findings de um serviço e ambiente, ordenados por data de detecção descendente.
    /// Suporta paginação via page e pageSize.
    /// </summary>
    Task<IReadOnlyList<DriftFinding>> ListByServiceAsync(
        string serviceName,
        string environment,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista drift findings que ainda não foram reconhecidos pela equipe,
    /// ordenados por severidade descendente e data de detecção descendente.
    /// Suporta paginação via page e pageSize.
    /// </summary>
    Task<IReadOnlyList<DriftFinding>> ListUnacknowledgedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todos os drift findings no tenant num período temporal (para relatórios de anomalias).
    /// Inclui findings reconhecidos e não-reconhecidos, ordenados por data de detecção descendente.
    /// </summary>
    Task<IReadOnlyList<DriftFinding>> ListByTenantInPeriodAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo drift finding ao repositório.</summary>
    void Add(DriftFinding finding);
}
