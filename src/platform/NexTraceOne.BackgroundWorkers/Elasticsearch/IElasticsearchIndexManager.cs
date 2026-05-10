namespace NexTraceOne.BackgroundWorkers.Elasticsearch;

/// <summary>
/// Gere políticas ILM e templates de índice no cluster Elasticsearch.
/// W7-01: ES ILM auto-apply.
/// </summary>
public interface IElasticsearchIndexManager
{
    /// <summary>Aplica as políticas ILM configuradas ao cluster Elasticsearch.</summary>
    Task ApplyIlmPoliciesAsync(CancellationToken cancellationToken);

    /// <summary>Verifica se o cluster Elasticsearch está acessível.</summary>
    Task<bool> IsClusterHealthyAsync(CancellationToken cancellationToken);
}
