using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure.Chaos;

/// <summary>
/// Implementação nula de IChaosProvider.
/// Retorna estado simulado enquanto nenhum motor de chaos (Litmus, Chaos Mesh, Gremlin) estiver configurado.
/// </summary>
internal sealed class NullChaosProvider : IChaosProvider
{
    public bool IsConfigured => false;

    public Task<ChaosExperimentResult> SubmitExperimentAsync(
        ChaosExperimentRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new ChaosExperimentResult(
            ExperimentId: Guid.NewGuid().ToString("N"),
            Status: "Pending",
            IsSimulated: true,
            SimulatedNote: "No chaos engine configured. Experiment was not submitted to any real system."));

    public Task<IReadOnlyList<ChaosExperimentStatus>> ListRunningExperimentsAsync(
        string? tenantId = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ChaosExperimentStatus>>([]);
}
