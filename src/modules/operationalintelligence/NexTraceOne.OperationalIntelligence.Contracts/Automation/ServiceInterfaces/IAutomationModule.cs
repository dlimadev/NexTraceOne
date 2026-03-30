namespace NexTraceOne.OperationalIntelligence.Contracts.Automation.ServiceInterfaces;

// IMPLEMENTATION STATUS: Implemented — AutomationModuleService registered in Automation/DependencyInjection.cs.

/// <summary>
/// Interface pública do módulo Automation.
/// Outros módulos que precisarem de dados de automação devem usar este contrato —
/// nunca acessar o DbContext ou repositórios diretamente.
/// Garante isolamento de base de dados entre serviços.
/// </summary>
public interface IAutomationModule
{
    /// <summary>
    /// Obtém o status do workflow de automação pelo identificador.
    /// Retorna null se o workflow não existir.
    /// </summary>
    Task<string?> GetWorkflowStatusAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista workflows de automação ativos (não concluídos) para um serviço.
    /// </summary>
    Task<IReadOnlyList<AutomationWorkflowSummary>> GetActiveWorkflowsAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se existem workflows de automação pendentes que bloqueiam uma mudança.
    /// Utilizado pelo módulo ChangeGovernance para validação de promoção.
    /// </summary>
    Task<bool> HasBlockingWorkflowsAsync(string serviceName, string environment, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resumo de um workflow de automação para consumo por outros módulos.
/// Contém os dados essenciais do workflow sem expor detalhes internos do domínio.
/// </summary>
public sealed record AutomationWorkflowSummary(
    string WorkflowId,
    string ServiceName,
    string WorkflowStatus,
    string ActionType,
    DateTimeOffset CreatedAt);
