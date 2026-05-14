namespace NexTraceOne.AIKnowledge.Infrastructure.Services.AIAgents;

/// <summary>
/// Interface para provedores de LLM (Large Language Models).
/// Suporta integração com OpenAI, Anthropic Claude, e outros providers.
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Gera uma resposta baseada no prompt fornecido.
    /// </summary>
    Task<string> GenerateAsync(string prompt);

    /// <summary>
    /// Gera respostas em batch para múltiplos prompts.
    /// </summary>
    Task<List<string>> BatchGenerateAsync(List<string> prompts);

    /// <summary>
    /// Verifica se o provider está configurado corretamente.
    /// </summary>
    bool IsConfigured();
}