using NexTraceOne.Catalog.Application.DependencyGovernance.Features.CheckDependencyPolicies;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class CheckDependencyPoliciesTests
{
    [Fact]
    public async Task Handle_CriticalVuln_ReturnsViolation()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "vuln-pkg", "1.0.0", PackageEcosystem.NuGet, true);
        dep.AddVulnerability(new PackageVulnerability(
            "CVE-001", VulnerabilitySeverity.Critical, 9.8m, "RCE", "<2.0", "2.0",
            DateTimeOffset.UtcNow, "NVD", ExploitMaturity.Active));
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new CheckDependencyPolicies.Handler(repo);

        var result = await handler.Handle(
            new CheckDependencyPolicies.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(v => v.PolicyId == "POLICY-001");
    }

    [Fact]
    public async Task Handle_CriticalLicense_ReturnsViolation()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "risky-pkg", "1.0.0",
            PackageEcosystem.NuGet, true, "AGPL-3.0", LicenseRiskLevel.Critical);
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new CheckDependencyPolicies.Handler(repo);

        var result = await handler.Handle(
            new CheckDependencyPolicies.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(v => v.PolicyId == "POLICY-002");
    }

    [Fact]
    public async Task Handle_NoLicense_ReturnsViolation()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "no-license-pkg", "1.0.0",
            PackageEcosystem.NuGet, true, license: null);
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new CheckDependencyPolicies.Handler(repo);

        var result = await handler.Handle(
            new CheckDependencyPolicies.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(v => v.PolicyId == "POLICY-003");
    }

    [Fact]
    public async Task Handle_CleanProfile_ReturnsNoViolations()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "safe-pkg", "2.0.0",
            PackageEcosystem.NuGet, true, "MIT", LicenseRiskLevel.Low);
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new CheckDependencyPolicies.Handler(repo);

        var result = await handler.Handle(
            new CheckDependencyPolicies.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
