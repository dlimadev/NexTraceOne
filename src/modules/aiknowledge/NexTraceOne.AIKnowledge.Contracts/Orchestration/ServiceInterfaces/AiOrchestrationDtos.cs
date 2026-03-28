namespace NexTraceOne.AIKnowledge.Contracts.Orchestration.ServiceInterfaces;

/// <summary>
/// Resumo cross-module de conversa de orquestração de IA.
/// Expõe apenas metadados, sem conteúdo de turnos.
/// </summary>
public sealed record ConversationSummaryDto(
    Guid Id,
    string Title,
    string OwnerUserId,
    string ServiceName,
    int TurnCount,
    int TotalTokens,
    string? ModelUsed,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Resumo cross-module de execução de agente de IA.
/// </summary>
public sealed record AgentExecutionResultDto(
    Guid Id,
    string AgentType,
    string ServiceName,
    string Status,
    string Summary,
    DateTimeOffset CreatedAt,
    int TokensUsed);
