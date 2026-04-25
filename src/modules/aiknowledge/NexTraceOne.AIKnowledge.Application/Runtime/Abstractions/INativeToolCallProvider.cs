namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Marca providers que expõem native function/tool calling.
/// Providers que implementam esta interface têm <see cref="SupportsNativeToolCalls"/> == true
/// e devem também implementar <see cref="IFunctionCallingChatProvider"/>.
/// Providers sem esta interface usam <see cref="StructuredOutputFallback"/> como alternativa.
/// </summary>
public interface INativeToolCallProvider
{
    /// <summary>Indica se o provider suporta tool calling nativo a nível de protocolo.</summary>
    bool SupportsNativeToolCalls { get; }
}

/// <summary>Contrato para roteamento de tool calls entre native e fallback.</summary>
public interface IToolCallRouter
{
    /// <summary>
    /// Executa inferência com tools disponíveis, escolhendo a estratégia adequada ao provider.
    /// Se o provider implementa <see cref="IFunctionCallingChatProvider"/>, usa native tool calling.
    /// Caso contrário, usa <see cref="StructuredOutputFallback"/> com JSON explícito no system prompt.
    /// </summary>
    Task<FunctionCallingResult> RouteAsync(
        IChatCompletionProvider provider,
        ChatCompletionRequest request,
        IReadOnlyList<FunctionDefinition> functions,
        CancellationToken ct = default);
}
