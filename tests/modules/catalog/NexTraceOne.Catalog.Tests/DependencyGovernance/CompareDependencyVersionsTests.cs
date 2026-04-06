using NexTraceOne.Catalog.Application.DependencyGovernance.Features.CompareDependencyVersions;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class CompareDependencyVersionsTests
{
    private static ServiceDependencyProfile CreateProfileWith(Guid serviceId, params (string name, string version)[] deps)
    {
        var profile = ServiceDependencyProfile.Create(serviceId);
        var depList = deps.Select(d => PackageDependency.Create(
            profile.Id.Value, d.name, d.version, PackageEcosystem.NuGet, true)).ToList();
        profile.UpdateScan(depList, null, SbomFormat.CycloneDx);
        return profile;
    }

    [Fact]
    public async Task Handle_DifferentDeps_ClassifiesCorrectly()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var profileA = CreateProfileWith(idA, ("A", "1.0.0"), ("B", "2.0.0"));
        var profileB = CreateProfileWith(idB, ("B", "3.0.0"), ("C", "1.0.0"));

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profileA, profileB });
        var handler = new CompareDependencyVersions.Handler(repo);

        var result = await handler.Handle(
            new CompareDependencyVersions.Query(idA, idB), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OnlyInA.Should().ContainSingle(r => r.PackageName == "A");
        result.Value.OnlyInB.Should().ContainSingle(r => r.PackageName == "C");
        result.Value.InBothDifferentVersions.Should().ContainSingle(r => r.PackageName == "B");
    }

    [Fact]
    public async Task Handle_SameDeps_ReturnsInBothSame()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var profileA = CreateProfileWith(idA, ("Shared", "1.0.0"));
        var profileB = CreateProfileWith(idB, ("Shared", "1.0.0"));

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profileA, profileB });
        var handler = new CompareDependencyVersions.Handler(repo);

        var result = await handler.Handle(
            new CompareDependencyVersions.Query(idA, idB), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InBothSameVersion.Should().ContainSingle(r => r.PackageName == "Shared");
    }

    [Fact]
    public async Task Handle_MissingProfileA_ReturnsError()
    {
        var repo = new InMemoryServiceDependencyProfileRepository();
        var handler = new CompareDependencyVersions.Handler(repo);

        var result = await handler.Handle(
            new CompareDependencyVersions.Query(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
