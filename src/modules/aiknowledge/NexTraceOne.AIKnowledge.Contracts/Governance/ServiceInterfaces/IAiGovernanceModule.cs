namespace NexTraceOne.AIKnowledge.Contracts.Governance.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo AI Governance.
/// Expõe métricas de uso de tokens e atribuição de modelo para consumo por
/// outros bounded contexts (ex: Orchestration para enriquecer ConversationSummaryDto).
/// </summary>
public interface IAiGovernanceModule
{
    /// <summary>
    /// Obtém o total de tokens e o modelo predominante usado num determinado contexto de execução.
    /// Retorna null quando não há registo de uso de tokens para o execution ID.
    /// </summary>
    Task<TokenUsageSummaryDto?> GetTokenUsageByExecutionIdAsync(
        string executionId,
        CancellationToken ct = default);
}

/// <summary>
/// Resumo de uso de tokens para um determinado execution/conversation ID.
/// </summary>
public sealed record TokenUsageSummaryDto(
    int TotalTokens,
    string? ModelName,
    decimal? EstimatedCostUsd);
