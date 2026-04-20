using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.OpenAI;

/// <summary>
/// Client HTTP para comunicação com a API OpenAI (Chat Completions).
/// Encapsula serialização, autenticação, timeout e logging estruturado.
/// Não usa SDK externo — apenas HttpClient nativo para manter zero dependências extras.
/// </summary>
public sealed class OpenAiHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OpenAiHttpClient(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OpenAiChatResponse?> ChatAsync(
        OpenAiChatRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "OpenAI chat request for model {Model}", request.Model);

            var sw = Stopwatch.StartNew();
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(
                JsonOptions, cancellationToken);

            sw.Stop();
            _logger.LogInformation(
                "OpenAI chat completed in {ElapsedMs}ms for model {Model}",
                sw.ElapsedMilliseconds, request.Model);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI chat failed for model {Model}", request.Model);
            throw;
        }
    }

    /// <summary>
    /// Envia uma requisição de chat à API OpenAI com stream=true e retorna chunks SSE progressivamente.
    /// O OpenAI retorna linhas "data: {json}" e termina com "data: [DONE]".
    /// </summary>
    public async IAsyncEnumerable<OpenAiStreamChunk> ChatStreamAsync(
        OpenAiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OpenAI streaming chat request for model {Model}", request.Model);

        var streamRequest = new OpenAiStreamChatRequest
        {
            Model = request.Model,
            Messages = request.Messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            Stream = true
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = JsonContent.Create(streamRequest, options: JsonOptions)
        };

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

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

            var data = line["data: ".Length..];

            if (data == "[DONE]")
                yield break;

            OpenAiStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(data, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize OpenAI streaming chunk");
                continue;
            }

            if (chunk is not null)
                yield return chunk;
        }
    }

    /// <summary>
    /// Envia uma requisição de chat com tools (function calling nativo) à API OpenAI.
    /// Retorna tool_calls quando o modelo decide invocar uma função.
    /// Documentação: https://platform.openai.com/docs/guides/function-calling
    /// </summary>
    public async Task<OpenAiToolChatResponse?> ChatWithToolsAsync(
        OpenAiToolChatRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "OpenAI tool-calling chat request for model {Model}", request.Model);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<OpenAiToolChatResponse>(
                JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI tool-calling chat failed for model {Model}", request.Model);
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            // A lightweight models list call is the standard OpenAI health check approach
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/v1/models");
            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await _httpClient.SendAsync(httpRequest, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Gera embeddings para uma lista de textos via OpenAI /v1/embeddings.</summary>
    public async Task<(IReadOnlyList<float[]> Embeddings, int Tokens)> GenerateEmbeddingsAsync(
        string model, IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var request = new OpenAiEmbeddingRequest { Model = model, Input = texts.ToList() };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/embeddings")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(JsonOptions, cancellationToken);
        var embeddings = result?.Data.Select(d => d.Embedding.ToArray()).ToList()
            ?? new List<float[]>();

        return (embeddings, result?.Usage?.TotalTokens ?? 0);
    }
}

// ── OpenAI API DTOs ──

public sealed class OpenAiChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OpenAiChatMessage> Messages { get; set; } = [];

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
}

/// <summary>Request DTO for OpenAI with stream field (used only for streaming calls).</summary>
public sealed class OpenAiStreamChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OpenAiChatMessage> Messages { get; set; } = [];

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

public sealed class OpenAiChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public sealed class OpenAiChatResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<OpenAiChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public OpenAiUsage? Usage { get; set; }
}

public sealed class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiChatMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public sealed class OpenAiUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

// ── OpenAI Streaming DTOs ──

/// <summary>Single chunk from the OpenAI streaming response (SSE).</summary>
public sealed class OpenAiStreamChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<OpenAiStreamChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public OpenAiUsage? Usage { get; set; }
}

/// <summary>Choice within a streaming chunk.</summary>
public sealed class OpenAiStreamChoice
{
    [JsonPropertyName("delta")]
    public OpenAiStreamDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>Delta content from a streaming chunk.</summary>
public sealed class OpenAiStreamDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

// ── OpenAI Embedding DTOs ──

public sealed class OpenAiEmbeddingRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public List<string> Input { get; set; } = [];
}

public sealed class OpenAiEmbeddingResponse
{
    [JsonPropertyName("data")]
    public List<OpenAiEmbeddingData> Data { get; set; } = [];

    [JsonPropertyName("usage")]
    public OpenAiUsage? Usage { get; set; }
}

public sealed class OpenAiEmbeddingData
{
    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; } = [];

    [JsonPropertyName("index")]
    public int Index { get; set; }
}

// ── OpenAI Function Calling DTOs ──

/// <summary>Request de chat com tools (function calling nativo).</summary>
public sealed class OpenAiToolChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OpenAiChatMessage> Messages { get; set; } = [];

    [JsonPropertyName("tools")]
    public List<OpenAiTool> Tools { get; set; } = [];

    [JsonPropertyName("tool_choice")]
    public string ToolChoice { get; set; } = "auto";

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
}

/// <summary>Definição de tool no formato OpenAI (type: "function").</summary>
public sealed class OpenAiTool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenAiToolFunction Function { get; set; } = new();
}

/// <summary>Definição da função dentro de uma tool OpenAI.</summary>
public sealed class OpenAiToolFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public object? Parameters { get; set; }
}

/// <summary>Resposta de chat com tools — inclui tool_calls quando o modelo invoca funções.</summary>
public sealed class OpenAiToolChatResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<OpenAiToolChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public OpenAiUsage? Usage { get; set; }
}

/// <summary>Choice de uma resposta com function calling.</summary>
public sealed class OpenAiToolChoice
{
    [JsonPropertyName("message")]
    public OpenAiToolMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>Mensagem com possíveis tool_calls no response.</summary>
public sealed class OpenAiToolMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OpenAiToolCall>? ToolCalls { get; set; }
}

/// <summary>Chamada de tool nativa incluída na resposta OpenAI.</summary>
public sealed class OpenAiToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenAiToolCallFunction Function { get; set; } = new();
}

/// <summary>Detalhe da função chamada pelo modelo.</summary>
public sealed class OpenAiToolCallFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}
