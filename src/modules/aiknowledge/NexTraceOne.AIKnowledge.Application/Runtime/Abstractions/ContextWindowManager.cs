namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Implementação de gestão da context window de modelos de IA.
/// Usa ITokenCounterService para estimativa precisa de tokens (em vez de heurística 4 chars/token).
/// Aplica sliding window quando o histórico excede o limite, preservando system prompt e mensagens mais recentes.
/// </summary>
public sealed class ContextWindowManager : IContextWindowManager
{
    private readonly ITokenCounterService _tokenCounter;

    /// <summary>Overhead estimado de tokens por mensagem (role + formato JSON).</summary>
    private const int MessageOverheadTokens = 4;

    public ContextWindowManager(ITokenCounterService tokenCounter)
    {
        _tokenCounter = tokenCounter;
    }

    /// <summary>
    /// Reduz a lista de mensagens para caber no context window do modelo.
    /// Aplica sliding window: mantém system prompt e as mensagens mais recentes.
    /// </summary>
    public (IReadOnlyList<ChatMessage> Messages, bool WasTruncated) TrimToFit(
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
    /// Conta tokens de uma mensagem usando tokenizer real + overhead de formato.
    /// </summary>
    public int EstimateTokens(ChatMessage message)
        => _tokenCounter.CountTokens(message.Content) + MessageOverheadTokens;

    /// <summary>
    /// Conta tokens de um texto usando tokenizer real.
    /// </summary>
    public int EstimateTokens(string text)
        => string.IsNullOrEmpty(text) ? 0 : _tokenCounter.CountTokens(text);

    /// <summary>
    /// Estima o total de tokens de uma lista de mensagens.
    /// </summary>
    public int EstimateTotalTokens(IReadOnlyList<ChatMessage> messages)
        => messages.Sum(EstimateTokens);
}
