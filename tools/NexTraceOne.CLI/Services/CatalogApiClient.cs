using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexTraceOne.CLI.Services;

/// <summary>
/// Cliente HTTP para o Catalog API do NexTraceOne.
/// Encapsula chamadas REST para listagem e consulta de serviços.
/// </summary>
public sealed class CatalogApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CatalogApiClient(string baseUrl, string? token = null)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(30)
        };
        if (!string.IsNullOrWhiteSpace(token))
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<CatalogListResponse> ListServicesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            "/api/v1/catalog/services", cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CatalogListResponse>(
            JsonOptions, cancellationToken).ConfigureAwait(false);

        return result ?? new CatalogListResponse([], 0);
    }

    public async Task<ServiceDetail?> GetServiceAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"/api/v1/catalog/services/{Uri.EscapeDataString(serviceId)}", cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ServiceDetail>(
            JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() => _httpClient.Dispose();
}

/// <summary>Resposta da listagem de serviços do catálogo.</summary>
public sealed record CatalogListResponse(
    IReadOnlyList<ServiceListItem> Items,
    int TotalCount);

/// <summary>Item resumido de serviço na listagem.</summary>
public sealed record ServiceListItem(
    string ServiceId,
    string Name,
    string? Domain,
    string? Owner,
    string? Criticality,
    string? Status);

/// <summary>Detalhe completo de um serviço.</summary>
public sealed record ServiceDetail(
    string ServiceId,
    string Name,
    string? Domain,
    string? Owner,
    string? Team,
    string? Criticality,
    string? Status,
    string? Description,
    string? Type,
    string? Version,
    string[]? Tags,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt);
