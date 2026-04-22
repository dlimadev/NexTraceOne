using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NexTrace.Sdk.Clients;

/// <summary>
/// Sub-cliente para Compliance da API NexTraceOne.
/// Permite consultar a cobertura de um standard de compliance (ex: GDPR, SOC2).
/// </summary>
public sealed class ComplianceClient
{
    private readonly HttpClient _http;

    internal ComplianceClient(HttpClient http) => _http = http;

    /// <summary>
    /// Verifica a cobertura de um standard de compliance.
    /// </summary>
    public async Task<ComplianceCoverage?> CheckCoverageAsync(string standard, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(standard)) throw new ArgumentNullException(nameof(standard));
        return await _http.GetFromJsonAsync<ComplianceCoverage>(
            $"/api/v1/compliance/coverage-matrix?standard={Uri.EscapeDataString(standard)}", ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Resultado da cobertura de compliance para um standard específico.
/// </summary>
public sealed class ComplianceCoverage
{
    [JsonPropertyName("standard")]
    public string? Standard { get; init; }

    [JsonPropertyName("coveragePercent")]
    public double CoveragePercent { get; init; }

    [JsonPropertyName("controlsMet")]
    public int ControlsMet { get; init; }

    [JsonPropertyName("controlsTotal")]
    public int ControlsTotal { get; init; }

    [JsonPropertyName("gaps")]
    public string[]? Gaps { get; init; }
}
