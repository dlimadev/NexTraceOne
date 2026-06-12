using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Null;

/// <summary>
/// Provider de IA que não executa inferência. Usado quando o usuário ou tenant
/// desabilita a IA para uma funcionalidade, permitindo graceful degradation
/// sem que módulos consumidores precisem de verificações condicionais espalhadas.
///
/// Implementa o Null Object Pattern para o subsistema de IA.
/// </summary>
public sealed class NullAiProvider : IAiProvider, IChatCompletionProvider
{
    public string ProviderId => "null";
    public string DisplayName => "IA Desabilitada";
    public bool IsLocal => true;

    public Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new AiProviderHealthResult(
            IsHealthy: true,
            ProviderId: ProviderId,
            Message: "No-op provider is always healthy",
            ResponseTime: TimeSpan.Zero));

    public Task<IReadOnlyList<AiProviderModelInfo>> ListAvailableModelsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AiProviderModelInfo>>(Array.Empty<AiProviderModelInfo>());

    public Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatCompletionResult(
            Success: false,
            Content: null,
            ModelId: "null",
            ProviderId: ProviderId,
            PromptTokens: 0,
            CompletionTokens: 0,
            Duration: TimeSpan.Zero,
            ErrorMessage: "IA está desabilitada para esta funcionalidade."
        ));

    public async IAsyncEnumerable<ChatStreamChunk> CompleteStreamingAsync(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatStreamChunk(
            Content: "IA está desabilitada para esta funcionalidade.",
            IsComplete: true,
            ModelId: "null",
            ProviderId: ProviderId,
            PromptTokens: 0,
            CompletionTokens: 0,
            ErrorMessage: null);

        await Task.CompletedTask; // Satisfy async enumerable requirement
    }
}
