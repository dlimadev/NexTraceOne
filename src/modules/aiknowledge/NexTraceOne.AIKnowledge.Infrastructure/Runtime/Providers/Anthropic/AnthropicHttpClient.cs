using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Anthropic;

/// <summary>
/// Client HTTP para comunicação com a API Anthropic Messages (Claude).
/// Encapsula autenticação via x-api-key e x-anthropic-version, serialização e logging.
/// Não usa SDK externo — apenas HttpClient nativo para manter zero dependências extras.
/// Documentação: https://docs.anthropic.com/en/api/messages
/// </summary>
public sealed class AnthropicHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AnthropicHttpClient(
        HttpClient httpClient,
        IOptions<AnthropicOptions> options,
        ILogger<AnthropicHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Envia uma requisição de chat à API Anthropic e retorna a resposta completa.</summary>
    public async Task<AnthropicMessagesResponse?> ChatAsync(
        AnthropicMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Anthropic chat request for model {Model}", request.Model);

            var httpRequest = BuildRequest("/v1/messages", request);
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AnthropicMessagesResponse>(
                JsonOptions, cancellationToken);

            _logger.LogInformation("Anthropic chat completed for model {Model}", request.Model);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic chat failed for model {Model}", request.Model);
            throw;
        }
    }

    /// <summary>
    /// Envia uma requisição de chat com stream=true e retorna chunks SSE progressivamente.
    /// Anthropic retorna linhas "event: content_block_delta\ndata: {json}" por chunk.
    /// </summary>
    public async IAsyncEnumerable<AnthropicStreamEvent> ChatStreamAsync(
        AnthropicMessagesRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Anthropic streaming chat request for model {Model}", request.Model);

        var streamRequest = request with { Stream = true };
        var httpRequest = BuildRequest("/v1/messages", streamRequest);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? eventType = null;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                eventType = null;
                continue;
            }

            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                eventType = line["event: ".Length..];
                continue;
            }

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var data = line["data: ".Length..];

            AnthropicStreamEvent? evt;
            try
            {
                evt = JsonSerializer.Deserialize<AnthropicStreamEvent>(data, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Anthropic streaming event (type={Type})", eventType);
                continue;
            }

            if (evt is not null)
                yield return evt;
        }
    }

    /// <summary>Verifica a disponibilidade da API Anthropic com uma requisição leve.</summary>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            // Minimal request to check API availability — use a tiny message
            var probe = new AnthropicMessagesRequest
            {
                Model = _options.DefaultChatModel,
                MaxTokens = 1,
                Messages = [new AnthropicMessage { Role = "user", Content = "Hi" }]
            };

            var httpRequest = BuildRequest("/v1/messages", probe);
            var response = await _httpClient.SendAsync(httpRequest, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private HttpRequestMessage BuildRequest(string path, object body)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", _options.ApiVersion);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return httpRequest;
    }
}

// ── Anthropic Messages API DTOs ───────────────────────────────────────────────

/// <summary>Request para a API Anthropic Messages.</summary>
public sealed record AnthropicMessagesRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; init; } = 2048;

    [JsonPropertyName("messages")]
    public List<AnthropicMessage> Messages { get; init; } = [];

    [JsonPropertyName("system")]
    public string? System { get; init; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    [JsonPropertyName("stream")]
    public bool? Stream { get; init; }
}

/// <summary>Mensagem individual para a API Anthropic.</summary>
public sealed class AnthropicMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>Resposta completa da API Anthropic Messages.</summary>
public sealed class AnthropicMessagesResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("content")]
    public List<AnthropicContentBlock> Content { get; set; } = [];

    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }
}

/// <summary>Bloco de conteúdo da resposta Anthropic.</summary>
public sealed class AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>Estatísticas de uso de tokens da API Anthropic.</summary>
public sealed class AnthropicUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}

/// <summary>Evento de streaming da API Anthropic SSE.</summary>
public sealed class AnthropicStreamEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [JsonPropertyName("delta")]
    public AnthropicStreamDelta? Delta { get; set; }

    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }

    [JsonPropertyName("message")]
    public AnthropicMessagesResponse? Message { get; set; }
}

/// <summary>Delta incremental de um chunk de streaming Anthropic.</summary>
public sealed class AnthropicStreamDelta
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }
}
