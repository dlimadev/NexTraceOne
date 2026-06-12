using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Gemini;

/// <summary>
/// Client HTTP para comunicação com a API Google Gemini (Generative Language API).
/// Encapsula serialização, autenticação, timeout e logging estruturado.
/// Não usa SDK externo — apenas HttpClient nativo para manter zero dependências extras.
/// </summary>
public sealed class GeminiHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiHttpClient(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GeminiChatResponse?> GenerateContentAsync(
        GeminiChatRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Gemini generateContent request for model {Model}", request.Model);

            var sw = Stopwatch.StartNew();
            var url = $"/models/{request.Model}:generateContent?key={_options.ApiKey}";

            var response = await _httpClient.PostAsJsonAsync(url, request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiChatResponse>(
                JsonOptions, cancellationToken);

            sw.Stop();
            _logger.LogInformation(
                "Gemini generateContent completed in {ElapsedMs}ms for model {Model}",
                sw.ElapsedMilliseconds, request.Model);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini generateContent failed for model {Model}", request.Model);
            throw;
        }
    }

    public async IAsyncEnumerable<GeminiStreamChunk> StreamGenerateContentAsync(
        GeminiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Gemini streaming request for model {Model}", request.Model);

        var url = $"/models/{request.Model}:streamGenerateContent?key={_options.ApiKey}";

        var response = await _httpClient.PostAsJsonAsync(url, request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("[", StringComparison.Ordinal) || line.StartsWith("]", StringComparison.Ordinal))
                continue;

            // Gemini stream returns individual JSON objects, sometimes prefixed with comma
            var json = line.TrimStart(',').Trim();
            if (string.IsNullOrWhiteSpace(json))
                continue;

            GeminiStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<GeminiStreamChunk>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Gemini streaming chunk");
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

            var url = $"/models?key={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

// ── Gemini API DTOs ──

public sealed class GeminiChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = [];

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}

public sealed class GeminiContent
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = [];
}

public sealed class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public sealed class GeminiGenerationConfig
{
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }
}

public sealed class GeminiChatResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate> Candidates { get; set; } = [];

    [JsonPropertyName("usageMetadata")]
    public GeminiUsageMetadata? UsageMetadata { get; set; }
}

public sealed class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}

public sealed class GeminiUsageMetadata
{
    [JsonPropertyName("promptTokenCount")]
    public int PromptTokenCount { get; set; }

    [JsonPropertyName("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }

    [JsonPropertyName("totalTokenCount")]
    public int TotalTokenCount { get; set; }
}

// ── Gemini Streaming DTOs ──

public sealed class GeminiStreamChunk
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate> Candidates { get; set; } = [];

    [JsonPropertyName("usageMetadata")]
    public GeminiUsageMetadata? UsageMetadata { get; set; }
}
