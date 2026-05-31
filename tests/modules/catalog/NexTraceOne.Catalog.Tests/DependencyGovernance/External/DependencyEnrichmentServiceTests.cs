using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Infrastructure.DependencyGovernance.External;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance.External;

public sealed class DependencyEnrichmentServiceTests
{
    [Fact]
    public async Task EnrichAsync_NuGetPackage_UpdatesLatestVersionAndLicense()
    {
        var metadata = Substitute.For<IPackageMetadataClient>();
        metadata.GetLatestStableVersionAsync("Newtonsoft.Json", Arg.Any<CancellationToken>()).Returns("13.0.3");
        metadata.GetDeprecationInfoAsync("Newtonsoft.Json", "12.0.1", Arg.Any<CancellationToken>()).Returns((false, (string?)null));
        metadata.GetLicenseAsync("Newtonsoft.Json", "12.0.1", Arg.Any<CancellationToken>()).Returns("MIT");

        var vulnSource = Substitute.For<IVulnerabilityDataSource>();
        vulnSource.SourceName.Returns("OSV");
        vulnSource.QueryAsync("NuGet", "Newtonsoft.Json", "12.0.1", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VulnerabilityData>());

        var service = new DependencyEnrichmentService(
            new[] { vulnSource },
            metadata,
            NullLogger<DependencyEnrichmentService>.Instance);

        var profile = ServiceDependencyProfile.Create(Guid.NewGuid());
        var dep = PackageDependency.Create(profile.Id.Value, "Newtonsoft.Json", "12.0.1", PackageEcosystem.NuGet, true);
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        await service.EnrichAsync(profile);

        dep.LatestStableVersion.Should().Be("13.0.3");
        dep.License.Should().Be("MIT");
        dep.LicenseRisk.Should().Be(LicenseRiskLevel.Low);
        dep.IsOutdated.Should().BeTrue();
        profile.HealthScore.Should().Be(90); // 100 - 10 because 100% outdated > 20% threshold
    }

    [Fact]
    public async Task EnrichAsync_WithVulnerabilities_AddsVulnerabilitiesAndRecalculatesHealth()
    {
        var metadata = Substitute.For<IPackageMetadataClient>();
        metadata.GetLatestStableVersionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("2.0.0");
        metadata.GetDeprecationInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((false, (string?)null));
        metadata.GetLicenseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("MIT");

        var vulnSource = Substitute.For<IVulnerabilityDataSource>();
        vulnSource.SourceName.Returns("OSV");
        vulnSource.QueryAsync("NuGet", "VulnPackage", "1.0.0", Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new VulnerabilityData(
                    AdvisoryId: "GHSA-TEST",
                    CveId: "CVE-2024-TEST",
                    Summary: "Test vuln",
                    Severity: "CRITICAL",
                    CvssScore: 9.8,
                    AffectedVersionRange: "*",
                    FixedVersion: "2.0.0",
                    PublishedAt: DateTimeOffset.UtcNow,
                    SourceUrl: "https://osv.dev/GHSA-TEST")
            });

        var service = new DependencyEnrichmentService(
            new[] { vulnSource },
            metadata,
            NullLogger<DependencyEnrichmentService>.Instance);

        var profile = ServiceDependencyProfile.Create(Guid.NewGuid());
        var dep = PackageDependency.Create(profile.Id.Value, "VulnPackage", "1.0.0", PackageEcosystem.NuGet, true);
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        await service.EnrichAsync(profile);

        dep.Vulnerabilities.Should().HaveCount(1);
        dep.Vulnerabilities[0].CveId.Should().Be("CVE-2024-TEST");
        dep.Vulnerabilities[0].Severity.Should().Be(VulnerabilitySeverity.Critical);
        profile.HealthScore.Should().Be(65); // 100 - 25 critical - 10 outdated ratio
    }

    [Fact]
    public async Task EnrichAsync_NonNuGetPackage_SkipsMetadataButQueriesVulnerabilities()
    {
        var metadata = Substitute.For<IPackageMetadataClient>();
        var vulnSource = Substitute.For<IVulnerabilityDataSource>();
        vulnSource.SourceName.Returns("OSV");
        vulnSource.QueryAsync("Npm", "lodash", "4.17.20", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VulnerabilityData>());

        var service = new DependencyEnrichmentService(
            new[] { vulnSource },
            metadata,
            NullLogger<DependencyEnrichmentService>.Instance);

        var profile = ServiceDependencyProfile.Create(Guid.NewGuid());
        var dep = PackageDependency.Create(profile.Id.Value, "lodash", "4.17.20", PackageEcosystem.Npm, true);
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        await service.EnrichAsync(profile);

        await metadata.DidNotReceive().GetLatestStableVersionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        dep.LatestStableVersion.Should().BeNull();
    }
}
