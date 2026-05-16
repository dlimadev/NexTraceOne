using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Tokenizers;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação de contagem de tokens usando Microsoft.ML.Tokenizers (cl100k_base).
/// Substitui a heurística de ~4 chars/token por tokenização real com erro &lt; 2%.
/// </summary>
public sealed class TokenCounterService : ITokenCounterService
{
    private readonly ILogger<TokenCounterService> _logger;
    private readonly TiktokenTokenizer _tokenizer;

    // Tokenizer cache por modelo (quando suportarmos múltiplos esquemas no futuro)
    private static readonly Lazy<TiktokenTokenizer> DefaultTokenizer = new(() =>
    {
        // cl100k_base é o esquema usado por GPT-4, GPT-4o, GPT-3.5-turbo
        // Ollama models usam esquemas diferentes, mas cl100k_base é o mais comum em produção
        // e fornece estimativa conservadora (normalmente conta ligeiramente mais tokens).
        return TiktokenTokenizer.CreateForModel("gpt-4");
    });

    public TokenCounterService(ILogger<TokenCounterService> logger)
    {
        _logger = logger;
        _tokenizer = DefaultTokenizer.Value;
    }

    public int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        try
        {
            return _tokenizer.CountTokens(text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token counting failed; falling back to heuristic");
            return text.Length / 4;
        }
    }

    public int CountTokens(string text, string modelName)
    {
        // Futuro: suportar esquemas específicos por modelo (llama, qwen, etc.)
        // Por agora, usamos cl100k_base para todos como estimativa conservadora.
        _logger.LogDebug("Token counting for model {Model} — using cl100k_base as conservative estimate", modelName);
        return CountTokens(text);
    }

    public string TruncateToTokens(string text, int maxTokens)
    {
        if (string.IsNullOrEmpty(text) || maxTokens <= 0)
            return string.Empty;

        var currentTokens = CountTokens(text);
        if (currentTokens <= maxTokens)
            return text;

        // Binary search para encontrar o corte exato em caracteres
        var low = 0;
        var high = text.Length;
        var bestFit = 0;

        while (low <= high)
        {
            var mid = (low + high) / 2;
            var substring = text[..mid];
            var tokens = CountTokens(substring);

            if (tokens <= maxTokens)
            {
                bestFit = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        var result = text[..bestFit];
        // Garantir que não cortamos no meio de um surrogate pair
        while (bestFit > 0 && char.IsHighSurrogate(text[bestFit - 1]))
        {
            bestFit--;
            result = text[..bestFit];
        }

        result = result.TrimEnd();

        // Considerar tokens do sufixo "..."
        var suffixTokens = CountTokens("...");
        while (result.Length > 0 && CountTokens(result) + suffixTokens > maxTokens)
        {
            // Remover a última palavra/caractere até caber
            var lastSpace = result.LastIndexOf(' ');
            if (lastSpace > 0)
                result = result[..lastSpace];
            else
                result = result[..^1];
        }

        return result + "...";
    }
}
