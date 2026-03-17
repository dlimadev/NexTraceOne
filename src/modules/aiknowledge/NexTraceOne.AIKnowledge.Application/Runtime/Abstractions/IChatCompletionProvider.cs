namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Contrato para execução de chat/completion em provedores de IA.
/// Implementado por cada provider concreto (OllamaChatProvider, OpenAiChatProvider, etc.).
/// </summary>
public interface IChatCompletionProvider
{
    /// <summary>Identificador do provider associado.</summary>
    string ProviderId { get; }

    /// <summary>Executa uma inferência de chat/completion.</summary>
    Task<ChatCompletionResult> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Request para execução de chat/completion.</summary>
public sealed record ChatCompletionRequest(
    string ModelId,
    IReadOnlyList<ChatMessage> Messages,
    double? Temperature = null,
    int? MaxTokens = null,
    string? SystemPrompt = null);

/// <summary>Mensagem individual no contexto de uma conversa.</summary>
public sealed record ChatMessage(
    string Role,
    string Content);

/// <summary>Resultado de uma execução de chat/completion.</summary>
public sealed record ChatCompletionResult(
    bool Success,
    string? Content,
    string ModelId,
    string ProviderId,
    int PromptTokens,
    int CompletionTokens,
    TimeSpan Duration,
    string? ErrorMessage = null);
