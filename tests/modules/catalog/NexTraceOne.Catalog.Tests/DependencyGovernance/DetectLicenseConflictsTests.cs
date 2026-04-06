using NexTraceOne.Catalog.Application.DependencyGovernance.Features.DetectLicenseConflicts;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class DetectLicenseConflictsTests
{
    [Fact]
    public async Task Handle_GplAndMit_DetectsConflict()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var deps = new[]
        {
            PackageDependency.Create(profile.Id.Value, "gpl-lib", "1.0", PackageEcosystem.NuGet, true, "GPL-3.0"),
            PackageDependency.Create(profile.Id.Value, "mit-lib", "2.0", PackageEcosystem.NuGet, true, "MIT")
        };
        profile.UpdateScan(deps, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new DetectLicenseConflicts.Handler(repo);

        var result = await handler.Handle(
            new DetectLicenseConflicts.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].LicenseA.Should().Be("GPL-3.0");
        result.Value[0].LicenseB.Should().Be("MIT");
    }

    [Fact]
    public async Task Handle_AllMitLicenses_NoConflict()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var deps = new[]
        {
            PackageDependency.Create(profile.Id.Value, "a", "1.0", PackageEcosystem.NuGet, true, "MIT"),
            PackageDependency.Create(profile.Id.Value, "b", "1.0", PackageEcosystem.NuGet, true, "Apache-2.0")
        };
        profile.UpdateScan(deps, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new DetectLicenseConflicts.Handler(repo);

        var result = await handler.Handle(
            new DetectLicenseConflicts.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoLicenses_NoConflict()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "unlicensed", "1.0", PackageEcosystem.NuGet, true);
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new DetectLicenseConflicts.Handler(repo);

        var result = await handler.Handle(
            new DetectLicenseConflicts.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
