using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPreProductionComparison;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseImpactReport;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.GetEnvironmentPromotionPath;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para features de Change Intelligence e Promoção sem cobertura prévia:
/// GetPreProductionComparison, GetReleaseImpactReport, GetEnvironmentPromotionPath.
/// Cobre cenários de releases não encontradas, ausência de baselines e dados completos.
/// </summary>
public sealed class ChangeGovernanceExtendedGapTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(FixedNow);
        return c;
    }

    private static Release CreateRelease(string serviceName = "catalog-service", string version = "v1.0.0") =>
        Release.Create(Guid.NewGuid(), Guid.NewGuid(), serviceName, version, "Production",
            "gitlab-ci", "abc123def456", FixedNow);

    // ── GetPreProductionComparison ──────────────────────────────────────────

    [Fact]
    public async Task GetPreProductionComparison_PreProdReleaseNotFound_ReturnsFailure()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var baselineRepo = Substitute.For<IReleaseBaselineRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var handler = new GetPreProductionComparison.Handler(releaseRepo, baselineRepo);
        var result = await handler.Handle(
            new GetPreProductionComparison.Query(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetPreProductionComparison_ProductionReleaseNotFound_ReturnsFailure()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var baselineRepo = Substitute.For<IReleaseBaselineRepository>();

        var preProdId = ReleaseId.New();
        var prodId = ReleaseId.New();
        var preProdRelease = CreateRelease("checkout-service", "v2.0.0-rc1");

        releaseRepo.GetByIdAsync(preProdId, Arg.Any<CancellationToken>())
            .Returns(preProdRelease);
        releaseRepo.GetByIdAsync(prodId, Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var handler = new GetPreProductionComparison.Handler(releaseRepo, baselineRepo);
        var result = await handler.Handle(
            new GetPreProductionComparison.Query(preProdId.Value, prodId.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetPreProductionComparison_NoBothBaselines_ReturnsNoBaselineData()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var baselineRepo = Substitute.For<IReleaseBaselineRepository>();

        var preProdRelease = CreateRelease("order-service", "v3.0.0-beta");
        var prodRelease = CreateRelease("order-service", "v2.9.0");

        var preProdId = preProdRelease.Id;
        var prodId = prodRelease.Id;

        releaseRepo.GetByIdAsync(preProdId, Arg.Any<CancellationToken>())
            .Returns(preProdRelease);
        releaseRepo.GetByIdAsync(prodId, Arg.Any<CancellationToken>())
            .Returns(prodRelease);

        baselineRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ReleaseBaseline?)null);

        var handler = new GetPreProductionComparison.Handler(releaseRepo, baselineRepo);
        var result = await handler.Handle(
            new GetPreProductionComparison.Query(preProdId.Value, prodId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasBaselineData.Should().BeFalse();
    }

    [Fact]
    public async Task GetPreProductionComparison_WithBothBaselines_ReturnsComparison()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var baselineRepo = Substitute.For<IReleaseBaselineRepository>();

        var preProdRelease = CreateRelease("payment-service", "v5.0.0-rc2");
        var prodRelease = CreateRelease("payment-service", "v4.9.1");

        var preProdId = preProdRelease.Id;
        var prodId = prodRelease.Id;

        releaseRepo.GetByIdAsync(preProdId, Arg.Any<CancellationToken>())
            .Returns(preProdRelease);
        releaseRepo.GetByIdAsync(prodId, Arg.Any<CancellationToken>())
            .Returns(prodRelease);

        var preProdBaseline = ReleaseBaseline.Create(
            preProdId, requestsPerMinute: 1000, errorRate: 0.5m,
            avgLatencyMs: 45m, p95LatencyMs: 90m, p99LatencyMs: 150m,
            throughput: 850m,
            collectedFrom: FixedNow.AddHours(-2), collectedTo: FixedNow.AddHours(-1),
            capturedAt: FixedNow);

        var prodBaseline = ReleaseBaseline.Create(
            prodId, requestsPerMinute: 950, errorRate: 0.8m,
            avgLatencyMs: 50m, p95LatencyMs: 95m, p99LatencyMs: 160m,
            throughput: 800m,
            collectedFrom: FixedNow.AddDays(-1), collectedTo: FixedNow.AddDays(-1).AddHours(1),
            capturedAt: FixedNow);

        baselineRepo.GetByReleaseIdAsync(preProdId, Arg.Any<CancellationToken>())
            .Returns(preProdBaseline);
        baselineRepo.GetByReleaseIdAsync(prodId, Arg.Any<CancellationToken>())
            .Returns(prodBaseline);

        var handler = new GetPreProductionComparison.Handler(releaseRepo, baselineRepo);
        var result = await handler.Handle(
            new GetPreProductionComparison.Query(preProdId.Value, prodId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasBaselineData.Should().BeTrue();
        result.Value.ErrorRate.Should().NotBeNull();
        result.Value.OverallSignal.Should().NotBeNullOrWhiteSpace();
    }

    // ── GetReleaseImpactReport ──────────────────────────────────────────────

    [Fact]
    public async Task GetReleaseImpactReport_ReleaseNotFound_ReturnsFailure()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var blastRadiusRepo = Substitute.For<IBlastRadiusRepository>();
        var scoreRepo = Substitute.For<IChangeScoreRepository>();
        var commitRepo = Substitute.For<ICommitAssociationRepository>();
        var workItemRepo = Substitute.For<IWorkItemAssociationRepository>();
        var approvalRepo = Substitute.For<IApprovalRequestRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var handler = new GetReleaseImpactReport.Handler(
            releaseRepo, blastRadiusRepo, scoreRepo, commitRepo, workItemRepo, approvalRepo, CreateClock());

        var result = await handler.Handle(
            new GetReleaseImpactReport.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("RELEASE_NOT_FOUND");
    }

    [Fact]
    public async Task GetReleaseImpactReport_ReleaseFound_ReturnsReport()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var blastRadiusRepo = Substitute.For<IBlastRadiusRepository>();
        var scoreRepo = Substitute.For<IChangeScoreRepository>();
        var commitRepo = Substitute.For<ICommitAssociationRepository>();
        var workItemRepo = Substitute.For<IWorkItemAssociationRepository>();
        var approvalRepo = Substitute.For<IApprovalRequestRepository>();

        var release = CreateRelease("catalog-service", "v2.0.0");
        var releaseId = release.Id;

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        blastRadiusRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((BlastRadiusReport?)null);
        scoreRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ChangeIntelligenceScore?)null);
        commitRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<CommitAssociation>().AsReadOnly());
        workItemRepo.ListActiveByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkItemAssociation>().AsReadOnly());
        approvalRepo.ListPendingByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ReleaseApprovalRequest>().AsReadOnly());

        var handler = new GetReleaseImpactReport.Handler(
            releaseRepo, blastRadiusRepo, scoreRepo, commitRepo, workItemRepo, approvalRepo, CreateClock());

        var result = await handler.Handle(
            new GetReleaseImpactReport.Query(releaseId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("catalog-service");
        result.Value.Version.Should().Be("v2.0.0");
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── GetEnvironmentPromotionPath ─────────────────────────────────────────

    [Fact]
    public async Task GetEnvironmentPromotionPath_NoPromotions_ReturnsEmptyPath()
    {
        var promotionRepo = Substitute.For<IPromotionRequestRepository>();
        var envRepo = Substitute.For<IDeploymentEnvironmentRepository>();

        promotionRepo.ListByReleaseIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionRequest>().AsReadOnly());

        var handler = new GetEnvironmentPromotionPath.Handler(promotionRepo, envRepo);
        var result = await handler.Handle(
            new GetEnvironmentPromotionPath.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Steps.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEnvironmentPromotionPath_WithPromotions_ReturnsPath()
    {
        var promotionRepo = Substitute.For<IPromotionRequestRepository>();
        var envRepo = Substitute.For<IDeploymentEnvironmentRepository>();

        var releaseId = Guid.NewGuid();
        var devEnvId = DeploymentEnvironmentId.New();
        var stagingEnvId = DeploymentEnvironmentId.New();

        var promotion = PromotionRequest.Create(
            releaseId, devEnvId, stagingEnvId, "engineer@example.com", FixedNow);

        var devEnv = DeploymentEnvironment.Create(
            "Development", "Dev environment", 1, false, false, FixedNow);
        var stagingEnv = DeploymentEnvironment.Create(
            "Staging", "Staging environment", 2, true, false, FixedNow);

        promotionRepo.ListByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(new List<PromotionRequest> { promotion }.AsReadOnly());

        envRepo.GetByIdAsync(devEnvId, Arg.Any<CancellationToken>())
            .Returns(devEnv);
        envRepo.GetByIdAsync(stagingEnvId, Arg.Any<CancellationToken>())
            .Returns(stagingEnv);

        var handler = new GetEnvironmentPromotionPath.Handler(promotionRepo, envRepo);
        var result = await handler.Handle(
            new GetEnvironmentPromotionPath.Query(releaseId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Steps.Should().ContainSingle();
        result.Value.Steps[0].SourceEnvironment.Should().Be("Development");
        result.Value.Steps[0].TargetEnvironment.Should().Be("Staging");
    }

    [Fact]
    public async Task GetEnvironmentPromotionPath_MultipleSteps_StepsOrderedByCreation()
    {
        var promotionRepo = Substitute.For<IPromotionRequestRepository>();
        var envRepo = Substitute.For<IDeploymentEnvironmentRepository>();

        var releaseId = Guid.NewGuid();
        var devId = DeploymentEnvironmentId.New();
        var stagingId = DeploymentEnvironmentId.New();
        var prodId = DeploymentEnvironmentId.New();

        var step1 = PromotionRequest.Create(releaseId, devId, stagingId, "engineer@example.com", FixedNow);
        var step2 = PromotionRequest.Create(releaseId, stagingId, prodId, "tech-lead@example.com", FixedNow.AddHours(2));

        promotionRepo.ListByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(new List<PromotionRequest> { step1, step2 }.AsReadOnly());

        envRepo.GetByIdAsync(devId, Arg.Any<CancellationToken>())
            .Returns(DeploymentEnvironment.Create("Development", "Dev", 1, false, false, FixedNow));
        envRepo.GetByIdAsync(stagingId, Arg.Any<CancellationToken>())
            .Returns(DeploymentEnvironment.Create("Staging", "Staging", 2, true, false, FixedNow));
        envRepo.GetByIdAsync(prodId, Arg.Any<CancellationToken>())
            .Returns(DeploymentEnvironment.Create("Production", "Prod", 3, true, true, FixedNow));

        var handler = new GetEnvironmentPromotionPath.Handler(promotionRepo, envRepo);
        var result = await handler.Handle(
            new GetEnvironmentPromotionPath.Query(releaseId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Steps.Should().HaveCount(2);
        result.Value.Steps[0].SourceEnvironment.Should().Be("Development");
        result.Value.Steps[1].TargetEnvironment.Should().Be("Production");
    }
}
