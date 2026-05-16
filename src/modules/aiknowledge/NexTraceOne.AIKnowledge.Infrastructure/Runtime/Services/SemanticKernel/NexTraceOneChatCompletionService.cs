using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services.SemanticKernel;

/// <summary>
/// Adapter que expõe os providers customizados (Ollama, OpenAI, etc.) como um <see cref="IChatCompletionService"/>
/// nativo do Semantic Kernel. Permite que o kernel invoque LLMs sem depender de conectores alpha/RC externos.
/// </summary>
public sealed class NexTraceOneChatCompletionService : IChatCompletionService
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly string? _preferredProviderId;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

    public NexTraceOneChatCompletionService(IAiProviderFactory providerFactory, string? preferredProviderId = null)
    {
        _providerFactory = providerFactory;
        _preferredProviderId = preferredProviderId;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(kernel);
        if (provider is null)
        {
            throw new InvalidOperationException("No chat completion provider is available. Ensure Ollama or OpenAI is configured and enabled.");
        }

        var request = ConvertToRequest(chatHistory, executionSettings);
        var result = await provider.CompleteAsync(request, cancellationToken);

        if (!result.Success)
        {
            throw new KernelException($"Chat completion failed via {provider.ProviderId}: {result.ErrorMessage}");
        }

        return new List<ChatMessageContent>
        {
            new ChatMessageContent(AuthorRole.Assistant, result.Content ?? string.Empty)
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(kernel);
        if (provider is null)
        {
            throw new InvalidOperationException("No chat completion provider is available for streaming.");
        }

        var request = ConvertToRequest(chatHistory, executionSettings);

        await foreach (var chunk in provider.CompleteStreamingAsync(request, cancellationToken))
        {
            yield return new StreamingChatMessageContent(
                AuthorRole.Assistant,
                chunk.Content,
                modelId: chunk.ModelId,
                metadata: new Dictionary<string, object?>
                {
                    [nameof(chunk.IsComplete)] = chunk.IsComplete,
                    [nameof(chunk.ProviderId)] = chunk.ProviderId
                });
        }
    }

    private IChatCompletionProvider? ResolveProvider(Kernel? kernel)
    {
        // 1. Try kernel data override
        if (kernel?.Data.TryGetValue("ProviderId", out var pid) == true && pid is string providerId)
        {
            return _providerFactory.GetChatProvider(providerId);
        }

        // 2. Try preferred provider
        if (!string.IsNullOrWhiteSpace(_preferredProviderId))
        {
            return _providerFactory.GetChatProvider(_preferredProviderId);
        }

        // 3. Fallback to any available provider
        return _providerFactory.GetChatProvider("ollama")
            ?? _providerFactory.GetChatProvider("openai");
    }

    private static ChatCompletionRequest ConvertToRequest(ChatHistory chatHistory, PromptExecutionSettings? settings)
    {
        var messages = chatHistory
            .Select(m => new ChatMessage(m.Role.Label, m.Content ?? string.Empty))
            .ToList();

        string? systemPrompt = null;
        if (messages.Count > 0 && messages[0].Role.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            systemPrompt = messages[0].Content;
        }

        double? temperature = null;
        int? maxTokens = null;

        if (settings is not null)
        {
            if (settings.ExtensionData?.TryGetValue("temperature", out var temp) == true)
            {
                temperature = Convert.ToDouble(temp);
            }
            if (settings.ExtensionData?.TryGetValue("max_tokens", out var max) == true)
            {
                maxTokens = Convert.ToInt32(max);
            }
        }

        // Use last message's role as model hint, or default
        var modelId = messages.LastOrDefault()?.Role ?? "default";

        return new ChatCompletionRequest(
            modelId,
            messages,
            temperature,
            maxTokens,
            systemPrompt);
    }
}
