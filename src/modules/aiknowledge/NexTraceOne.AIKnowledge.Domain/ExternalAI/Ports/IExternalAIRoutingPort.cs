namespace NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;

/// <summary>
/// Porta de roteamento para provedores de IA externa.
/// Define o contrato para envio de consultas a serviços de IA (OpenAI, Azure OpenAI, etc.).
/// Preparada para futura extração como AI Gateway independente.
/// </summary>
public interface IExternalAIRoutingPort
{
    /// <summary>
    /// Roteia uma consulta para o provedor de IA mais adequado.
    /// <paramref name="capability"/> é verificado contra políticas ativas de ExternalAI antes do envio —
    /// se uma política activa bloquear o contexto, o pedido é rejeitado com fallback determinístico.
    /// <paramref name="environment"/> permite aplicar regras mais restritivas em produção.
    /// </summary>
    Task<string> RouteQueryAsync(
        string context,
        string query,
        string? preferredProvider = null,
        string? capability = null,
        string? environment = null,
        CancellationToken cancellationToken = default);
}
