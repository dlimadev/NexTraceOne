using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.DependencyGovernance.External;

/// <summary>
/// Cliente para a API pública do NuGet.org (v3).
/// Documentação: https://learn.microsoft.com/nuget/api/overview
/// </summary>
internal sealed class NuGetPackageClient : IPackageMetadataClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NuGetPackageClient> _logger;

    public NuGetPackageClient(HttpClient httpClient, ILogger<NuGetPackageClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> GetLatestStableVersionAsync(
        string packageName, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLowerInvariant()}/index.json";
        try
        {
            var response = await _httpClient.GetFromJsonAsync<NuGetVersionsResponse>(url, cancellationToken);
            return response?.Versions?
                .Where(v => !v.Contains('-', StringComparison.Ordinal)) // exclui prerelease
                .MaxBy(v => v, SemVerComparer.Instance);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("NuGet package {Package} not found.", packageName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NuGet version lookup failed for {Package}.", packageName);
            return null;
        }
    }

    public async Task<(bool IsDeprecated, string? DeprecationMessage)> GetDeprecationInfoAsync(
        string packageName, string version, CancellationToken cancellationToken = default)
    {
        // NuGet não expõe depreciação diretamente na API v3 simples.
        // Usamos o registration endpoint como fallback.
        var registrationUrl = $"https://api.nuget.org/v3/registration5-semver1/{packageName.ToLowerInvariant()}/{version.ToLowerInvariant()}.json";
        try
        {
            var response = await _httpClient.GetFromJsonAsync<NuGetRegistrationLeaf>(registrationUrl, cancellationToken);
            if (response?.CatalogEntry?.Deprecation is not null)
            {
                return (true, response.CatalogEntry.Deprecation.Message);
            }
            return (false, null);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NuGet deprecation lookup failed for {Package}@{Version}.", packageName, version);
            return (false, null);
        }
    }

    public async Task<string?> GetLicenseAsync(
        string packageName, string version, CancellationToken cancellationToken = default)
    {
        var registrationUrl = $"https://api.nuget.org/v3/registration5-semver1/{packageName.ToLowerInvariant()}/{version.ToLowerInvariant()}.json";
        try
        {
            var response = await _httpClient.GetFromJsonAsync<NuGetRegistrationLeaf>(registrationUrl, cancellationToken);
            return response?.CatalogEntry?.LicenseExpression;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NuGet license lookup failed for {Package}@{Version}.", packageName, version);
            return null;
        }
    }
}

// --- NuGet API DTOs ---

internal sealed record NuGetVersionsResponse(List<string> Versions);

internal sealed record NuGetRegistrationLeaf(
    [property: JsonPropertyName("catalogEntry")]
    NuGetCatalogEntry? CatalogEntry);

internal sealed record NuGetCatalogEntry(
    [property: JsonPropertyName("licenseExpression")]
    string? LicenseExpression,
    NuGetDeprecation? Deprecation);

internal sealed record NuGetDeprecation(string? Message);

/// <summary>
/// Comparador SemVer simplificado para NuGet (major.minor.patch[-prerelease]).
/// </summary>
internal sealed class SemVerComparer : IComparer<string>
{
    public static SemVerComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var xParts = x.Split(['.', '-'], StringSplitOptions.RemoveEmptyEntries);
        var yParts = y.Split(['.', '-'], StringSplitOptions.RemoveEmptyEntries);

        var max = Math.Min(xParts.Length, yParts.Length);
        for (var i = 0; i < max; i++)
        {
            var xIsNum = int.TryParse(xParts[i], out var xNum);
            var yIsNum = int.TryParse(yParts[i], out var yNum);

            if (xIsNum && yIsNum)
            {
                var cmp = xNum.CompareTo(yNum);
                if (cmp != 0) return cmp;
            }
            else
            {
                var cmp = string.CompareOrdinal(xParts[i], yParts[i]);
                if (cmp != 0) return cmp;
            }
        }

        return xParts.Length.CompareTo(yParts.Length);
    }
}
