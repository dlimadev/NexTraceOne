using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GenerateSbom;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class GenerateSbomTests
{
    [Fact]
    public async Task Handle_CycloneDxFormat_GeneratesValidSbom()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "Newtonsoft.Json", "13.0.0", PackageEcosystem.NuGet, true);
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new GenerateSbom.Handler(repo, new InMemoryUnitOfWork());

        var result = await handler.Handle(
            new GenerateSbom.Command(serviceId, SbomFormat.CycloneDx), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Format.Should().Be(SbomFormat.CycloneDx);
        result.Value.SbomContent.Should().Contain("CycloneDX");
        result.Value.SbomContent.Should().Contain("Newtonsoft.Json");
    }

    [Fact]
    public async Task Handle_SpdxFormat_GeneratesValidSbom()
    {
        var serviceId = Guid.NewGuid();
        var profile = ServiceDependencyProfile.Create(serviceId);
        var dep = PackageDependency.Create(profile.Id.Value, "react", "18.0.0", PackageEcosystem.Npm, true);
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile });
        var handler = new GenerateSbom.Handler(repo, new InMemoryUnitOfWork());

        var result = await handler.Handle(
            new GenerateSbom.Command(serviceId, SbomFormat.Spdx), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Format.Should().Be(SbomFormat.Spdx);
        result.Value.SbomContent.Should().Contain("SPDX-2.3");
        result.Value.SbomContent.Should().Contain("react");
    }

    [Fact]
    public async Task Handle_NonExistentProfile_ReturnsError()
    {
        var repo = new InMemoryServiceDependencyProfileRepository();
        var handler = new GenerateSbom.Handler(repo, new InMemoryUnitOfWork());

        var result = await handler.Handle(
            new GenerateSbom.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
