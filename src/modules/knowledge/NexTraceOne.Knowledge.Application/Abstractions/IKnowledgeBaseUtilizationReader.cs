namespace NexTraceOne.Knowledge.Application.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de utilização do knowledge hub.
/// Por omissão satisfeita por <c>NullKnowledgeBaseUtilizationReader</c> (honest-null).
/// Wave AY.2 — GetKnowledgeBaseUtilizationReport.
/// </summary>
public interface IKnowledgeBaseUtilizationReader
{
    Task<KnowledgeBaseUtilizationData> ReadByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Dados de utilização do knowledge hub para o período.</summary>
    public sealed record KnowledgeBaseUtilizationData(
        IReadOnlyList<SearchTermEntry> SearchTerms,
        IReadOnlyList<DocumentAccessEntry> AccessedDocuments,
        IReadOnlyList<RunbookAccessEntry> AccessedRunbooks,
        int DailyActiveKnowledgeUsers,
        int TotalSearchSessions,
        int SessionsWithResultClick);

    /// <summary>Entrada de termo de pesquisa.</summary>
    public sealed record SearchTermEntry(
        string Term,
        int SearchCount,
        int ResultCount,
        int ClickCount);

    /// <summary>Entrada de acesso a documento.</summary>
    public sealed record DocumentAccessEntry(
        string DocumentId,
        string Title,
        string Category,
        int AccessCount);

    /// <summary>Entrada de acesso a runbook.</summary>
    public sealed record RunbookAccessEntry(
        string ServiceId,
        string ServiceName,
        int AccessCount);
}
