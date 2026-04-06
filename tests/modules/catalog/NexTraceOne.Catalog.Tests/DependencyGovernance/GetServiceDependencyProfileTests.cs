using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetServiceDependencyProfile;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class GetServiceDependencyProfileTests
{
    private static ServiceDependencyProfile CreateProfile(Guid serviceId)
    {
        var profile = ServiceDependencyProfile.Create(serviceId);
        var deps = new[]
        {
            PackageDependency.Create(profile.Id.Value, "Newtonsoft.Json", "13.0.0", PackageEcosystem.NuGet, true, "MIT")
        };
        profile.UpdateScan(deps, null, SbomFormat.CycloneDx);
        return profile;
    }

    [Fact]
    public async Task Handle_ExistingProfile_ReturnsResponse()
    {
        var serviceId = Guid.NewGuid();
        var profile = CreateProfile(serviceId);
        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new GetServiceDependencyProfile.Handler(repo);

        var result = await handler.Handle(
            new GetServiceDependencyProfile.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be(serviceId);
        result.Value.TotalDependencies.Should().Be(1);
        result.Value.Dependencies.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NonExistentProfile_ReturnsError()
    {
        var repo = new InMemoryServiceDependencyProfileRepository();
        var handler = new GetServiceDependencyProfile.Handler(repo);

        var result = await handler.Handle(
            new GetServiceDependencyProfile.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DependencyGovernance.ProfileNotFound");
    }

    [Fact]
    public async Task Handle_ProfileWithSbom_ReturnsSbomFlag()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        profile.UpdateScan(Array.Empty<PackageDependency>(), "sbom-content", SbomFormat.CycloneDx);
        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new GetServiceDependencyProfile.Handler(repo);

        var result = await handler.Handle(
            new GetServiceDependencyProfile.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasSbom.Should().BeTrue();
    }
}
