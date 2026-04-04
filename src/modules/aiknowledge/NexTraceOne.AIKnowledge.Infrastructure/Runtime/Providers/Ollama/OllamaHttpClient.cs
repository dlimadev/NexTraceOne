using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Ollama;

/// <summary>
/// Client HTTP real para comunicação com Ollama.
/// Encapsula todas as chamadas REST, serialização, timeout, retry e logging estruturado.
/// Isolado na camada Infrastructure — domínio e aplicação não conhecem detalhes do Ollama.
/// </summary>
public sealed class OllamaHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaHttpClient(
        HttpClient httpClient,
        IOptions<OllamaOptions> options,
        ILogger<OllamaHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OllamaChatResponse?> ChatAsync(
        OllamaChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var maxRetries = _options.MaxRetries;

        while (true)
        {
            attempt++;
            try
            {
                _logger.LogInformation(
                    "Ollama chat request attempt {Attempt}/{MaxAttempts} for model {Model}",
                    attempt, maxRetries + 1, request.Model);

                var sw = Stopwatch.StartNew();
                var response = await _httpClient.PostAsJsonAsync(
                    "api/chat", request, JsonOptions, cancellationToken);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
                    JsonOptions, cancellationToken);
                sw.Stop();

                _logger.LogInformation(
                    "Ollama chat completed in {ElapsedMs}ms for model {Model}",
                    sw.ElapsedMilliseconds, request.Model);

                return result;
            }
            catch (Exception ex) when (attempt <= maxRetries && ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Ollama chat attempt {Attempt} failed for model {Model}. Retrying...",
                    attempt, request.Model);
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Ollama chat failed after {Attempts} attempts for model {Model}",
                    attempt, request.Model);
                throw;
            }
        }
    }

    /// <summary>
    /// Envia uma requisição de chat ao Ollama com stream=true e retorna chunks NDJSON progressivamente.
    /// O Ollama retorna uma linha JSON por chunk, e a última linha tem done=true.
    /// </summary>
    public async IAsyncEnumerable<OllamaChatResponse> ChatStreamAsync(
        OllamaChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        request.Stream = true;

        _logger.LogInformation("Ollama streaming chat request for model {Model}", request.Model);

        var jsonContent = JsonContent.Create(request, options: JsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = jsonContent
        };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            OllamaChatResponse? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Ollama streaming chunk");
                continue;
            }

            if (chunk is not null)
                yield return chunk;

            if (chunk?.Done == true)
                yield break;
        }
    }

    public async Task<OllamaTagsResponse?> ListModelsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<OllamaTagsResponse>(
                "api/tags", JsonOptions, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Ollama models");
            return null;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync(string.Empty, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Gera embedding para um texto via Ollama /api/embeddings.</summary>
    public async Task<(float[] Embedding, int Tokens)> GenerateEmbeddingAsync(
        string model, string text, CancellationToken cancellationToken = default)
    {
        var request = new OllamaEmbeddingRequest { Model = model, Prompt = text };
        var response = await _httpClient.PostAsJsonAsync("api/embeddings", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(JsonOptions, cancellationToken);
        float[] embedding = result?.Embedding?.ToArray() ?? Array.Empty<float>();
        return (embedding, text.Length / 4); // approximate token count
    }
}

// ── Ollama API DTOs ──

public sealed class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaChatMessage> Messages { get; set; } = [];

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("options")]
    public OllamaChatOptions? Options { get; set; }
}

public sealed class OllamaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public sealed class OllamaChatOptions
{
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("num_predict")]
    public int? NumPredict { get; set; }
}

public sealed class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public OllamaChatMessage? Message { get; set; }

    [JsonPropertyName("total_duration")]
    public long TotalDuration { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvalCount { get; set; }

    [JsonPropertyName("eval_count")]
    public int EvalCount { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

public sealed class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelInfo> Models { get; set; } = [];
}

public sealed class OllamaModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("details")]
    public OllamaModelDetails? Details { get; set; }
}

public sealed class OllamaModelDetails
{
    [JsonPropertyName("family")]
    public string Family { get; set; } = string.Empty;

    [JsonPropertyName("parameter_size")]
    public string ParameterSize { get; set; } = string.Empty;
}

public sealed class OllamaEmbeddingRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;
}

public sealed class OllamaEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; } = [];
}
