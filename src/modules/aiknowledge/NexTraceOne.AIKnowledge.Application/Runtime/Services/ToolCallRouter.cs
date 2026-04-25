using Microsoft.Extensions.Logging;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Roteia execuções com tool calling para a estratégia adequada ao provider.
/// Providers com IFunctionCallingChatProvider usam native tool calling.
/// Providers sem suporte nativo usam StructuredOutputFallback.
/// </summary>
public sealed class ToolCallRouter : IToolCallRouter
{
    private readonly StructuredOutputFallback _fallback;
    private readonly ILogger<ToolCallRouter> _logger;

    public ToolCallRouter(
        StructuredOutputFallback fallback,
        ILogger<ToolCallRouter> logger)
    {
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<FunctionCallingResult> RouteAsync(
        IChatCompletionProvider provider,
        ChatCompletionRequest request,
        IReadOnlyList<FunctionDefinition> functions,
        CancellationToken ct = default)
    {
        if (functions.Count == 0)
        {
            var plain = await provider.CompleteAsync(request, ct);
            return PlainResultToFunctionCalling(plain);
        }

        if (provider is IFunctionCallingChatProvider nativeProvider)
        {
            _logger.LogDebug(
                "ToolCallRouter: using native tool calling for provider {ProviderId}",
                provider.ProviderId);
            return await nativeProvider.CompleteWithToolsAsync(request, functions, ct);
        }

        _logger.LogDebug(
            "ToolCallRouter: provider {ProviderId} has no native tool calling — using StructuredOutputFallback",
            provider.ProviderId);
        return await _fallback.CompleteWithStructuredJsonAsync(provider, request, functions, ct);
    }

    private static FunctionCallingResult PlainResultToFunctionCalling(ChatCompletionResult plain)
        => new(
            plain.Success,
            plain.Content,
            [],
            plain.ModelId,
            plain.ProviderId,
            plain.PromptTokens,
            plain.CompletionTokens,
            plain.Duration,
            ErrorMessage: plain.ErrorMessage);
}
