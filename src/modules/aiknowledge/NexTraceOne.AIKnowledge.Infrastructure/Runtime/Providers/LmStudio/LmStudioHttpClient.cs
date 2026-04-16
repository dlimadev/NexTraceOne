using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.LmStudio;

/// <summary>
/// Client HTTP para comunicação com LM Studio Server.
/// LM Studio expõe uma API compatível com OpenAI em http://localhost:1234/v1.
/// Não requer chave de API — autenticação é feita apenas por rede local.
/// Encapsula serialização, timeout e logging estruturado.
/// </summary>
public sealed class LmStudioHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly LmStudioOptions _options;
    private readonly ILogger<LmStudioHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LmStudioHttpClient(
        HttpClient httpClient,
        IOptions<LmStudioOptions> options,
        ILogger<LmStudioHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Envia requisição de chat e retorna a resposta completa.</summary>
    public async Task<LmStudioChatResponse?> ChatAsync(
        LmStudioChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                _logger.LogInformation(
                    "LM Studio chat request attempt {Attempt}/{Max} for model {Model}",
                    attempt, _options.MaxRetries + 1, request.Model);

                var sw = Stopwatch.StartNew();
                var response = await _httpClient.PostAsJsonAsync(
                    "chat/completions", request, JsonOptions, cancellationToken);

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<LmStudioChatResponse>(
                    JsonOptions, cancellationToken);

                sw.Stop();
                _logger.LogInformation(
                    "LM Studio chat completed in {ElapsedMs}ms for model {Model}",
                    sw.ElapsedMilliseconds, request.Model);

                return result;
            }
            catch (Exception ex) when (attempt <= _options.MaxRetries
                && ex is HttpRequestException or TaskCanceledException { InnerException: TimeoutException })
            {
                _logger.LogWarning(ex,
                    "LM Studio chat failed (attempt {Attempt}/{Max}), retrying...",
                    attempt, _options.MaxRetries + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LM Studio chat failed for model {Model}", request.Model);
                throw;
            }
        }
    }

    /// <summary>
    /// Verifica se o servidor LM Studio está activo chamando GET /v1/models.
    /// Retorna true se o servidor responde com HTTP 2xx.
    /// </summary>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("models", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LM Studio health check failed");
            return false;
        }
    }

    /// <summary>Lista os modelos disponíveis no servidor LM Studio.</summary>
    public async Task<LmStudioModelsResponse?> ListModelsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("models", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LmStudioModelsResponse>(
                JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list LM Studio models");
            return null;
        }
    }
}

// ── Request / Response DTOs ─────────────────────────────────────────────────
// LM Studio segue a especificação OpenAI Chat Completions, portanto os
// DTOs são compatíveis e não requerem mapeamento adicional.

/// <summary>Requisição de chat compatível com OpenAI enviada para o LM Studio.</summary>
public sealed class LmStudioChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public IList<LmStudioChatMessage> Messages { get; set; } = [];

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.3;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 2048;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

/// <summary>Mensagem individual no histórico de conversa.</summary>
public sealed class LmStudioChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>Resposta de chat do servidor LM Studio.</summary>
public sealed class LmStudioChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("choices")]
    public IList<LmStudioChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public LmStudioUsage? Usage { get; set; }
}

/// <summary>Choice (geração) retornada pelo LM Studio.</summary>
public sealed class LmStudioChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public LmStudioChatMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>Estatísticas de uso de tokens.</summary>
public sealed class LmStudioUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>Resposta da listagem de modelos disponíveis no LM Studio.</summary>
public sealed class LmStudioModelsResponse
{
    [JsonPropertyName("data")]
    public IList<LmStudioModelInfo> Data { get; set; } = [];
}

/// <summary>Informação de um modelo disponível no servidor LM Studio.</summary>
public sealed class LmStudioModelInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }
}
