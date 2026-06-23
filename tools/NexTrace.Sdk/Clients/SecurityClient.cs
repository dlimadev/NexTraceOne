using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NexTrace.Sdk.Clients;

/// <summary>
/// Sub-cliente para governança de segurança de dependências (supply chain) da API NexTraceOne.
/// Expõe a saúde de dependências de um serviço e o inventário de serviços vulneráveis,
/// alimentado pelo enriquecimento ao vivo de OSV e NuGet.org.
/// </summary>
public sealed class SecurityClient
{
    private readonly HttpClient _http;

    internal SecurityClient(HttpClient http) => _http = http;

    /// <summary>
    /// Retorna o painel de saúde de dependências de um serviço (score, contagem de vulnerabilidades por severidade).
    /// </summary>
    public async Task<DependencyHealth?> GetDependencyHealthAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentNullException(nameof(serviceId));
        return await _http.GetFromJsonAsync<DependencyHealth>(
            $"/api/v1/catalog/dependencies/{Uri.EscapeDataString(serviceId)}/health", ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Lista os serviços com vulnerabilidades iguais ou acima da severidade mínima informada.
    /// </summary>
    /// <param name="minSeverity">Severidade mínima: Low, Medium, High ou Critical. Padrão do servidor: High.</param>
    public async Task<IReadOnlyList<VulnerableService>> ListVulnerableServicesAsync(
        string? minSeverity = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(minSeverity)
            ? "/api/v1/catalog/dependencies/vulnerable"
            : $"/api/v1/catalog/dependencies/vulnerable?minSeverity={Uri.EscapeDataString(minSeverity)}";

        var result = await _http.GetFromJsonAsync<List<VulnerableService>>(url, ct).ConfigureAwait(false);
        return result ?? [];
    }
}

/// <summary>Painel de saúde de dependências de um serviço.</summary>
public sealed class DependencyHealth
{
    [JsonPropertyName("serviceId")]
    public string? ServiceId { get; init; }

    [JsonPropertyName("healthScore")]
    public int HealthScore { get; init; }

    [JsonPropertyName("lastScanAt")]
    public DateTimeOffset LastScanAt { get; init; }

    [JsonPropertyName("totalDeps")]
    public int TotalDeps { get; init; }

    [JsonPropertyName("directDeps")]
    public int DirectDeps { get; init; }

    [JsonPropertyName("transitiveDeps")]
    public int TransitiveDeps { get; init; }

    [JsonPropertyName("criticalVulnCount")]
    public int CriticalVulnCount { get; init; }

    [JsonPropertyName("highVulnCount")]
    public int HighVulnCount { get; init; }

    [JsonPropertyName("mediumVulnCount")]
    public int MediumVulnCount { get; init; }

    [JsonPropertyName("lowVulnCount")]
    public int LowVulnCount { get; init; }

    [JsonPropertyName("outdatedCount")]
    public int OutdatedCount { get; init; }

    [JsonPropertyName("deprecatedCount")]
    public int DeprecatedCount { get; init; }

    [JsonPropertyName("licenseRiskCounts")]
    public IReadOnlyDictionary<string, int> LicenseRiskCounts { get; init; } = new Dictionary<string, int>();
}

/// <summary>Resumo de um serviço com vulnerabilidades acima do limiar.</summary>
public sealed class VulnerableService
{
    [JsonPropertyName("profileId")]
    public string? ProfileId { get; init; }

    [JsonPropertyName("serviceId")]
    public string? ServiceId { get; init; }

    [JsonPropertyName("healthScore")]
    public int HealthScore { get; init; }

    [JsonPropertyName("criticalCount")]
    public int CriticalCount { get; init; }

    [JsonPropertyName("highCount")]
    public int HighCount { get; init; }

    [JsonPropertyName("mediumCount")]
    public int MediumCount { get; init; }

    [JsonPropertyName("lastScanAt")]
    public DateTimeOffset LastScanAt { get; init; }
}
