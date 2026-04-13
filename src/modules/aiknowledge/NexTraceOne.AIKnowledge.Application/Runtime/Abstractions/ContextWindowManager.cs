namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Utilitário de gestão da context window de modelos de IA.
/// Aplica sliding window quando o histórico de mensagens excede o limite do modelo,
/// preservando sempre o system prompt e as mensagens mais recentes.
///
/// Estimativa de tokens: 4 chars ≈ 1 token (heurística BPE para texto em inglês/português).
/// Para produção, integrar com tokenizer real do provider quando disponível.
/// </summary>
public static class ContextWindowManager
{
    /// <summary>Número de chars estimados por token (heurística conservadora).</summary>
    private const int CharsPerToken = 4;

    /// <summary>
    /// Reduz a lista de mensagens para caber no context window do modelo.
    /// Aplica sliding window: mantém system prompt e as mensagens mais recentes.
    /// </summary>
    /// <param name="messages">Lista completa de mensagens (incluindo system).</param>
    /// <param name="maxContextTokens">Limite de tokens do modelo (ex: 4096).</param>
    /// <param name="reserveForCompletion">Tokens reservados para a resposta do modelo (default: 1024).</param>
    /// <returns>Lista de mensagens que cabe no context window disponível.</returns>
    public static (IReadOnlyList<ChatMessage> Messages, bool WasTruncated) TrimToFit(
        IReadOnlyList<ChatMessage> messages,
        int maxContextTokens,
        int reserveForCompletion = 1024)
    {
        if (messages.Count == 0)
            return (messages, false);

        var available = maxContextTokens - reserveForCompletion;
        if (available <= 0)
            available = maxContextTokens / 2;

        // Separate system message(s) — always preserved
        var systemMessages = messages
            .Where(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var nonSystemMessages = messages
            .Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var systemTokens = systemMessages.Sum(EstimateTokens);

        if (systemTokens >= available)
        {
            // System prompt itself exceeds window — return only last user message
            var lastUser = nonSystemMessages.LastOrDefault();
            var minimal = lastUser is not null
                ? new List<ChatMessage> { lastUser }
                : new List<ChatMessage>();
            return (minimal, true);
        }

        var remainingTokens = available - systemTokens;

        // Apply sliding window on non-system messages (most recent first)
        var selected = new List<ChatMessage>();
        var tokenCount = 0;

        for (var i = nonSystemMessages.Count - 1; i >= 0; i--)
        {
            var msg = nonSystemMessages[i];
            var tokens = EstimateTokens(msg);

            if (tokenCount + tokens > remainingTokens)
                break;

            selected.Insert(0, msg);
            tokenCount += tokens;
        }

        var wasTruncated = selected.Count < nonSystemMessages.Count;
        var result = new List<ChatMessage>(systemMessages.Count + selected.Count);
        result.AddRange(systemMessages);
        result.AddRange(selected);

        return (result, wasTruncated);
    }

    /// <summary>
    /// Estima o número de tokens de uma mensagem usando heurística de 4 chars/token.
    /// </summary>
    public static int EstimateTokens(ChatMessage message)
        => EstimateTokens(message.Content) + 4; // +4 for role/format overhead

    /// <summary>
    /// Estima o número de tokens de um texto.
    /// </summary>
    public static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return Math.Max(1, (text.Length + CharsPerToken - 1) / CharsPerToken);
    }

    /// <summary>
    /// Estima o total de tokens de uma lista de mensagens.
    /// </summary>
    public static int EstimateTotalTokens(IReadOnlyList<ChatMessage> messages)
        => messages.Sum(EstimateTokens);
}
