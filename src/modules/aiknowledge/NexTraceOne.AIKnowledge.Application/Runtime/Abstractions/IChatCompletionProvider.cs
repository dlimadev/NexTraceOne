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

// ── Native Function Calling ──

/// <summary>
/// Definição de função disponível para o modelo chamar via native function calling.
/// Mapeada a partir de ToolDefinition para o formato esperado pelo provider (OpenAI / Anthropic).
/// </summary>
public sealed record FunctionDefinition(
    /// <summary>Nome único da função (igual ao nome da ToolDefinition).</summary>
    string Name,
    /// <summary>Descrição da função para o modelo.</summary>
    string Description,
    /// <summary>JSON Schema dos parâmetros aceites (serializado como objeto anónimo).</summary>
    object? Parameters = null);

/// <summary>
/// Chamada de tool nativa retornada pelo modelo no response.
/// Cada NativeToolCall corresponde a uma invocação estruturada decidida pelo modelo.
/// </summary>
public sealed record NativeToolCall(
    /// <summary>Identificador único da chamada (fornecido pelo provider).</summary>
    string Id,
    /// <summary>Nome da função a invocar.</summary>
    string FunctionName,
    /// <summary>Argumentos da chamada em formato JSON.</summary>
    string ArgumentsJson);

/// <summary>
/// Resultado de uma inferência com suporte a native function calling.
/// Pode conter texto de resposta, chamadas de tool, ou ambos.
/// </summary>
public sealed record FunctionCallingResult(
    bool Success,
    string? Content,
    IReadOnlyList<NativeToolCall> ToolCalls,
    string ModelId,
    string ProviderId,
    int PromptTokens,
    int CompletionTokens,
    TimeSpan Duration,
    string? FinishReason = null,
    string? ErrorMessage = null)
{
    /// <summary>Indica se o modelo solicitou execução de tools nativas.</summary>
    public bool HasToolCalls => ToolCalls.Count > 0;
}

/// <summary>
/// Extensão de IChatCompletionProvider com suporte a native function calling.
/// Implementado por providers que suportam o protocolo nativo de tools (OpenAI, Anthropic).
/// Providers sem esta interface usam o fallback textual [TOOL_CALL:] em AiAgentRuntimeService.
/// </summary>
public interface IFunctionCallingChatProvider : IChatCompletionProvider
{
    /// <summary>
    /// Executa inferência com definições de funções disponíveis para chamada nativa.
    /// O modelo decide autonomamente se chama uma função ou responde com texto.
    /// </summary>
    /// <param name="request">Request de chat com histórico de mensagens.</param>
    /// <param name="functions">Lista de funções disponíveis para o modelo invocar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com Content (texto) e/ou ToolCalls (chamadas nativas).</returns>
    Task<FunctionCallingResult> CompleteWithToolsAsync(
        ChatCompletionRequest request,
        IReadOnlyList<FunctionDefinition> functions,
        CancellationToken cancellationToken = default);
}

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
