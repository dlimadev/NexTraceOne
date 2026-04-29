namespace NexTraceOne.Integrations.Domain;

/// <summary>
/// Contrato para integração com motores de chaos engineering (Litmus, Chaos Mesh, Gremlin, …).
/// A implementação padrão é <c>NullChaosProvider</c> que retorna estado simulado até que um motor real esteja configurado.
/// DEG-04 — Chaos Engineering.
/// </summary>
public interface IChaosProvider
{
    bool IsConfigured { get; }

    Task<ChaosExperimentResult> SubmitExperimentAsync(
        ChaosExperimentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChaosExperimentStatus>> ListRunningExperimentsAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Pedido de criação de um experimento de chaos.</summary>
public sealed record ChaosExperimentRequest(
    string Name,
    string TargetService,
    string FaultType,
    int DurationSeconds,
    string? TenantId);

/// <summary>Resultado do submit de um experimento de chaos.</summary>
public sealed record ChaosExperimentResult(
    string ExperimentId,
    string Status,
    bool IsSimulated,
    string? SimulatedNote);

/// <summary>Estado de um experimento de chaos em execução.</summary>
public sealed record ChaosExperimentStatus(
    string ExperimentId,
    string Name,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);
