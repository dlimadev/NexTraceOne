using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Enums;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;

/// <summary>
/// Repositório de conversas multi-turno de IA do módulo de orquestração.
/// </summary>
public interface IAiOrchestrationConversationRepository
{
    /// <summary>Lista conversas com filtros opcionais e paginação.</summary>
    Task<(IReadOnlyList<AiConversation> Items, int Total)> ListHistoryAsync(
        Guid? releaseId,
        string? serviceName,
        string? topicFilter,
        ConversationStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>Lista as conversas mais recentes associadas a uma release.</summary>
    Task<IReadOnlyList<ConversationSummaryData>> GetRecentByReleaseAsync(
        Guid releaseId,
        int maxCount,
        CancellationToken ct);
}

/// <summary>Resumo de conversa de orquestração para contexto de release.</summary>
public sealed record ConversationSummaryData(
    string Topic,
    int TurnCount,
    string Status,
    string? Summary);
