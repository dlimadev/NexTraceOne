using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.SearchLogs;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de pesquisa de logs estruturados.
/// Abstracção sobre Elasticsearch para o feature SearchLogs.
/// SaaS-07: Log Search UI.
/// </summary>
public interface ILogSearchService
{
    /// <summary>
    /// Pesquisa logs com os filtros especificados.
    /// Devolve tuplo (entradas, total).
    /// </summary>
    Task<(IReadOnlyList<SearchLogs.LogEntry> Entries, long Total)> SearchAsync(
        LogSearchRequest request,
        CancellationToken cancellationToken);
}

/// <summary>Parâmetros de pesquisa de logs.</summary>
public sealed record LogSearchRequest(
    Guid TenantId,
    string? ServiceName,
    string? Severity,
    string? Environment,
    string? SearchText,
    DateTimeOffset From,
    DateTimeOffset To,
    int Page,
    int PageSize);
