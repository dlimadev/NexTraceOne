using Ardalis.GuardClauses;

using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;

/// <summary>
/// Dependência de pacote identificada no perfil de dependências de um serviço.
/// </summary>
public sealed class PackageDependency
{
    private PackageDependency() { }

    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public string PackageName { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public PackageEcosystem Ecosystem { get; private set; }
    public bool IsDirect { get; private set; }
    public string? License { get; private set; }
    public LicenseRiskLevel LicenseRisk { get; private set; }
    public string? LatestStableVersion { get; private set; }
    public bool IsOutdated { get; private set; }
    public string? DeprecationNotice { get; private set; }

    public IReadOnlyList<PackageVulnerability> Vulnerabilities { get; private set; } = new List<PackageVulnerability>();

    public static PackageDependency Create(
        Guid profileId,
        string packageName,
        string version,
        PackageEcosystem ecosystem,
        bool isDirect,
        string? license = null,
        LicenseRiskLevel licenseRisk = LicenseRiskLevel.Low,
        string? deprecationNotice = null)
    {
        Guard.Against.NullOrWhiteSpace(packageName);
        Guard.Against.NullOrWhiteSpace(version);
        return new PackageDependency
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            PackageName = packageName.Trim(),
            Version = version.Trim(),
            Ecosystem = ecosystem,
            IsDirect = isDirect,
            License = license,
            LicenseRisk = licenseRisk,
            DeprecationNotice = deprecationNotice
        };
    }

    public void AddVulnerability(PackageVulnerability vuln)
    {
        Guard.Against.Null(vuln);
        var list = Vulnerabilities.ToList();
        list.Add(vuln);
        Vulnerabilities = list.AsReadOnly();
    }

    public void MarkAsOutdated(string latestVersion)
    {
        Guard.Against.NullOrWhiteSpace(latestVersion);
        IsOutdated = true;
        LatestStableVersion = latestVersion;
    }
}
