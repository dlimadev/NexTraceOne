using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using GetMaturityV2Feature = NexTraceOne.Catalog.Application.Graph.Features.GetServiceMaturityScoreV2.GetServiceMaturityScoreV2;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave H.3 — Service Maturity Score v2.
/// Cobre scorecard dimensional com pesos por tier e postura de vulnerabilidade.
/// </summary>
public sealed class ServiceMaturityV2Tests
{
    private static readonly Guid ServiceId = Guid.NewGuid();

    private static ServiceAsset CreateService(
        string name = "svc-x",
        string team = "platform",
        string techOwner = "alice",
        string bizOwner = "bob",
        ServiceTierType tier = ServiceTierType.Standard,
        string? docUrl = null)
    {
        var svc = ServiceAsset.Create(name, "platform", team);
        svc.UpdateOwnership(team, techOwner, bizOwner);
        svc.SetTier(tier);
        if (docUrl is not null)
            svc.UpdateDetails(name, "desc", ServiceType.RestApi, "area", Criticality.Medium,
                LifecycleStatus.Active, ExposureType.Internal, docUrl, "https://repo");
        return svc;
    }

    private static (IServiceAssetRepository, IServiceLinkRepository, IApiAssetRepository,
        IContractVersionRepository, IVulnerabilityAdvisoryRepository) SetupMocks(
            ServiceAsset service,
            IReadOnlyList<ServiceLink>? links = null,
            IReadOnlyList<ApiAsset>? apis = null,
            IReadOnlyList<ContractVersion>? contracts = null,
            int criticalAdvisories = 0,
            int highAdvisories = 0)
    {
        var svcRepo = Substitute.For<IServiceAssetRepository>();
        svcRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var linkRepo = Substitute.For<IServiceLinkRepository>();
        linkRepo.ListByServiceAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(links ?? (IReadOnlyList<ServiceLink>)[]);

        var apiRepo = Substitute.For<IApiAssetRepository>();
        apiRepo.ListByServiceIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(apis ?? (IReadOnlyList<ApiAsset>)[]);

        var contractRepo = Substitute.For<IContractVersionRepository>();
        contractRepo.ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(contracts ?? (IReadOnlyList<ContractVersion>)[]);

        var vulnRepo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        vulnRepo.CountByServiceAndSeverityAsync(
                Arg.Any<Guid>(), VulnerabilitySeverity.Critical, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(criticalAdvisories);
        vulnRepo.CountByServiceAndSeverityAsync(
                Arg.Any<Guid>(), VulnerabilitySeverity.High, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(highAdvisories);

        return (svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo);
    }

    // ── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MaturityV2_Returns_NotFound_For_Missing_Service()
    {
        var svcRepo = Substitute.For<IServiceAssetRepository>();
        svcRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);
        var linkRepo = Substitute.For<IServiceLinkRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var vulnRepo = Substitute.For<IVulnerabilityAdvisoryRepository>();

        var handler = new GetMaturityV2Feature.Handler(svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo);
        var result = await handler.Handle(new GetMaturityV2Feature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task MaturityV2_Returns_All_Six_Dimensions()
    {
        var svc = CreateService();
        var (svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo) = SetupMocks(svc);

        var handler = new GetMaturityV2Feature.Handler(svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo);
        var result = await handler.Handle(new GetMaturityV2Feature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Dimensions.Should().HaveCount(6);
        result.Value.Dimensions.Select(d => d.DimensionKey)
            .Should().BeEquivalentTo(
            ["ownership", "contracts", "documentation", "operational_readiness", "tier_compliance", "vulnerability_posture"]);
    }

    [Fact]
    public async Task MaturityV2_VulnerabilityPosture_Zero_When_Critical_Advisory()
    {
        var svc = CreateService();
        var (svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo) = SetupMocks(svc, criticalAdvisories: 2);

        var handler = new GetMaturityV2Feature.Handler(svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo);
        var result = await handler.Handle(new GetMaturityV2Feature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var vulnDim = result.Value.Dimensions.Single(d => d.DimensionKey == "vulnerability_posture");
        vulnDim.Score.Should().Be(0m);
    }

    [Fact]
    public async Task MaturityV2_VulnerabilityPosture_Perfect_When_No_Advisories()
    {
        var svc = CreateService();
        var (svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo) = SetupMocks(svc);

        var handler = new GetMaturityV2Feature.Handler(svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo);
        var result = await handler.Handle(new GetMaturityV2Feature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var vulnDim = result.Value.Dimensions.Single(d => d.DimensionKey == "vulnerability_posture");
        vulnDim.Score.Should().Be(1m);
    }

    [Fact]
    public async Task MaturityV2_Ownership_FullScore_When_All_Owners_Present()
    {
        var svc = CreateService(team: "payments", techOwner: "alice", bizOwner: "bob");
        var (svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo) = SetupMocks(svc);

        var handler = new GetMaturityV2Feature.Handler(svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo);
        var result = await handler.Handle(new GetMaturityV2Feature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var ownerDim = result.Value.Dimensions.Single(d => d.DimensionKey == "ownership");
        ownerDim.Score.Should().Be(1m);
    }

    [Fact]
    public async Task MaturityV2_Level_Nascente_When_Score_Below_40()
    {
        // No owners, no contracts, no docs, no ops, critical advisories
        var svc = ServiceAsset.Create("ghost-svc", "platform", "lost-team");
        svc.SetTier(ServiceTierType.Critical);
        var (svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo) = SetupMocks(svc, criticalAdvisories: 5);

        var handler = new GetMaturityV2Feature.Handler(svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo);
        var result = await handler.Handle(new GetMaturityV2Feature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Level.Should().Be("Nascente");
    }

    [Fact]
    public async Task MaturityV2_Tier_Is_Reflected_In_Response()
    {
        var svc = CreateService(tier: ServiceTierType.Critical);
        var (svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo) = SetupMocks(svc);

        var handler = new GetMaturityV2Feature.Handler(svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo);
        var result = await handler.Handle(new GetMaturityV2Feature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tier.Should().Be("Critical");
    }

    [Fact]
    public async Task MaturityV2_Validator_Rejects_Empty_ServiceId()
    {
        var validator = new GetMaturityV2Feature.Validator();
        var result = await validator.ValidateAsync(new GetMaturityV2Feature.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task MaturityV2_Score_Is_Between_0_And_100()
    {
        var svc = CreateService(docUrl: "https://docs.example.com");
        var (svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo) = SetupMocks(svc);

        var handler = new GetMaturityV2Feature.Handler(svcRepo, linkRepo, apiRepo, contractRepo, vulnRepo);
        var result = await handler.Handle(new GetMaturityV2Feature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallScore.Should().BeGreaterThanOrEqualTo(0m);
        result.Value.OverallScore.Should().BeLessThanOrEqualTo(100m);
    }

    [Fact]
    public async Task MaturityV2_Experimental_Tier_Applies_More_Lenient_Weights()
    {
        // Service with only ownership — should score higher under Experimental
        var svcExp = CreateService(tier: ServiceTierType.Experimental);
        var svcCrit = CreateService(tier: ServiceTierType.Critical);

        var (svcRepoExp, linkRepoExp, apiRepoExp, contractRepoExp, vulnRepoExp) = SetupMocks(svcExp, criticalAdvisories: 1);
        var (svcRepoCrit, linkRepoCrit, apiRepoCrit, contractRepoCrit, vulnRepoCrit) = SetupMocks(svcCrit, criticalAdvisories: 1);

        var handlerExp = new GetMaturityV2Feature.Handler(svcRepoExp, linkRepoExp, apiRepoExp, contractRepoExp, vulnRepoExp);
        var handlerCrit = new GetMaturityV2Feature.Handler(svcRepoCrit, linkRepoCrit, apiRepoCrit, contractRepoCrit, vulnRepoCrit);

        var resultExp = await handlerExp.Handle(new GetMaturityV2Feature.Query(ServiceId), CancellationToken.None);
        var resultCrit = await handlerCrit.Handle(new GetMaturityV2Feature.Query(ServiceId), CancellationToken.None);

        resultExp.IsSuccess.Should().BeTrue();
        resultCrit.IsSuccess.Should().BeTrue();
        // Experimental weights vulnerability less, so score should be higher
        resultExp.Value.OverallScore.Should().BeGreaterThan(resultCrit.Value.OverallScore);
    }
}
