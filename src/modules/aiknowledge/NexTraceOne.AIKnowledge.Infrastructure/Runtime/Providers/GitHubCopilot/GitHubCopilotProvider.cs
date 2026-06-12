using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.GitHubCopilot;

/// <summary>
/// Provider de IA GitHub Copilot (via GitHub Models API) — externo, configurável, sem SDK proprietário.
/// Implementa IAiProvider e IChatCompletionProvider para integração no AI Runtime.
/// Activo apenas quando ApiToken está configurada em AiRuntime:GitHubCopilot.
/// </summary>
public sealed class GitHubCopilotProvider : IAiProvider, IChatCompletionProvider
{
    public const string ProviderIdentifier = "github-copilot";

    private static readonly string[] DefaultCapabilities = ["chat", "completion", "reasoning"];

    private readonly GitHubCopilotHttpClient _client;
    private readonly GitHubCopilotOptions _options;
    private readonly ILogger<GitHubCopilotProvider> _logger;

    public GitHubCopilotProvider(
        GitHubCopilotHttpClient client,
        IOptions<GitHubCopilotOptions> options,
        ILogger<GitHubCopilotProvider> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => ProviderIdentifier;
    public string DisplayName => "GitHub Copilot";
    public bool IsLocal => false;

    public async Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var healthy = await _client.IsHealthyAsync(cancellationToken);
            sw.Stop();
            return new AiProviderHealthResult(
                healthy,
                ProviderId,
                healthy ? "GitHub Copilot API is reachable" : "GitHub Copilot API is not responding",
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "GitHub Copilot health check failed");
            return new AiProviderHealthResult(
                false, ProviderId,
                $"Health check failed: {ex.Message}", sw.Elapsed);
        }
    }

    public Task<IReadOnlyList<AiProviderModelInfo>> ListAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AiProviderModelInfo> models =
        [
            new AiProviderModelInfo("gpt-4o", "GPT-4o", null, ["chat", "vision", "reasoning"]),
            new AiProviderModelInfo("gpt-4o-mini", "GPT-4o Mini", null, DefaultCapabilities),
            new AiProviderModelInfo("copilot-chat", "Copilot Chat", null, ["chat", "code"]),
        ];
        return Task.FromResult(models);
    }

    public async Task<ChatCompletionResult> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var modelId = string.IsNullOrWhiteSpace(request.ModelId)
            ? _options.DefaultChatModel
            : request.ModelId;

        var sw = Stopwatch.StartNew();
        try
        {
            var messages = request.Messages.Select(m => new GitHubChatMessage(m.Role, m.Content)).ToList();

            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                messages.Insert(0, new GitHubChatMessage("system", request.SystemPrompt));
            }

            var chatRequest = new GitHubChatRequest(
                Model: modelId,
                Messages: messages,
                Temperature: request.Temperature ?? _options.DefaultTemperature,
                MaxTokens: request.MaxTokens ?? _options.DefaultMaxTokens);

            var response = await _client.SendChatCompletionAsync(chatRequest, cancellationToken);
            sw.Stop();

            if (response?.Choices is not { Count: > 0 } ||
                string.IsNullOrWhiteSpace(response.Choices[0].Message?.Content))
            {
                return new ChatCompletionResult(
                    false, null, modelId, ProviderId,
                    0, 0, sw.Elapsed, "No response from GitHub Copilot");
            }

            var choice = response.Choices[0];
            var content = choice.Message?.Content ?? string.Empty;

            return new ChatCompletionResult(
                true,
                content,
                modelId,
                ProviderId,
                response.Usage?.PromptTokens ?? 0,
                response.Usage?.CompletionTokens ?? 0,
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GitHub Copilot chat completion failed for model {Model}", modelId);
            return new ChatCompletionResult(
                false, null, modelId, ProviderId,
                0, 0, sw.Elapsed, ex.Message);
        }
    }

    public async IAsyncEnumerable<ChatStreamChunk> CompleteStreamingAsync(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var modelId = string.IsNullOrWhiteSpace(request.ModelId)
            ? _options.DefaultChatModel
            : request.ModelId;

        var messages = request.Messages.Select(m => new GitHubChatMessage(m.Role, m.Content)).ToList();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Insert(0, new GitHubChatMessage("system", request.SystemPrompt));
        }

        var chatRequest = new GitHubChatRequest(
            Model: modelId,
            Messages: messages,
            Temperature: request.Temperature ?? _options.DefaultTemperature,
            MaxTokens: request.MaxTokens ?? _options.DefaultMaxTokens);

        var hasYielded = false;
        await foreach (var chunk in _client.StreamChatCompletionAsync(chatRequest, cancellationToken))
        {
            hasYielded = true;
            var content = string.Empty;
            var finishReason = (string?)null;

            if (chunk.Choices is { Count: > 0 })
            {
                content = chunk.Choices[0].Delta?.Content ?? string.Empty;
                finishReason = chunk.Choices[0].FinishReason;
            }

            var isComplete = finishReason is "stop" or "length";

            yield return new ChatStreamChunk(
                content,
                isComplete,
                modelId,
                ProviderId,
                isComplete ? (chunk.Usage?.PromptTokens ?? 0) : 0,
                isComplete ? (chunk.Usage?.CompletionTokens ?? 0) : 0);

            if (isComplete)
                yield break;
        }

        if (!hasYielded)
        {
            yield return new ChatStreamChunk(
                string.Empty, true, modelId, ProviderId,
                ErrorMessage: "No streaming response from GitHub Copilot");
        }
    }
}
