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
/// Permite consultar, criar, atualizar e remover serviços.
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

    /// <summary>
    /// Cria um novo serviço no catálogo.
    /// </summary>
    public async Task<ServiceSummary?> CreateServiceAsync(CreateServiceRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await _http.PostAsJsonAsync("/api/v1/catalog/services", request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceSummary>(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Atualiza um serviço existente pelo nome canónico.
    /// </summary>
    public async Task<ServiceSummary?> UpdateServiceAsync(string name, UpdateServiceRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        ArgumentNullException.ThrowIfNull(request);
        var response = await _http.PutAsJsonAsync($"/api/v1/catalog/services/{Uri.EscapeDataString(name)}", request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceSummary>(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Remove um serviço pelo nome canónico.
    /// </summary>
    public async Task DeleteServiceAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        var response = await _http.DeleteAsync($"/api/v1/catalog/services/{Uri.EscapeDataString(name)}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Executa o scaffold de um serviço a partir de um template.
    /// </summary>
    public async Task<ScaffoldResult?> ScaffoldAsync(ScaffoldRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = request.TemplateId is not null
            ? $"/api/v1/catalog/templates/{Uri.EscapeDataString(request.TemplateId)}/scaffold"
            : $"/api/v1/catalog/templates/slug/{Uri.EscapeDataString(request.TemplateSlug!)}/scaffold";

        var response = await _http.PostAsJsonAsync(url, request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ScaffoldResult>(ct).ConfigureAwait(false);
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

/// <summary>
/// Request para criação de um serviço no catálogo.
/// </summary>
public sealed class CreateServiceRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("team")]
    public string Team { get; set; } = string.Empty;

    [JsonPropertyName("tier")]
    public string Tier { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Request para atualização de um serviço no catálogo.
/// </summary>
public sealed class UpdateServiceRequest
{
    [JsonPropertyName("team")]
    public string? Team { get; set; }

    [JsonPropertyName("tier")]
    public string? Tier { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Request para execução de scaffold de um serviço.
/// </summary>
public sealed class ScaffoldRequest
{
    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }

    [JsonPropertyName("templateSlug")]
    public string? TemplateSlug { get; set; }

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, string>? Parameters { get; set; }
}

/// <summary>
/// Resultado de uma operação de scaffold.
/// </summary>
public sealed class ScaffoldResult
{
    [JsonPropertyName("serviceId")]
    public string? ServiceId { get; init; }

    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; init; }

    [JsonPropertyName("files")]
    public IReadOnlyList<string>? Files { get; init; }

    [JsonPropertyName("outputPath")]
    public string? OutputPath { get; init; }
}
