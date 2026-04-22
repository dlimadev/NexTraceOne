using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NexTrace.Sdk.Clients;

/// <summary>
/// Sub-cliente para acesso ao Service Catalog da API NexTraceOne.
/// Permite consultar serviços por nome ou equipa.
/// </summary>
public sealed class ServiceCatalogClient
{
    private readonly HttpClient _http;

    internal ServiceCatalogClient(HttpClient http) => _http = http;

    /// <summary>
    /// Retorna os detalhes de um serviço pelo nome canónico.
    /// </summary>
    public async Task<ServiceSummary?> GetServiceAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        return await _http.GetFromJsonAsync<ServiceSummary>(
            $"/api/v1/catalog/services/{Uri.EscapeDataString(name)}", ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Lista serviços, opcionalmente filtrados pela equipa proprietária.
    /// </summary>
    public async Task<IReadOnlyList<ServiceSummary>> ListServicesAsync(string? team = null, CancellationToken ct = default)
    {
        var url = team is null
            ? "/api/v1/catalog/services"
            : $"/api/v1/catalog/services?team={Uri.EscapeDataString(team)}";

        var result = await _http.GetFromJsonAsync<List<ServiceSummary>>(url, ct).ConfigureAwait(false);
        return result ?? new List<ServiceSummary>();
    }
}

/// <summary>
/// Resumo de um serviço retornado pelo Service Catalog.
/// </summary>
public sealed class ServiceSummary
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("team")]
    public string? Team { get; init; }

    [JsonPropertyName("tier")]
    public string? Tier { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}
