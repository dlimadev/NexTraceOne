namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de contagem de tokens para estimativa precisa de context window.
/// Implementado via Microsoft.ML.Tokenizers para suporte a múltiplos esquemas de tokenização.
/// </summary>
public interface ITokenCounterService
{
    /// <summary>
    /// Conta tokens em um texto usando o tokenizer padrão (cl100k_base — mesmo do GPT-4/Tiktoken).
    /// </summary>
    int CountTokens(string text);

    /// <summary>
    /// Conta tokens em um texto para um modelo específico (quando suportado).
    /// </summary>
    int CountTokens(string text, string modelName);

    /// <summary>
    /// Trunca um texto para um número máximo de tokens.
    /// </summary>
    string TruncateToTokens(string text, int maxTokens);
}
