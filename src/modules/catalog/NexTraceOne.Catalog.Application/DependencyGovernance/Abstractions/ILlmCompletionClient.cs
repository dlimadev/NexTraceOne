namespace NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;

/// <summary>
/// Cliente abstrato para completão de texto via LLM (Ollama, OpenAI, etc).
/// </summary>
public interface ILlmCompletionClient
{
    /// <summary>
    /// Envia um prompt e retorna a resposta de texto.
    /// </summary>
    Task<string?> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
