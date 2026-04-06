using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class DependencyGovernanceTests
{
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid TemplateId = Guid.NewGuid();

    [Fact]
    public void ServiceDependencyProfile_Create_SetsDefaults()
    {
        var profile = ServiceDependencyProfile.Create(ServiceId, TemplateId);

        profile.ServiceId.Should().Be(ServiceId);
        profile.TemplateId.Should().Be(TemplateId);
        profile.HealthScore.Should().Be(100);
        profile.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void ServiceDependencyProfile_Create_WithoutTemplateId_IsAllowed()
    {
        var profile = ServiceDependencyProfile.Create(ServiceId);
        profile.TemplateId.Should().BeNull();
    }

    [Fact]
    public void ServiceDependencyProfile_Create_EmptyServiceId_Throws()
    {
        var action = () => ServiceDependencyProfile.Create(Guid.Empty);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void ServiceDependencyProfile_UpdateScan_SetsDependencies()
    {
        var profile = ServiceDependencyProfile.Create(ServiceId);
        var deps = new[]
        {
            PackageDependency.Create(profile.Id.Value, "Newtonsoft.Json", "13.0.0", PackageEcosystem.NuGet, isDirect: true),
            PackageDependency.Create(profile.Id.Value, "Serilog", "3.0.0", PackageEcosystem.NuGet, isDirect: false)
        };

        profile.UpdateScan(deps, null, SbomFormat.CycloneDx);

        profile.TotalDependencies.Should().Be(2);
        profile.DirectDependencies.Should().Be(1);
        profile.TransitiveDependencies.Should().Be(1);
    }

    [Fact]
    public void ServiceDependencyProfile_UpdateScan_CalculatesHealthScore_WithCriticalVuln()
    {
        var profile = ServiceDependencyProfile.Create(ServiceId);
        var dep = PackageDependency.Create(profile.Id.Value, "vuln-pkg", "1.0.0", PackageEcosystem.NuGet, isDirect: true);
        dep.AddVulnerability(new PackageVulnerability(
            "CVE-2024-001", VulnerabilitySeverity.Critical, 9.8m,
            "Critical RCE", "< 2.0.0", "2.0.0",
            DateTimeOffset.UtcNow, "NVD", ExploitMaturity.Active));

        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        profile.HealthScore.Should().Be(75); // 100 - 25 for critical
    }

    [Fact]
    public void ServiceDependencyProfile_HealthScore_DoesNotGoBelowZero()
    {
        var profile = ServiceDependencyProfile.Create(ServiceId);
        var dep = PackageDependency.Create(profile.Id.Value, "vuln-pkg", "1.0.0", PackageEcosystem.NuGet, isDirect: true);
        for (int i = 0; i < 10; i++)
        {
            dep.AddVulnerability(new PackageVulnerability(
                $"CVE-2024-{i:000}", VulnerabilitySeverity.Critical, 9.8m,
                "Critical RCE", "< 2.0.0", null,
                DateTimeOffset.UtcNow, "NVD", ExploitMaturity.Active));
        }

        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        profile.HealthScore.Should().Be(0);
    }

    [Fact]
    public void PackageDependency_Create_SetsProperties()
    {
        var dep = PackageDependency.Create(
            Guid.NewGuid(), "MyPackage", "1.0.0", PackageEcosystem.NuGet,
            isDirect: true, license: "MIT", licenseRisk: LicenseRiskLevel.Low);

        dep.PackageName.Should().Be("MyPackage");
        dep.Version.Should().Be("1.0.0");
        dep.Ecosystem.Should().Be(PackageEcosystem.NuGet);
        dep.IsDirect.Should().BeTrue();
        dep.License.Should().Be("MIT");
        dep.LicenseRisk.Should().Be(LicenseRiskLevel.Low);
        dep.Vulnerabilities.Should().BeEmpty();
    }

    [Fact]
    public void PackageDependency_Create_NullName_Throws()
    {
        var action = () => PackageDependency.Create(Guid.NewGuid(), null!, "1.0.0", PackageEcosystem.NuGet, true);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void PackageDependency_AddVulnerability_AppendsToList()
    {
        var dep = PackageDependency.Create(Guid.NewGuid(), "pkg", "1.0.0", PackageEcosystem.NuGet, true);
        var vuln = new PackageVulnerability(
            "CVE-2024-001", VulnerabilitySeverity.High, 7.5m,
            "SQL Injection", "< 2.0.0", "2.0.0",
            DateTimeOffset.UtcNow, "NVD", ExploitMaturity.Functional);

        dep.AddVulnerability(vuln);

        dep.Vulnerabilities.Should().HaveCount(1);
        dep.Vulnerabilities[0].CveId.Should().Be("CVE-2024-001");
    }

    [Fact]
    public void PackageDependency_MarkAsOutdated_SetsFlag()
    {
        var dep = PackageDependency.Create(Guid.NewGuid(), "pkg", "1.0.0", PackageEcosystem.NuGet, true);

        dep.MarkAsOutdated("2.0.0");

        dep.IsOutdated.Should().BeTrue();
        dep.LatestStableVersion.Should().Be("2.0.0");
    }

    [Fact]
    public void ServiceDependencyProfile_HealthScore_ReducedByOutdatedRatio()
    {
        var profile = ServiceDependencyProfile.Create(ServiceId);
        var deps = new List<PackageDependency>();
        for (int i = 0; i < 5; i++)
        {
            var dep = PackageDependency.Create(profile.Id.Value, $"pkg-{i}", "1.0.0", PackageEcosystem.NuGet, true);
            if (i < 2) dep.MarkAsOutdated("2.0.0"); // 40% outdated > 20% threshold
            deps.Add(dep);
        }

        profile.UpdateScan(deps, null, SbomFormat.CycloneDx);

        profile.HealthScore.Should().Be(90); // 100 - 10 for >20% outdated
    }
}
