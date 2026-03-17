namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Ports;

/// <summary>
/// Porta de recebimento de eventos de deployment de plataformas CI/CD.
/// Define o contrato para ingestão de notificações de deploy (GitHub, GitLab, Jenkins, Azure DevOps).
/// Preparada para futura extração como Deployment Orchestrator independente.
/// </summary>
public interface IDeploymentEventPort
{
    /// <summary>
    /// Registra um evento de deployment recebido de uma plataforma externa.
    /// </summary>
    Task RegisterDeploymentEventAsync(string sourceSystem, string environment, string version, string payload, CancellationToken cancellationToken = default);
}
