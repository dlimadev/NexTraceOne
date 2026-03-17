using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade RuntimeSnapshot.
/// Provê operações de leitura e escrita para snapshots de saúde e performance de serviços em runtime.
/// </summary>
public interface IRuntimeSnapshotRepository
{
    /// <summary>Busca um snapshot de runtime pelo seu identificador.</summary>
    Task<RuntimeSnapshot?> GetByIdAsync(RuntimeSnapshotId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista snapshots de runtime de um serviço e ambiente, ordenados por data de captura descendente.
    /// Suporta paginação via page e pageSize.
    /// </summary>
    Task<IReadOnlyList<RuntimeSnapshot>> ListByServiceAsync(
        string serviceName,
        string environment,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o snapshot mais recente de um serviço e ambiente.
    /// Retorna null se nenhum snapshot foi capturado para a combinação informada.
    /// </summary>
    Task<RuntimeSnapshot?> GetLatestByServiceAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo snapshot de runtime ao repositório.</summary>
    void Add(RuntimeSnapshot snapshot);
}
