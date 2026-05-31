using Microsoft.Extensions.Logging;

using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Infrastructure.DependencyGovernance.External;

/// <summary>
/// Enriquece um <see cref="ServiceDependencyProfile"/> com dados de registries públicos
/// (vulnerabilidades via OSV, metadados via NuGet.org).
/// </summary>
internal sealed class DependencyEnrichmentService : IDependencyEnrichmentService
{
    private readonly IEnumerable<IVulnerabilityDataSource> _vulnSources;
    private readonly IPackageMetadataClient _metadataClient;
    private readonly ILogger<DependencyEnrichmentService> _logger;

    public DependencyEnrichmentService(
        IEnumerable<IVulnerabilityDataSource> vulnSources,
        IPackageMetadataClient metadataClient,
        ILogger<DependencyEnrichmentService> logger)
    {
        _vulnSources = vulnSources;
        _metadataClient = metadataClient;
        _logger = logger;
    }

    /// <summary>
    /// Enriquece todas as dependências do perfil.
    /// </summary>
    public async Task EnrichAsync(
        ServiceDependencyProfile profile,
        CancellationToken cancellationToken = default)
    {
        foreach (var dep in profile.Dependencies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await EnrichDependencyAsync(dep, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich dependency {Package}@{Version}.", dep.PackageName, dep.Version);
            }
        }

        profile.RecalculateHealthScore();
    }

    private async Task EnrichDependencyAsync(PackageDependency dep, CancellationToken ct)
    {
        // ── Metadados do registry ──────────────────────────────────────
        if (dep.Ecosystem == PackageEcosystem.NuGet)
        {
            var latest = await _metadataClient.GetLatestStableVersionAsync(dep.PackageName, ct);
            if (!string.IsNullOrEmpty(latest))
            {
                dep.UpdateLatestVersion(latest);
            }

            var (isDeprecated, deprecationMsg) = await _metadataClient.GetDeprecationInfoAsync(dep.PackageName, dep.Version, ct);
            if (isDeprecated && !string.IsNullOrEmpty(latest))
            {
                dep.MarkAsOutdated(latest);
            }

            var license = await _metadataClient.GetLicenseAsync(dep.PackageName, dep.Version, ct);
            if (!string.IsNullOrEmpty(license))
            {
                dep.UpdateLicense(license);
            }
        }

        // ── Vulnerabilidades ───────────────────────────────────────────
        var ecosystemName = dep.Ecosystem.ToString();
        foreach (var source in _vulnSources)
        {
            var vulns = await source.QueryAsync(ecosystemName, dep.PackageName, dep.Version, ct);
            foreach (var v in vulns)
            {
                dep.AddVulnerability(new PackageVulnerability(
                    CveId: v.CveId ?? v.AdvisoryId,
                    Severity: MapSeverity(v.Severity),
                    CvssScore: (decimal)(v.CvssScore ?? 0),
                    Description: v.Summary,
                    AffectedVersionRange: v.AffectedVersionRange ?? "*",
                    FixedInVersion: v.FixedVersion,
                    PublishedAt: v.PublishedAt ?? DateTimeOffset.UtcNow,
                    Source: $"{source.SourceName}:{v.AdvisoryId}",
                    ExploitMaturity: ExploitMaturity.NotDefined));
            }
        }
    }

    private static VulnerabilitySeverity MapSeverity(string severity)
    {
        return severity.ToUpperInvariant() switch
        {
            "CRITICAL" => VulnerabilitySeverity.Critical,
            "HIGH" => VulnerabilitySeverity.High,
            "MEDIUM" or "MODERATE" => VulnerabilitySeverity.Medium,
            "LOW" => VulnerabilitySeverity.Low,
            _ => VulnerabilitySeverity.None
        };
    }
}
