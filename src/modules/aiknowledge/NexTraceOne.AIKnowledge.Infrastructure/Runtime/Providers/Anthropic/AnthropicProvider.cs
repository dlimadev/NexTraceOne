using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Anthropic;

/// <summary>
/// Provider de IA Anthropic/Claude — externo, configurável, sem SDK proprietário.
/// Implementa IAiProvider, IChatCompletionProvider e IFunctionCallingChatProvider para integração no AI Runtime.
/// Activo apenas quando ApiKey está configurada em AiRuntime:Anthropic.
///
/// Anthropic usa a Messages API (POST /v1/messages).
/// O campo "content" da resposta é uma lista de blocos; extraímos o primeiro bloco do tipo "text".
/// Referência: https://docs.anthropic.com/en/api/messages
/// </summary>
public sealed class AnthropicProvider : IAiProvider, IChatCompletionProvider, IFunctionCallingChatProvider, INativeToolCallProvider
{
    public bool SupportsNativeToolCalls => true;

    public const string ProviderIdentifier = "anthropic";

    private static readonly string[] DefaultCapabilities = ["chat", "completion", "reasoning", "long_context"];

    private readonly AnthropicHttpClient _client;
    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicProvider> _logger;

    public AnthropicProvider(
        AnthropicHttpClient client,
        IOptions<AnthropicOptions> options,
        ILogger<AnthropicProvider> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => ProviderIdentifier;
    public string DisplayName => "Anthropic";
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
                healthy ? "Anthropic API is reachable" : "Anthropic API is not responding",
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Anthropic health check failed");
            return new AiProviderHealthResult(
                false, ProviderId,
                $"Health check failed: {ex.Message}", sw.Elapsed);
        }
    }

    public Task<IReadOnlyList<AiProviderModelInfo>> ListAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        // Static list — Anthropic model catalogue (updated April 2026 — Claude 4.x family)
        IReadOnlyList<AiProviderModelInfo> models =
        [
            // Claude 4.x — latest generation (April 2026)
            new AiProviderModelInfo("claude-opus-4-7", "Claude Opus 4.7", null, DefaultCapabilities),
            new AiProviderModelInfo("claude-sonnet-4-6", "Claude Sonnet 4.6", null, DefaultCapabilities),
            new AiProviderModelInfo("claude-haiku-4-5", "Claude Haiku 4.5", null, DefaultCapabilities),
            // Claude 3.x — previous generation (kept for backwards compatibility)
            new AiProviderModelInfo("claude-3-5-sonnet-20241022", "Claude 3.5 Sonnet", null, DefaultCapabilities),
            new AiProviderModelInfo("claude-3-5-haiku-20241022", "Claude 3.5 Haiku", null, DefaultCapabilities),
            new AiProviderModelInfo("claude-3-opus-20240229", "Claude 3 Opus", null, DefaultCapabilities),
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
            var anthropicRequest = BuildMessagesRequest(request, modelId, stream: null);
            var response = await _client.ChatAsync(anthropicRequest, cancellationToken);
            sw.Stop();

            if (response is null)
            {
                return new ChatCompletionResult(
                    false, null, modelId, ProviderId,
                    0, 0, sw.Elapsed, "No response from Anthropic API");
            }

            var textContent = ExtractTextContent(response.Content);
            if (string.IsNullOrWhiteSpace(textContent))
            {
                return new ChatCompletionResult(
                    false, null, response.Model.NullIfEmpty() ?? modelId, ProviderId,
                    response.Usage?.InputTokens ?? 0,
                    response.Usage?.OutputTokens ?? 0,
                    sw.Elapsed,
                    $"Stop reason: {response.StopReason ?? "unknown"}");
            }

            return new ChatCompletionResult(
                true,
                textContent,
                response.Model.NullIfEmpty() ?? modelId,
                ProviderId,
                response.Usage?.InputTokens ?? 0,
                response.Usage?.OutputTokens ?? 0,
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Anthropic chat completion failed for model {Model}", modelId);
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

        var anthropicRequest = BuildMessagesRequest(request, modelId, stream: true);
        var hasYielded = false;
        var inputTokens = 0;
        var outputTokens = 0;

        await foreach (var evt in _client.ChatStreamAsync(anthropicRequest, cancellationToken))
        {
            switch (evt.Type)
            {
                case "message_start":
                    if (evt.Message?.Usage is not null)
                        inputTokens = evt.Message.Usage.InputTokens;
                    break;

                case "content_block_delta" when evt.Delta?.Type == "text_delta":
                    var text = evt.Delta.Text ?? string.Empty;
                    if (!string.IsNullOrEmpty(text))
                    {
                        hasYielded = true;
                        yield return new ChatStreamChunk(
                            text, false, modelId, ProviderId);
                    }
                    break;

                case "message_delta":
                    if (evt.Usage is not null)
                        outputTokens = evt.Usage.OutputTokens;
                    break;

                case "message_stop":
                    yield return new ChatStreamChunk(
                        string.Empty, true, modelId, ProviderId,
                        inputTokens, outputTokens);
                    yield break;
            }
        }

        if (!hasYielded)
        {
            yield return new ChatStreamChunk(
                string.Empty, true, modelId, ProviderId,
                ErrorMessage: "No streaming response from Anthropic");
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private AnthropicMessagesRequest BuildMessagesRequest(
        ChatCompletionRequest request,
        string modelId,
        bool? stream)
    {
        // Anthropic separates system prompt from messages
        var systemContent = request.SystemPrompt;
        var systemMessages = request.Messages
            .Where(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (systemMessages.Count > 0)
        {
            var combined = string.Join("\n\n", systemMessages.Select(m => m.Content));
            systemContent = string.IsNullOrWhiteSpace(systemContent)
                ? combined
                : $"{systemContent}\n\n{combined}";
        }

        var nonSystemMessages = request.Messages
            .Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
            .Select(m => new AnthropicMessage { Role = m.Role, Content = m.Content })
            .ToList();

        return new AnthropicMessagesRequest
        {
            Model = modelId,
            MaxTokens = request.MaxTokens ?? _options.DefaultMaxTokens,
            Messages = nonSystemMessages,
            System = systemContent.NullIfEmpty(),
            Temperature = request.Temperature ?? _options.DefaultTemperature,
            Stream = stream
        };
    }

    private static string? ExtractTextContent(IReadOnlyList<AnthropicContentBlock> blocks)
        => blocks.FirstOrDefault(b => b.Type == "text")?.Text;

    /// <inheritdoc/>
    public async Task<FunctionCallingResult> CompleteWithToolsAsync(
        ChatCompletionRequest request,
        IReadOnlyList<FunctionDefinition> functions,
        CancellationToken cancellationToken = default)
    {
        var modelId = string.IsNullOrWhiteSpace(request.ModelId)
            ? _options.DefaultChatModel
            : request.ModelId;

        var sw = Stopwatch.StartNew();
        try
        {
            var messages = request.Messages
                .Where(m => m.Role != "system")
                .Select(m => new AnthropicMessage { Role = m.Role, Content = m.Content })
                .ToList();

            var systemPrompt = request.Messages
                .FirstOrDefault(m => m.Role == "system")?.Content;

            var tools = functions.Select(f => new AnthropicTool
            {
                Name = f.Name,
                Description = f.Description,
                InputSchema = f.Parameters
            }).ToList();

            var toolRequest = new AnthropicToolChatRequest
            {
                Model = modelId,
                MaxTokens = request.MaxTokens ?? _options.DefaultMaxTokens,
                Messages = messages,
                Tools = tools,
                System = systemPrompt.NullIfEmpty(),
                Temperature = request.Temperature ?? _options.DefaultTemperature,
            };

            var response = await _client.ChatWithToolsAsync(toolRequest, cancellationToken);
            sw.Stop();

            if (response is null)
            {
                return new FunctionCallingResult(
                    false, null, [], modelId, ProviderId,
                    0, 0, sw.Elapsed, ErrorMessage: "No response from Anthropic tool-calling endpoint");
            }

            // Extract text content and tool_use blocks
            var textContent = response.Content
                .Where(b => b.Type == "text")
                .Select(b => b.Text)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

            var nativeToolCalls = response.Content
                .Where(b => b.Type == "tool_use" && b.Id is not null && b.Name is not null)
                .Select(b => new NativeToolCall(
                    b.Id!,
                    b.Name!,
                    b.Input.HasValue ? b.Input.Value.GetRawText() : "{}"))
                .ToList();

            return new FunctionCallingResult(
                true,
                textContent,
                nativeToolCalls,
                response.Model ?? modelId,
                ProviderId,
                response.Usage?.InputTokens ?? 0,
                response.Usage?.OutputTokens ?? 0,
                sw.Elapsed,
                FinishReason: response.StopReason);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Anthropic tool-calling completion failed for model {Model}", modelId);
            return new FunctionCallingResult(
                false, null, [], modelId, ProviderId,
                0, 0, sw.Elapsed, ErrorMessage: ex.Message);
        }
    }
}

/// <summary>Extension helper para strings.</summary>
internal static class StringExtensions
{
    internal static string? NullIfEmpty(this string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
