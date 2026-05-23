namespace NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;

/// <summary>
/// Serviço de replaneamento adaptativo para workflows multi-agent.
/// Quando um step falha, tenta gerar um novo plano para os steps restantes.
/// </summary>
public interface IWorkflowReplanningService
{
    /// <summary>
    /// Tenta replanear os steps restantes de um workflow após uma falha.
    /// </summary>
    /// <param name="originalWorkflow">Workflow original.</param>
    /// <param name="completedSteps">Steps que já foram executados com sucesso.</param>
    /// <param name="failedStep">Step que falhou.</param>
    /// <param name="errorMessage">Mensagem de erro da falha.</param>
    /// <param name="currentOutput">Output acumulado até o momento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Nova definição de workflow com os steps replanejados, ou null se não for possível.</returns>
    Task<AgentWorkflowDefinition?> ReplanAsync(
        AgentWorkflowDefinition originalWorkflow,
        IReadOnlyList<AgentWorkflowStepResult> completedSteps,
        AgentWorkflowStep failedStep,
        string errorMessage,
        string currentOutput,
        CancellationToken cancellationToken = default);
}
