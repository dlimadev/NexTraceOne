namespace NexTraceOne.ExternalAi.Domain.Ports;

/// <summary>
/// Porta de roteamento para provedores de IA externa.
/// Define o contrato para envio de consultas a serviços de IA (OpenAI, Azure OpenAI, etc.).
/// Preparada para futura extração como AI Gateway independente.
/// </summary>
public interface IExternalAIRoutingPort
{
    /// <summary>
    /// Roteia uma consulta para o provedor de IA mais adequado.
    /// </summary>
    Task<string> RouteQueryAsync(string context, string query, string? preferredProvider = null, CancellationToken cancellationToken = default);
}
