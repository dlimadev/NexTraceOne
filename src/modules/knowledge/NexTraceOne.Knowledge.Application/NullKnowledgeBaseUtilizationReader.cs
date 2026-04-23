using NexTraceOne.Knowledge.Application.Abstractions;

namespace NexTraceOne.Knowledge.Application;

/// <summary>
/// Implementação null (honest-null) de IKnowledgeBaseUtilizationReader.
/// Retorna dados vazios — sem eventos de utilização do knowledge hub disponíveis.
/// Wave AY.2 — GetKnowledgeBaseUtilizationReport.
/// </summary>
public sealed class NullKnowledgeBaseUtilizationReader : IKnowledgeBaseUtilizationReader
{
    public Task<IKnowledgeBaseUtilizationReader.KnowledgeBaseUtilizationData>
        ReadByTenantAsync(string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => Task.FromResult(new IKnowledgeBaseUtilizationReader.KnowledgeBaseUtilizationData(
            SearchTerms: [],
            AccessedDocuments: [],
            AccessedRunbooks: [],
            DailyActiveKnowledgeUsers: 0,
            TotalSearchSessions: 0,
            SessionsWithResultClick: 0));
}
