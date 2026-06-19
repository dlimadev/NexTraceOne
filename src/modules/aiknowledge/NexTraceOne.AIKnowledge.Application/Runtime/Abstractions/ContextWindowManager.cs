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
            // System prompt itself exceeds window — truncate system messages to fit
            // while preserving at least the most recent user message, but only if there
            // is enough budget for a meaningful truncated system prompt.
            var systemBudget = (int)(available * 0.6);
            var lastUser = nonSystemMessages.LastOrDefault();

            if (systemBudget >= MessageOverheadTokens + 20)
            {
                var truncatedSystem = TruncateSystemMessages(systemMessages, systemBudget);
                var minimal = new List<ChatMessage>(truncatedSystem);
                if (lastUser is not null) minimal.Add(lastUser);
                return (minimal, true);
            }

            // Budget is too small to fit even a truncated system prompt safely.
            var fallback = lastUser is not null
                ? new List<ChatMessage> { lastUser }
                : new List<ChatMessage>();
            return (fallback, true);
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

            selected.Add(msg);
            tokenCount += tokens;
        }

        selected.Reverse();
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

    private List<ChatMessage> TruncateSystemMessages(IReadOnlyList<ChatMessage> systemMessages, int maxTokens)
    {
        var result = new List<ChatMessage>();
        var usedTokens = 0;
        var budgetPerMessage = maxTokens / Math.Max(systemMessages.Count, 1);

        foreach (var msg in systemMessages)
        {
            var msgTokens = EstimateTokens(msg);
            if (usedTokens + msgTokens <= maxTokens)
            {
                result.Add(msg);
                usedTokens += msgTokens;
                continue;
            }

            var remaining = maxTokens - usedTokens;
            if (remaining <= MessageOverheadTokens + 20)
                break;

            // Approximate chars per token from current message
            var contentTokens = Math.Max(msgTokens - MessageOverheadTokens, 1);
            var charsPerToken = (double)msg.Content.Length / contentTokens;
            var maxChars = Math.Max(50, (int)((remaining - MessageOverheadTokens) * charsPerToken));
            var truncated = maxChars < msg.Content.Length
                ? string.Concat(msg.Content.AsSpan(0, maxChars), "... [truncated]")
                : msg.Content;

            var truncatedMsg = new ChatMessage(msg.Role, truncated);
            result.Add(truncatedMsg);
            break;
        }

        return result;
    }
}
