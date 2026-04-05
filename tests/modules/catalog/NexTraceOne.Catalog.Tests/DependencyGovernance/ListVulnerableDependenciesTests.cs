using NexTraceOne.Catalog.Application.DependencyGovernance.Features.ListVulnerableDependencies;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class ListVulnerableDependenciesTests
{
    private static ServiceDependencyProfile CreateProfileWithVuln(Guid serviceId, VulnerabilitySeverity severity)
    {
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "vuln-pkg", "1.0.0", PackageEcosystem.NuGet, true);
        dep.AddVulnerability(new PackageVulnerability(
            "CVE-2024-001", severity, 8.0m, "Test vuln", "<2.0", "2.0",
            DateTimeOffset.UtcNow, "NVD", ExploitMaturity.Functional));
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);
        return profile;
    }

    [Fact]
    public async Task Handle_ProfileWithHighVuln_IsIncluded()
    {
        var serviceId = Guid.NewGuid();
        var profile = CreateProfileWithVuln(serviceId, VulnerabilitySeverity.High);
        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new ListVulnerableDependencies.Handler(repo);

        var result = await handler.Handle(
            new ListVulnerableDependencies.Query(VulnerabilitySeverity.High), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].ServiceId.Should().Be(serviceId);
    }

    [Fact]
    public async Task Handle_ProfileWithLowVuln_ExcludedWhenMinIsHigh()
    {
        var serviceId = Guid.NewGuid();
        var profile = CreateProfileWithVuln(serviceId, VulnerabilitySeverity.Low);
        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new ListVulnerableDependencies.Handler(repo);

        var result = await handler.Handle(
            new ListVulnerableDependencies.Query(VulnerabilitySeverity.High), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new InMemoryServiceDependencyProfileRepository();
        var handler = new ListVulnerableDependencies.Handler(repo);

        var result = await handler.Handle(
            new ListVulnerableDependencies.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
