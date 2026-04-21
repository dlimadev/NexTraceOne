using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Features.GetServiceOwnershipHealthReport;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave L.1 — Service Ownership Health Report.
/// Cobre score de saúde de ownership, classificação de banda de saúde e detecção de problemas.
/// </summary>
public sealed class ServiceOwnershipHealthReportTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ServiceAsset MakeService(
        string name,
        string teamName = "team-alpha",
        string techOwner = "dev@example.com",
        string bizOwner = "pm@example.com",
        ServiceTierType tier = ServiceTierType.Standard,
        string? docUrl = "https://docs.example.com/api",
        DateTimeOffset? lastReviewAt = null)
    {
        var svc = ServiceAsset.Create(name, "finance", teamName);
        svc.SetTier(tier);
        svc.UpdateOwnership(svc.TeamName, techOwner ?? string.Empty, bizOwner ?? string.Empty);
        svc.UpdateDetails(
            displayName: name,
            description: "Test service",
            serviceType: NexTraceOne.Catalog.Domain.Graph.Enums.ServiceType.RestApi,
            systemArea: "test",
            criticality: NexTraceOne.Catalog.Domain.Graph.Enums.Criticality.Medium,
            lifecycleStatus: NexTraceOne.Catalog.Domain.Graph.Enums.LifecycleStatus.Active,
            exposureType: NexTraceOne.Catalog.Domain.Graph.Enums.ExposureType.Internal,
            documentationUrl: docUrl ?? string.Empty,
            repositoryUrl: "https://github.com/example/repo");
        if (lastReviewAt.HasValue)
            svc.RecordOwnershipReview(lastReviewAt.Value);
        return svc;
    }

    private static GetServiceOwnershipHealthReport.Handler CreateHandler(
        IReadOnlyList<ServiceAsset> services)
    {
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(services);
        return new GetServiceOwnershipHealthReport.Handler(repo);
    }

    // ── Handler tests ────────────────────────────────────────────────────

    [Fact]
    public async Task GetServiceOwnershipHealthReport_Empty_When_No_Services()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
        result.Value.Services.Should().BeEmpty();
        result.Value.CatalogHealthScore.Should().Be(100m);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_FullScore_When_Service_Is_Complete()
    {
        var svc = MakeService("perfect-svc", lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var handler = CreateHandler([svc]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().HaveCount(1);
        result.Value.Services[0].HealthScore.Should().Be(100);
        result.Value.Services[0].Issues.Should().BeEmpty();
        result.Value.HealthBand.Should().Be(GetServiceOwnershipHealthReport.OwnershipHealthBand.Healthy);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_Missing_Team_Lowers_Score_By_35()
    {
        var svc = MakeService("no-team-svc", teamName: "unassigned",
            lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var handler = CreateHandler([svc]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].HealthScore.Should().Be(65);
        result.Value.Services[0].Issues.Should().Contain(GetServiceOwnershipHealthReport.OwnershipIssue.MissingTeam);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_Missing_TechOwner_Lowers_Score_By_25()
    {
        var svc = MakeService("no-tech-svc", techOwner: "",
            lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var handler = CreateHandler([svc]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].HealthScore.Should().Be(75);
        result.Value.Services[0].Issues.Should().Contain(GetServiceOwnershipHealthReport.OwnershipIssue.MissingTechnicalOwner);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_Stale_Review_Lowers_Score()
    {
        var svc = MakeService("stale-svc", lastReviewAt: DateTimeOffset.UtcNow.AddDays(-200));
        var handler = CreateHandler([svc]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId, OwnershipReviewStalenessThresholdDays: 180),
            CancellationToken.None);

        result.Value.Services[0].Issues.Should().Contain(GetServiceOwnershipHealthReport.OwnershipIssue.StaleReview);
        result.Value.Services[0].HealthScore.Should().BeLessThan(100);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_No_Review_Is_Stale()
    {
        var svc = MakeService("never-reviewed-svc");
        var handler = CreateHandler([svc]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].Issues.Should().Contain(GetServiceOwnershipHealthReport.OwnershipIssue.StaleReview);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_Missing_Doc_Lowers_Score_By_10()
    {
        var svc = MakeService("no-doc-svc", docUrl: null,
            lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var handler = CreateHandler([svc]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].HealthScore.Should().Be(90);
        result.Value.Services[0].Issues.Should().Contain(GetServiceOwnershipHealthReport.OwnershipIssue.MissingDocumentation);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_Worst_Case_Score_Is_Zero()
    {
        var svc = MakeService("broken-svc",
            teamName: "unassigned",
            techOwner: "",
            bizOwner: "",
            docUrl: null);
        var handler = CreateHandler([svc]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].HealthScore.Should().Be(0);
        result.Value.Services[0].Issues.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_HealthBand_Critical_When_Score_Below_50()
    {
        var svc = MakeService("broken-svc", teamName: "unassigned", techOwner: "", bizOwner: "", docUrl: null);
        var handler = CreateHandler([svc]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.HealthBand.Should().Be(GetServiceOwnershipHealthReport.OwnershipHealthBand.Critical);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_HealthBand_Healthy_When_Score_At_Least_90()
    {
        var svc = MakeService("good-svc", lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var handler = CreateHandler([svc]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.HealthBand.Should().Be(GetServiceOwnershipHealthReport.OwnershipHealthBand.Healthy);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_FiltersByTier()
    {
        var critical = MakeService("crit-svc", lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        critical.SetTier(ServiceTierType.Critical);
        var standard = MakeService("std-svc", lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var handler = CreateHandler([critical, standard]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId, TierFilter: ServiceTierType.Critical),
            CancellationToken.None);

        result.Value.Services.Should().HaveCount(1);
        result.Value.Services[0].ServiceName.Should().Be("crit-svc");
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_IssueBreakdown_Counted_Correctly()
    {
        var good = MakeService("good-svc", lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var noTeam = MakeService("no-team", teamName: "unknown", lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var noDoc = MakeService("no-doc", docUrl: null, lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var handler = CreateHandler([good, noTeam, noDoc]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.IssueBreakdown.MissingTeam.Should().Be(1);
        result.Value.IssueBreakdown.MissingDocumentation.Should().Be(1);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_RespectMaxServices()
    {
        var services = Enumerable.Range(1, 10)
            .Select(i => MakeService($"svc-{i}", lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10)))
            .ToList();
        var handler = CreateHandler(services);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId, MaxServices: 5), CancellationToken.None);

        result.Value.Services.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_HealthScoreThreshold_Filters_HighScore_Services()
    {
        var good = MakeService("good-svc", lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var bad = MakeService("bad-svc", teamName: "unassigned", techOwner: "", docUrl: null);
        var handler = CreateHandler([good, bad]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId, HealthScoreThreshold: 50), CancellationToken.None);

        result.Value.Services.Should().NotContain(s => s.ServiceName == "good-svc");
        result.Value.Services.Should().Contain(s => s.ServiceName == "bad-svc");
    }

    [Fact]
    public async Task GetServiceOwnershipHealthReport_Services_Ordered_ByScore_Ascending()
    {
        var good = MakeService("good-svc", lastReviewAt: DateTimeOffset.UtcNow.AddDays(-10));
        var bad = MakeService("bad-svc", teamName: "unassigned");
        var handler = CreateHandler([good, bad]);

        var result = await handler.Handle(
            new GetServiceOwnershipHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].ServiceName.Should().Be("bad-svc");
        result.Value.Services[1].ServiceName.Should().Be("good-svc");
    }

    // ── Validator tests ──────────────────────────────────────────────────

    [Fact]
    public void GetServiceOwnershipHealthReport_Validator_Rejects_MaxServices_Zero()
    {
        var validator = new GetServiceOwnershipHealthReport.Validator();
        var result = validator.Validate(new GetServiceOwnershipHealthReport.Query(TenantId, MaxServices: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetServiceOwnershipHealthReport_Validator_Rejects_MaxServices_Over_500()
    {
        var validator = new GetServiceOwnershipHealthReport.Validator();
        var result = validator.Validate(new GetServiceOwnershipHealthReport.Query(TenantId, MaxServices: 501));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetServiceOwnershipHealthReport_Validator_Rejects_StalenessThreshold_Below_7()
    {
        var validator = new GetServiceOwnershipHealthReport.Validator();
        var result = validator.Validate(
            new GetServiceOwnershipHealthReport.Query(TenantId, OwnershipReviewStalenessThresholdDays: 5));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetServiceOwnershipHealthReport_Validator_Accepts_Valid_Query()
    {
        var validator = new GetServiceOwnershipHealthReport.Validator();
        var result = validator.Validate(
            new GetServiceOwnershipHealthReport.Query(TenantId, 100, 180, ServiceTierType.Critical, 70));
        result.IsValid.Should().BeTrue();
    }
}
