using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NexTrace.Sdk.Clients;

/// <summary>
/// Sub-cliente para Change Intelligence da API NexTraceOne.
/// Permite consultar o confidence score de uma release e o estado de uma mudança por SHA.
/// </summary>
public sealed class ChangeClient
{
    private readonly HttpClient _http;

    internal ChangeClient(HttpClient http) => _http = http;

    /// <summary>
    /// Retorna o confidence score de uma release pelo identificador.
    /// </summary>
    public async Task<ConfidenceScore?> GetConfidenceScoreAsync(string releaseId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(releaseId)) throw new ArgumentNullException(nameof(releaseId));
        return await _http.GetFromJsonAsync<ConfidenceScore>(
            $"/api/v1/changes/{Uri.EscapeDataString(releaseId)}/confidence", ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Retorna o estado de uma mudança pelo commit SHA.
    /// </summary>
    public async Task<ChangeStatus?> GetChangeStatusAsync(string sha, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sha)) throw new ArgumentNullException(nameof(sha));
        return await _http.GetFromJsonAsync<ChangeStatus>(
            $"/api/v1/changes/by-sha/{Uri.EscapeDataString(sha)}", ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Resultado do confidence score de uma release.
/// </summary>
public sealed class ConfidenceScore
{
    [JsonPropertyName("releaseId")]
    public string? ReleaseId { get; init; }

    [JsonPropertyName("score")]
    public double Score { get; init; }

    [JsonPropertyName("tier")]
    public string? Tier { get; init; }

    [JsonPropertyName("recommendation")]
    public string? Recommendation { get; init; }
}

/// <summary>
/// Estado de uma mudança identificada por SHA de commit.
/// </summary>
public sealed class ChangeStatus
{
    [JsonPropertyName("sha")]
    public string? Sha { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("environment")]
    public string? Environment { get; init; }

    [JsonPropertyName("promotedAt")]
    public DateTimeOffset? PromotedAt { get; init; }
}
