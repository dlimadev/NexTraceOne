using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiUsageDashboard;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de entradas de auditoria de uso de IA.
/// Suporta listagem filtrada para a trilha de auditoria e registo de uso.
/// </summary>
public interface IAiUsageEntryRepository
{
    /// <summary>Lista entradas de auditoria com filtros compostos e limite de resultados.</summary>
    Task<IReadOnlyList<AIUsageEntry>> ListAsync(
        string? userId,
        Guid? modelId,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        UsageResult? result,
        AIClientType? clientType,
        int pageSize,
        CancellationToken ct);

    /// <summary>Adiciona uma nova entrada de auditoria para persistência.</summary>
    Task AddAsync(AIUsageEntry entry, CancellationToken ct);

    /// <summary>
    /// Elimina entradas de auditoria com Timestamp anterior à data limite.
    /// Usado pelo AiDataRetentionJob para aplicar política de retenção de dados.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct);

    /// <summary>
    /// Agrega uso de IA por dimensão (model, user, provider) num período.
    /// Retorna totais de tokens, requests e custo estimado agrupados pela dimensão solicitada.
    /// </summary>
    Task<IReadOnlyList<AiUsageAggregate>> GetAggregatedUsageAsync(
        Guid tenantId,
        DateTimeOffset start,
        DateTimeOffset end,
        string groupBy,
        int top,
        CancellationToken ct);
}
