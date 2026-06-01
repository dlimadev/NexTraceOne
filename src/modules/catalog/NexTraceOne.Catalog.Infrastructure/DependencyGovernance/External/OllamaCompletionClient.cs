using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.DependencyGovernance.External;

/// <summary>
/// Cliente de completão LLM via Ollama API local.
/// Documentação: https://github.com/ollama/ollama/blob/main/docs/api.md
/// </summary>
internal sealed class OllamaCompletionClient : ILlmCompletionClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaCompletionClient> _logger;
    private readonly string _model;

    public OllamaCompletionClient(HttpClient httpClient, ILogger<OllamaCompletionClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = "llama3.2:3b";
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var request = new OllamaRequest(_model, prompt, Stream: false);
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/generate",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken);
            return result?.Response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ollama completion failed.");
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Ollama completion cancelled.");
            throw;
        }
    }

    private sealed record OllamaRequest(string Model, string Prompt, bool Stream);

    private sealed record OllamaResponse(
        [property: JsonPropertyName("response")]
        string? Response);
}
