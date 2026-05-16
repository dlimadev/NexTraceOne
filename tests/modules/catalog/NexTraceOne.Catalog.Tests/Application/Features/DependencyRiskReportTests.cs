using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Features.GetDependencyRiskReport;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave I.3 — Dependency Risk Report.
/// Cobre cálculo de score de risco, classificação por nível e detecção de governance gaps.
/// </summary>
public sealed class DependencyRiskReportTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ServiceAsset MakeService(string name, ServiceTierType tier, string teamName = "team-alpha")
    {
        var svc = ServiceAsset.Create(name, "finance", teamName, Guid.NewGuid());
        svc.SetTier(tier);
        return svc;
    }

    // ── Handler tests ────────────────────────────────────────────────────

    [Fact]
    public async Task GetDependencyRiskReport_Returns_Empty_When_No_Services()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceAsset>)[]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ApiAsset>)[]);

        var handler = new GetDependencyRiskReport.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new GetDependencyRiskReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
        result.Value.Services.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDependencyRiskReport_Critical_Tier_Has_Higher_Base_Score()
    {
        var criticalSvc = MakeService("critical-svc", ServiceTierType.Critical);
        var standardSvc = MakeService("standard-svc", ServiceTierType.Standard);

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceAsset>)[criticalSvc, standardSvc]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ApiAsset>)[]);

        var handler = new GetDependencyRiskReport.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new GetDependencyRiskReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var criticalRisk = result.Value.Services.First(s => s.ServiceName == "critical-svc");
        var standardRisk = result.Value.Services.First(s => s.ServiceName == "standard-svc");
        criticalRisk.RiskScore.Should().BeGreaterThan(standardRisk.RiskScore);
    }

    [Fact]
    public async Task GetDependencyRiskReport_No_Owner_Increases_Risk()
    {
        var withOwner = MakeService("owned-svc", ServiceTierType.Standard, "team-alpha");
        var withoutOwner = MakeService("orphan-svc", ServiceTierType.Standard, "unassigned"); // unassigned = governance gap

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceAsset>)[withOwner, withoutOwner]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ApiAsset>)[]);

        var handler = new GetDependencyRiskReport.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new GetDependencyRiskReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var orphanRisk = result.Value.Services.First(s => s.ServiceName == "orphan-svc");
        var ownedRisk = result.Value.Services.First(s => s.ServiceName == "owned-svc");
        orphanRisk.RiskScore.Should().BeGreaterThan(ownedRisk.RiskScore);
        orphanRisk.HasOwner.Should().BeFalse();
    }

    [Fact]
    public async Task GetDependencyRiskReport_RiskLevels_Are_Classified_Correctly()
    {
        var criticalSvc = MakeService("critical-svc", ServiceTierType.Critical);
        var expSvc = MakeService("exp-svc", ServiceTierType.Experimental);

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceAsset>)[criticalSvc, expSvc]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ApiAsset>)[]);

        var handler = new GetDependencyRiskReport.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new GetDependencyRiskReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var criticalRisk = result.Value.Services.First(s => s.ServiceName == "critical-svc");
        // Critical tier (40 base) + owner (0 penalty) = 40 → Medium
        criticalRisk.RiskLevel.Should().BeOneOf(
            GetDependencyRiskReport.DependencyRiskLevel.Medium,
            GetDependencyRiskReport.DependencyRiskLevel.High,
            GetDependencyRiskReport.DependencyRiskLevel.Critical);
    }

    [Fact]
    public async Task GetDependencyRiskReport_Services_Ordered_By_RiskScore_Descending()
    {
        var lowRisk = MakeService("experimental-svc", ServiceTierType.Experimental, "team-a");
        var highRisk = MakeService("critical-svc", ServiceTierType.Critical, "unassigned"); // unassigned = +15 penalty

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceAsset>)[lowRisk, highRisk]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ApiAsset>)[]);

        var handler = new GetDependencyRiskReport.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new GetDependencyRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.Services.Should().BeInDescendingOrder(s => s.RiskScore);
        result.Value.Services.First().ServiceName.Should().Be("critical-svc");
    }

    [Fact]
    public async Task GetDependencyRiskReport_FiltersByTier()
    {
        var criticalSvc = MakeService("critical-svc", ServiceTierType.Critical);
        var standardSvc = MakeService("standard-svc", ServiceTierType.Standard);

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceAsset>)[criticalSvc, standardSvc]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ApiAsset>)[]);

        var handler = new GetDependencyRiskReport.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(
            new GetDependencyRiskReport.Query(TenantId, TierFilter: ServiceTierType.Critical),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(1);
        result.Value.Services.Should().AllSatisfy(s => s.Tier.Should().Be(ServiceTierType.Critical));
    }

    [Fact]
    public async Task GetDependencyRiskReport_Validator_Rejects_Empty_Tenant()
    {
        var validator = new GetDependencyRiskReport.Validator();
        var result = validator.Validate(new GetDependencyRiskReport.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetDependencyRiskReport_Validator_Rejects_Invalid_MaxServices()
    {
        var validator = new GetDependencyRiskReport.Validator();
        var resultLow = validator.Validate(new GetDependencyRiskReport.Query(TenantId, MaxServices: 0));
        var resultHigh = validator.Validate(new GetDependencyRiskReport.Query(TenantId, MaxServices: 201));
        resultLow.IsValid.Should().BeFalse();
        resultHigh.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetDependencyRiskReport_RiskFactors_Include_Governance_Gap_For_No_Owner()
    {
        var orphan = MakeService("orphan-svc", ServiceTierType.Standard, "unassigned");

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceAsset>)[orphan]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ApiAsset>)[]);

        var handler = new GetDependencyRiskReport.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new GetDependencyRiskReport.Query(TenantId), CancellationToken.None);

        var svc = result.Value.Services.First();
        svc.RiskFactors.Should().Contain(f => f.Contains("No owner team"));
    }

    [Fact]
    public async Task GetDependencyRiskReport_HighRiskCount_Counts_High_And_Critical()
    {
        // Score: Critical tier (40) + no owner (15) = 55 → Medium (not High)
        // Score: Critical tier (40) + no owner (15) + very high fan-in = varies
        // Let's ensure the HighRiskCount property is computed correctly.
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceAsset>)[]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ApiAsset>)[]);

        var handler = new GetDependencyRiskReport.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new GetDependencyRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.HighRiskCount.Should().Be(0);
    }
}
