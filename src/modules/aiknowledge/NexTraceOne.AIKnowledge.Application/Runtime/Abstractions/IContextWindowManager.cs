namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de gestão da context window de modelos de IA.
/// Aplica sliding window quando o histórico de mensagens excede o limite do modelo,
/// preservando sempre o system prompt e as mensagens mais recentes.
/// </summary>
public interface IContextWindowManager
{
    /// <summary>
    /// Reduz a lista de mensagens para caber no context window do modelo.
    /// Aplica sliding window: mantém system prompt e as mensagens mais recentes.
    /// </summary>
    (IReadOnlyList<ChatMessage> Messages, bool WasTruncated) TrimToFit(
        IReadOnlyList<ChatMessage> messages,
        int maxContextTokens,
        int reserveForCompletion = 1024);

    /// <summary>
    /// Conta tokens de uma mensagem (inclui overhead de role/formato).
    /// </summary>
    int EstimateTokens(ChatMessage message);

    /// <summary>
    /// Conta tokens de um texto.
    /// </summary>
    int EstimateTokens(string text);

    /// <summary>
    /// Estima o total de tokens de uma lista de mensagens.
    /// </summary>
    int EstimateTotalTokens(IReadOnlyList<ChatMessage> messages);
}
