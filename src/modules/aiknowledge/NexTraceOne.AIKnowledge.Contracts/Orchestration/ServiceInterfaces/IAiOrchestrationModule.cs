namespace NexTraceOne.AIKnowledge.Contracts.Orchestration.ServiceInterfaces;

// IMPLEMENTATION STATUS: Complete — implemented by AiOrchestrationModule, registered in DI, covered by AiOrchestrationModuleTests.

/// <summary>
/// Interface pública do módulo AiOrchestration.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IAiOrchestrationModule
{
    /// <summary>
    /// Obtém metadados de uma conversa por ID.
    /// Retorna null quando a conversa não existe.
    /// </summary>
    Task<ConversationSummaryDto?> GetConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Obtém conversas recentes por serviço.
    /// </summary>
    Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsByServiceAsync(
        string serviceName,
        int limit = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Obtém resumo de execução de agente por ID.
    /// Retorna null quando não há resultado correspondente.
    /// </summary>
    Task<AgentExecutionResultDto?> GetAgentExecutionResultAsync(Guid executionId, CancellationToken ct = default);
}
