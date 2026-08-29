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

    /// <summary>
    /// Assina digitalmente um artefato (docker-image, nuget-package, binary) via Cosign,
    /// gerando SBOM e entrada no transparency log (Rekor).
    /// </summary>
    public async Task<SignedArtifact?> SignArtifactAsync(SignArtifactRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await _http.PostAsJsonAsync(
            "/api/v1/governance/artifact-signing/sign", request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SignedArtifact>(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifica a assinatura digital de um artefato pelo seu identificador.
    /// </summary>
    public async Task<ArtifactVerification?> VerifyArtifactAsync(string artifactId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(artifactId)) throw new ArgumentNullException(nameof(artifactId));
        var response = await _http.PostAsJsonAsync(
            "/api/v1/governance/artifact-signing/verify", new VerifyArtifactRequest { ArtifactId = artifactId }, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ArtifactVerification>(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gera o SBOM (Software Bill of Materials) de um serviço no formato solicitado.
    /// </summary>
    /// <param name="serviceId">Identificador do serviço.</param>
    /// <param name="format">Formato do SBOM: CycloneDx (padrão do servidor) ou Spdx.</param>
    public async Task<SbomResult?> GenerateSbomAsync(string serviceId, string? format = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentNullException(nameof(serviceId));

        // Corpo vazio → servidor usa o formato padrão (CycloneDx). Enum serializado como string.
        object body = new { };
        if (!string.IsNullOrWhiteSpace(format))
            body = new { format };
        var response = await _http.PostAsJsonAsync(
            $"/api/v1/catalog/dependencies/{Uri.EscapeDataString(serviceId)}/sbom", body, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SbomResult>(ct).ConfigureAwait(false);
    }
}

/// <summary>Request para assinatura digital de um artefato.</summary>
public sealed class SignArtifactRequest
{
    [JsonPropertyName("artifactPath")]
    public string ArtifactPath { get; set; } = string.Empty;

    /// <summary>Tipo do artefato: docker-image, nuget-package ou binary.</summary>
    [JsonPropertyName("artifactType")]
    public string ArtifactType { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }
}

/// <summary>Resultado da assinatura de um artefato.</summary>
public sealed class SignedArtifact
{
    [JsonPropertyName("artifactId")]
    public string? ArtifactId { get; init; }

    [JsonPropertyName("artifactName")]
    public string? ArtifactName { get; init; }

    [JsonPropertyName("checksum")]
    public string? Checksum { get; init; }

    [JsonPropertyName("signature")]
    public string? Signature { get; init; }

    [JsonPropertyName("signedAt")]
    public DateTimeOffset SignedAt { get; init; }

    [JsonPropertyName("signerIdentity")]
    public string? SignerIdentity { get; init; }

    [JsonPropertyName("sbomJson")]
    public string? SbomJson { get; init; }

    [JsonPropertyName("transparencyLogEntry")]
    public string? TransparencyLogEntry { get; init; }
}

/// <summary>Request para verificação de assinatura.</summary>
public sealed class VerifyArtifactRequest
{
    [JsonPropertyName("artifactId")]
    public string ArtifactId { get; set; } = string.Empty;
}

/// <summary>Resultado da verificação de assinatura de um artefato.</summary>
public sealed class ArtifactVerification
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; init; }

    [JsonPropertyName("artifactId")]
    public string? ArtifactId { get; init; }

    [JsonPropertyName("verifiedAt")]
    public DateTimeOffset? VerifiedAt { get; init; }

    [JsonPropertyName("signerIdentity")]
    public string? SignerIdentity { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<string> Errors { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
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

/// <summary>Resultado da geração de SBOM de um serviço.</summary>
public sealed class SbomResult
{
    /// <summary>Conteúdo do SBOM (JSON do formato escolhido).</summary>
    [JsonPropertyName("sbomContent")]
    public string? SbomContent { get; init; }

    /// <summary>Formato do SBOM gerado (CycloneDx ou Spdx).</summary>
    [JsonPropertyName("format")]
    public string? Format { get; init; }
}
