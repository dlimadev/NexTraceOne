namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Contrato para execução de chat/completion em provedores de IA.
/// Implementado por cada provider concreto (OllamaChatProvider, OpenAiChatProvider, etc.).
/// </summary>
public interface IChatCompletionProvider
{
    /// <summary>Identificador do provider associado.</summary>
    string ProviderId { get; }

    /// <summary>Executa uma inferência de chat/completion (one-shot).</summary>
    Task<ChatCompletionResult> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa uma inferência de chat/completion com streaming incremental.
    /// Cada chunk contém um fragmento de texto gerado pelo modelo.
    /// O último chunk sinaliza conclusão via <see cref="ChatStreamChunk.IsComplete"/>.
    /// Providers que não suportam streaming devem retornar um único chunk com a resposta completa.
    /// </summary>
    IAsyncEnumerable<ChatStreamChunk> CompleteStreamingAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Indica se o provider suporta streaming real.</summary>
    bool SupportsStreaming => true;
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

/// <summary>
/// Fragmento incremental de resposta durante streaming de chat/completion.
/// Emitido progressivamente pelo provider conforme o modelo gera tokens.
/// </summary>
public sealed record ChatStreamChunk(
    /// <summary>Fragmento de texto gerado neste chunk.</summary>
    string Content,
    /// <summary>Indica se este é o último chunk da resposta.</summary>
    bool IsComplete,
    /// <summary>Modelo usado para gerar a resposta.</summary>
    string ModelId,
    /// <summary>Identificador do provider.</summary>
    string ProviderId,
    /// <summary>Tokens de prompt (disponível apenas no chunk final, 0 nos demais).</summary>
    int PromptTokens = 0,
    /// <summary>Tokens de completion (disponível apenas no chunk final, 0 nos demais).</summary>
    int CompletionTokens = 0,
    /// <summary>Mensagem de erro, quando aplicável.</summary>
    string? ErrorMessage = null);
