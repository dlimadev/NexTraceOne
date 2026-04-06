using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetTemplateDependencyHealth;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class GetTemplateDependencyHealthTests
{
    [Fact]
    public async Task Handle_MultipleProfiles_ReturnsAverageScore()
    {
        var templateId = Guid.NewGuid();
        var profile1 = ServiceDependencyProfile.Create(Guid.NewGuid(), templateId);
        var profile2 = ServiceDependencyProfile.Create(Guid.NewGuid(), templateId);

        // Profile1 has a critical vulnerability — score = 75
        var dep1 = PackageDependency.Create(profile1.Id.Value, "vuln-pkg", "1.0", PackageEcosystem.NuGet, true);
        dep1.AddVulnerability(new PackageVulnerability(
            "CVE-001", VulnerabilitySeverity.Critical, 9.8m, "RCE", "<2.0", "2.0",
            DateTimeOffset.UtcNow, "NVD", ExploitMaturity.Active));
        profile1.UpdateScan(new[] { dep1 }, null, SbomFormat.CycloneDx);

        // Profile2 is clean — score = 100
        var dep2 = PackageDependency.Create(profile2.Id.Value, "clean-pkg", "2.0", PackageEcosystem.NuGet, true, "MIT");
        profile2.UpdateScan(new[] { dep2 }, null, SbomFormat.CycloneDx);

        var repo = new InMemoryServiceDependencyProfileRepository(new[] { profile1, profile2 });
        var handler = new GetTemplateDependencyHealth.Handler(repo);

        var result = await handler.Handle(
            new GetTemplateDependencyHealth.Query(templateId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceCount.Should().Be(2);
        result.Value.AverageHealthScore.Should().Be(87); // (75+100)/2 = 87 (int)
    }

    [Fact]
    public async Task Handle_NoProfiles_ReturnsZeroStats()
    {
        var templateId = Guid.NewGuid();
        var repo = new InMemoryServiceDependencyProfileRepository();
        var handler = new GetTemplateDependencyHealth.Handler(repo);

        var result = await handler.Handle(
            new GetTemplateDependencyHealth.Query(templateId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceCount.Should().Be(0);
        result.Value.AverageHealthScore.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CommonVulnerabilities_AreSummarized()
    {
        var templateId = Guid.NewGuid();
        var vuln = new PackageVulnerability(
            "CVE-COMMON", VulnerabilitySeverity.High, 7.5m, "SQL Injection", "<2.0", "2.0",
            DateTimeOffset.UtcNow, "NVD", ExploitMaturity.ProofOfConcept);

        var profiles = Enumerable.Range(0, 3).Select(_ =>
        {
            var p = ServiceDependencyProfile.Create(Guid.NewGuid(), templateId);
            var d = PackageDependency.Create(p.Id.Value, "common-vuln-pkg", "1.0", PackageEcosystem.NuGet, true);
            d.AddVulnerability(vuln);
            p.UpdateScan(new[] { d }, null, SbomFormat.CycloneDx);
            return p;
        }).ToList();

        var repo = new InMemoryServiceDependencyProfileRepository(profiles);
        var handler = new GetTemplateDependencyHealth.Handler(repo);

        var result = await handler.Handle(
            new GetTemplateDependencyHealth.Query(templateId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MostCommonVulnerabilities.Should().ContainSingle(v => v.CveId == "CVE-COMMON");
        result.Value.MostCommonVulnerabilities[0].OccurrenceCount.Should().Be(3);
    }
}
