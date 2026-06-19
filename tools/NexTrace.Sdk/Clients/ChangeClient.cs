using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NexTrace.Sdk.Clients;

/// <summary>
/// Sub-cliente para Change Intelligence da API NexTraceOne.
/// Permite consultar confidence score, estado de mudanças e solicitar promoções.
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

    /// <summary>
    /// Retorna o resumo de mudanças para uma release.
    /// </summary>
    public async Task<ChangeSummary?> GetSummaryAsync(string releaseId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(releaseId)) throw new ArgumentNullException(nameof(releaseId));
        return await _http.GetFromJsonAsync<ChangeSummary>(
            $"/api/v1/changes/{Uri.EscapeDataString(releaseId)}/summary", ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Retorna as métricas DORA para o período solicitado.
    /// </summary>
    public async Task<DoraMetrics?> GetDoraMetricsAsync(string? team = null, string? period = null, CancellationToken ct = default)
    {
        var query = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(team))
            query.Append($"team={Uri.EscapeDataString(team)}&");
        if (!string.IsNullOrWhiteSpace(period))
            query.Append($"period={Uri.EscapeDataString(period)}&");

        var url = "/api/v1/changes/dora-metrics";
        if (query.Length > 0)
        {
            query.Insert(0, '?');
            url += query.ToString().TrimEnd('&');
        }

        return await _http.GetFromJsonAsync<DoraMetrics>(url, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Solicita a promoção de uma mudança para o próximo ambiente.
    /// </summary>
    public async Task<PromotionRequest?> RequestPromotionAsync(PromotionRequestRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await _http.PostAsJsonAsync("/api/v1/promotion/requests", request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PromotionRequest>(ct).ConfigureAwait(false);
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

/// <summary>
/// Resumo de mudanças de uma release.
/// </summary>
public sealed class ChangeSummary
{
    [JsonPropertyName("releaseId")]
    public string? ReleaseId { get; init; }

    [JsonPropertyName("totalChanges")]
    public int TotalChanges { get; init; }

    [JsonPropertyName("breakingChanges")]
    public int BreakingChanges { get; init; }

    [JsonPropertyName("riskLevel")]
    public string? RiskLevel { get; init; }
}

/// <summary>
/// Métricas DORA.
/// </summary>
public sealed class DoraMetrics
{
    [JsonPropertyName("deploymentFrequency")]
    public double DeploymentFrequency { get; init; }

    [JsonPropertyName("leadTimeForChanges")]
    public double LeadTimeForChanges { get; init; }

    [JsonPropertyName("changeFailureRate")]
    public double ChangeFailureRate { get; init; }

    [JsonPropertyName("timeToRestore")]
    public double TimeToRestore { get; init; }
}

/// <summary>
/// Request para solicitação de promoção.
/// </summary>
public sealed class PromotionRequestRequest
{
    [JsonPropertyName("releaseId")]
    public string ReleaseId { get; set; } = string.Empty;

    [JsonPropertyName("targetEnvironment")]
    public string TargetEnvironment { get; set; } = string.Empty;

    [JsonPropertyName("justification")]
    public string? Justification { get; set; }
}

/// <summary>
/// Resultado de uma solicitação de promoção.
/// </summary>
public sealed class PromotionRequest
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("releaseId")]
    public string? ReleaseId { get; init; }

    [JsonPropertyName("targetEnvironment")]
    public string? TargetEnvironment { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}
