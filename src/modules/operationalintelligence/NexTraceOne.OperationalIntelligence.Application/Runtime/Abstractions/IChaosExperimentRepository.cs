using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>Contrato do repositório para experimentos de chaos engineering.</summary>
public interface IChaosExperimentRepository
{
    Task<ChaosExperiment?> GetByIdAsync(ChaosExperimentId id, string tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChaosExperiment>> ListAsync(
        string tenantId,
        string? serviceName,
        string? environment,
        ExperimentStatus? status,
        CancellationToken cancellationToken);

    Task AddAsync(ChaosExperiment experiment, CancellationToken cancellationToken);

    Task UpdateAsync(ChaosExperiment experiment, CancellationToken cancellationToken);
}
