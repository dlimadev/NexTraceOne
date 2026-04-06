using NexTraceOne.Catalog.Application.DependencyGovernance.Features.SuggestDependencyUpgrades;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class SuggestDependencyUpgradesTests
{
    [Fact]
    public async Task Handle_OutdatedDep_ReturnsSuggestion()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "old-pkg", "1.0.0", PackageEcosystem.NuGet, true);
        dep.MarkAsOutdated("3.0.0");
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new SuggestDependencyUpgrades.Handler(repo);

        var result = await handler.Handle(
            new SuggestDependencyUpgrades.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(s => s.PackageName == "old-pkg");
        result.Value[0].SuggestedVersion.Should().Be("3.0.0");
    }

    [Fact]
    public async Task Handle_VulnerableWithFix_ReturnsSuggestion()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "vuln-pkg", "1.0.0", PackageEcosystem.NuGet, true);
        dep.AddVulnerability(new PackageVulnerability(
            "CVE-001", VulnerabilitySeverity.High, 8.0m, "Vuln", "<2.0", "2.0",
            DateTimeOffset.UtcNow, "NVD", ExploitMaturity.Functional));
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new SuggestDependencyUpgrades.Handler(repo);

        var result = await handler.Handle(
            new SuggestDependencyUpgrades.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(s => s.PackageName == "vuln-pkg");
        result.Value[0].SuggestedVersion.Should().Be("2.0");
    }

    [Fact]
    public async Task Handle_UpToDateDep_NotIncluded()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "good-pkg", "3.0.0", PackageEcosystem.NuGet, true, "MIT");
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new SuggestDependencyUpgrades.Handler(repo);

        var result = await handler.Handle(
            new SuggestDependencyUpgrades.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
