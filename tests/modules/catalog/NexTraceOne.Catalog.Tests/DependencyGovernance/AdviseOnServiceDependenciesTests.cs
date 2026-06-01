using Microsoft.Extensions.Logging;

using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.AdviseOnServiceDependencies;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public sealed class AdviseOnServiceDependenciesTests
{
    private readonly IServiceDependencyProfileRepository _repository;
    private readonly ILlmCompletionClient _llmClient;

    public AdviseOnServiceDependenciesTests()
    {
        _repository = new InMemoryServiceDependencyProfileRepository();
        _llmClient = Substitute.For<ILlmCompletionClient>();
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsError()
    {
        var handler = new AdviseOnServiceDependencies.Handler(_repository, _llmClient, Substitute.For<ILogger<AdviseOnServiceDependencies.Handler>>());
        var result = await handler.Handle(new AdviseOnServiceDependencies.Command(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DependencyGovernance.ProfileNotFound");
    }

    [Fact]
    public async Task Handle_LlmUnavailable_ReturnsRuleBasedAdvice()
    {
        var profile = CreateProfileWithVulnerabilities();
        await _repository.AddAsync(profile, default);

        _llmClient.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var handler = new AdviseOnServiceDependencies.Handler(_repository, _llmClient, Substitute.For<ILogger<AdviseOnServiceDependencies.Handler>>());
        var result = await handler.Handle(new AdviseOnServiceDependencies.Command(profile.ServiceId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExecutiveSummary.Should().Contain("moderate");
        result.Value.UpgradePath.Should().HaveCount(2);
        result.Value.UpgradePath[0].Priority.Should().Be("Critical");
        result.Value.RawLlmResponse.Should().BeNull();
    }

    [Fact]
    public async Task Handle_LlmAvailable_ReturnsParsedAdvice()
    {
        var profile = CreateProfileWithVulnerabilities();
        await _repository.AddAsync(profile, default);

        var llmJson = """
            {
                "executiveSummary": "Test summary from LLM.",
                "upgradePath": [
                    {
                        "packageName": "VulnPackage",
                        "currentVersion": "1.0.0",
                        "targetVersion": "2.0.0",
                        "priority": "High",
                        "rationale": "Fixes critical CVE"
                    }
                ]
            }
            """;
        _llmClient.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(llmJson);

        var handler = new AdviseOnServiceDependencies.Handler(_repository, _llmClient, Substitute.For<ILogger<AdviseOnServiceDependencies.Handler>>());
        var result = await handler.Handle(new AdviseOnServiceDependencies.Command(profile.ServiceId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExecutiveSummary.Should().Be("Test summary from LLM.");
        result.Value.UpgradePath.Should().HaveCount(1);
        result.Value.UpgradePath[0].PackageName.Should().Be("VulnPackage");
        result.Value.RawLlmResponse.Should().Be(llmJson);
    }

    [Fact]
    public async Task Handle_HealthyProfile_ReturnsPositiveSummary()
    {
        var profile = ServiceDependencyProfile.Create(Guid.NewGuid());
        var dep = PackageDependency.Create(profile.Id.Value, "SafePackage", "1.0.0", PackageEcosystem.NuGet, true);
        profile.UpdateScan(new[] { dep }, null, SbomFormat.CycloneDx);
        await _repository.AddAsync(profile, default);

        _llmClient.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var handler = new AdviseOnServiceDependencies.Handler(_repository, _llmClient, Substitute.For<ILogger<AdviseOnServiceDependencies.Handler>>());
        var result = await handler.Handle(new AdviseOnServiceDependencies.Command(profile.ServiceId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExecutiveSummary.Should().Contain("good");
        result.Value.UpgradePath.Should().BeEmpty();
    }

    private static ServiceDependencyProfile CreateProfileWithVulnerabilities()
    {
        var profile = ServiceDependencyProfile.Create(Guid.NewGuid());
        var dep1 = PackageDependency.Create(profile.Id.Value, "VulnPackage", "1.0.0", PackageEcosystem.NuGet, true);
        dep1.AddVulnerability(new PackageVulnerability(
            CveId: "CVE-2024-TEST", Severity: VulnerabilitySeverity.Critical,
            CvssScore: 9.8m, Description: "Test", AffectedVersionRange: "*",
            FixedInVersion: "2.0.0", PublishedAt: DateTimeOffset.UtcNow,
            Source: "OSV", ExploitMaturity: ExploitMaturity.NotDefined));

        var dep2 = PackageDependency.Create(profile.Id.Value, "OldPackage", "1.0.0", PackageEcosystem.NuGet, true);
        dep2.UpdateLatestVersion("2.0.0");

        profile.UpdateScan(new[] { dep1, dep2 }, null, SbomFormat.CycloneDx);
        return profile;
    }
}
