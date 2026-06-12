using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.GitHubCopilot;

/// <summary>
/// Client HTTP para comunicação com a API GitHub Models.
/// A API GitHub Models é compatível com OpenAI Chat Completions, mas usa endpoint próprio
/// e autenticação via token GitHub (Authorization: Bearer).
/// Não usa SDK externo — apenas HttpClient nativo.
/// </summary>
public sealed class GitHubCopilotHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly GitHubCopilotOptions _options;
    private readonly ILogger<GitHubCopilotHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GitHubCopilotHttpClient(
        HttpClient httpClient,
        IOptions<GitHubCopilotOptions> options,
        ILogger<GitHubCopilotHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GitHubChatResponse?> SendChatCompletionAsync(
        GitHubChatRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "GitHub Copilot chat completion request for model {Model}", request.Model);

            var sw = Stopwatch.StartNew();
            var response = await _httpClient.PostAsJsonAsync(
                "/chat/completions", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GitHubChatResponse>(
                JsonOptions, cancellationToken);

            sw.Stop();
            _logger.LogInformation(
                "GitHub Copilot chat completion completed in {ElapsedMs}ms for model {Model}",
                sw.ElapsedMilliseconds, request.Model);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub Copilot chat completion failed for model {Model}", request.Model);
            throw;
        }
    }

    public async IAsyncEnumerable<GitHubChatStreamChunk> StreamChatCompletionAsync(
        GitHubChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GitHub Copilot streaming request for model {Model}", request.Model);

        var streamRequest = request with { Stream = true };
        var response = await _httpClient.PostAsJsonAsync(
            "/chat/completions", streamRequest, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var payload = line[6..].Trim();
            if (payload == "[DONE]")
                yield break;

            GitHubChatStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<GitHubChatStreamChunk>(payload, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize GitHub Copilot streaming chunk");
                continue;
            }

            if (chunk is not null)
                yield return chunk;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            // GitHub Models doesn't have a dedicated health endpoint;
            // we use the models list endpoint as a lightweight check
            var response = await _httpClient.GetAsync("/models", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

// ── GitHub Models API DTOs (OpenAI-compatible) ──

public sealed record GitHubChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] List<GitHubChatMessage> Messages,
    [property: JsonPropertyName("temperature")] double? Temperature = null,
    [property: JsonPropertyName("max_tokens")] int? MaxTokens = null,
    [property: JsonPropertyName("stream")] bool Stream = false);

public sealed record GitHubChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

public sealed record GitHubChatResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<GitHubChatChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public GitHubChatUsage? Usage { get; set; }
}

public sealed record GitHubChatChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public GitHubChatMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("delta")]
    public GitHubChatMessage? Delta { get; set; }
}

public sealed record GitHubChatUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public sealed record GitHubChatStreamChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<GitHubChatChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public GitHubChatUsage? Usage { get; set; }
}
