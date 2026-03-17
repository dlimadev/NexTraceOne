namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Ports;

/// <summary>
/// Porta de decisão sobre deployments.
/// Define o contrato para avaliação e aprovação/rejeição de deployments com base em regras de governança.
/// Preparada para futura extração como Deployment Orchestrator independente.
/// </summary>
public interface IDeploymentDecisionPort
{
    /// <summary>
    /// Avalia se um deployment pode prosseguir com base nas regras de governança vigentes.
    /// </summary>
    Task<bool> EvaluateDeploymentAsync(Guid releaseId, string environment, CancellationToken cancellationToken = default);
}
