using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetDependencyHealthDashboard;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class GetDependencyHealthDashboardTests
{
    [Fact]
    public async Task Handle_ProfileWithDeps_ReturnsDashboard()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "pkg", "1.0.0", PackageEcosystem.NuGet, true, "MIT");
        dep.MarkAsOutdated("2.0.0");
        dep.AddVulnerability(new PackageVulnerability(
            "CVE-001", VulnerabilitySeverity.Critical, 9.0m, "RCE", "<2.0", "2.0",
            DateTimeOffset.UtcNow, "NVD", ExploitMaturity.Active));
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new GetDependencyHealthDashboard.Handler(repo);

        var result = await handler.Handle(
            new GetDependencyHealthDashboard.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be(serviceId);
        result.Value.CriticalVulnCount.Should().Be(1);
        result.Value.OutdatedCount.Should().Be(1);
        result.Value.TotalDeps.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NonExistentProfile_ReturnsError()
    {
        var repo = new InMemoryServiceDependencyProfileRepository();
        var handler = new GetDependencyHealthDashboard.Handler(repo);

        var result = await handler.Handle(
            new GetDependencyHealthDashboard.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_LicenseRiskCounts_AreGroupedCorrectly()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var deps = new[]
        {
            PackageDependency.Create(profile.Id.Value, "a", "1.0", PackageEcosystem.NuGet, true, "MIT", LicenseRiskLevel.Low),
            PackageDependency.Create(profile.Id.Value, "b", "1.0", PackageEcosystem.NuGet, true, "GPL-3.0", LicenseRiskLevel.High),
            PackageDependency.Create(profile.Id.Value, "c", "1.0", PackageEcosystem.NuGet, true, "MIT", LicenseRiskLevel.Low)
        };
        profile.UpdateScan(deps, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new GetDependencyHealthDashboard.Handler(repo);

        var result = await handler.Handle(
            new GetDependencyHealthDashboard.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LicenseRiskCounts.Should().ContainKey("Low");
        result.Value.LicenseRiskCounts["Low"].Should().Be(2);
        result.Value.LicenseRiskCounts.Should().ContainKey("High");
        result.Value.LicenseRiskCounts["High"].Should().Be(1);
    }
}
