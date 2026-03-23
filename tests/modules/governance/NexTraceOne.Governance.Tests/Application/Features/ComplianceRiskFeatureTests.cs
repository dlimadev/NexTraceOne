using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetBenchmarking;
using NexTraceOne.Governance.Application.Features.GetComplianceGaps;
using NexTraceOne.Governance.Application.Features.RunComplianceChecks;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using NSubstitute;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de compliance e risco.
/// </summary>
public sealed class ComplianceRiskFeatureTests
{
    private static readonly CostRecordSummary[] SampleRecords =
    [
        new("svc-payment-api", "Payment API", "Team Payments", "Payments", "Production", 12500m, "EUR", "2026-03", "azure"),
        new("svc-order-processor", "Order Processor", "Team Commerce", "Commerce", "Production", 18700m, "EUR", "2026-03", "azure")
    ];

    private static ICostIntelligenceModule CreateMock()
    {
        var mock = Substitute.For<ICostIntelligenceModule>();
        mock.GetCostRecordsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRecords);
        return mock;
    }

    private static (ITeamRepository, IGovernanceDomainRepository, IGovernancePackRepository, IGovernanceWaiverRepository) CreateGovernanceMocks()
    {
        var teamRepo = Substitute.For<ITeamRepository>();
        teamRepo.ListAsync(Arg.Any<TeamStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<Team>
            {
                Team.Create("platform-core", "Platform Core"),
                Team.Create("payments-squad", "Payments Squad")
            });

        var domainRepo = Substitute.For<IGovernanceDomainRepository>();
        domainRepo.ListAsync(Arg.Any<DomainCriticality?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceDomain>
            {
                GovernanceDomain.Create("payments", "Payments", criticality: DomainCriticality.Critical),
                GovernanceDomain.Create("platform", "Platform", criticality: DomainCriticality.High)
            });

        var packRepo = Substitute.For<IGovernancePackRepository>();
        var publishedPack = GovernancePack.Create("contracts-baseline", "Contracts Baseline", "Contract governance policies", GovernanceRuleCategory.Contracts);
        publishedPack.Publish("1.0.0");
        packRepo.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack> { publishedPack });

        var waiverRepo = Substitute.For<IGovernanceWaiverRepository>();
        waiverRepo.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<WaiverStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());

        return (teamRepo, domainRepo, packRepo, waiverRepo);
    }

    // ── GetComplianceGaps ──

    [Fact]
    public async Task GetComplianceGaps_ShouldReturnGaps()
    {
        // Arrange
        var handler = new GetComplianceGaps.Handler();
        var query = new GetComplianceGaps.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalGaps.Should().BeGreaterThan(0);
        result.Value.Gaps.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetComplianceGaps_ShouldContainCriticalAndHighCounts()
    {
        // Arrange
        var handler = new GetComplianceGaps.Handler();

        // Act
        var result = await handler.Handle(new GetComplianceGaps.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CriticalCount.Should().BeGreaterThanOrEqualTo(0);
        result.Value.HighCount.Should().BeGreaterThanOrEqualTo(0);
        result.Value.MediumCount.Should().BeGreaterThanOrEqualTo(0);
        result.Value.LowCount.Should().BeGreaterThanOrEqualTo(0);
        (result.Value.CriticalCount + result.Value.HighCount + result.Value.MediumCount + result.Value.LowCount)
            .Should().Be(result.Value.TotalGaps);
    }

    [Fact]
    public async Task GetComplianceGaps_GapsShouldHaveViolatedPolicies()
    {
        // Arrange
        var handler = new GetComplianceGaps.Handler();

        // Act
        var result = await handler.Handle(new GetComplianceGaps.Query(), CancellationToken.None);

        // Assert
        result.Value.Gaps.Should().AllSatisfy(gap =>
        {
            gap.GapId.Should().NotBeNullOrWhiteSpace();
            gap.ViolatedPolicyIds.Should().NotBeEmpty();
            gap.ViolationCount.Should().BeGreaterThan(0);
        });
    }

    // ── GetBenchmarking ──

    [Fact]
    public async Task GetBenchmarking_ShouldReturnComparisons()
    {
        // Arrange
        var handler = new GetBenchmarking.Handler(CreateMock());
        var query = new GetBenchmarking.Query("teams");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Comparisons.Should().NotBeEmpty();
        result.Value.Dimension.Should().Be("teams");
        result.Value.IsSimulated.Should().BeFalse();
        result.Value.DataSource.Should().Be("cost-intelligence");
    }

    [Fact]
    public async Task GetBenchmarking_ComparisonsShouldHaveGroupInfo()
    {
        // Arrange
        var handler = new GetBenchmarking.Handler(CreateMock());

        // Act
        var result = await handler.Handle(new GetBenchmarking.Query("teams"), CancellationToken.None);

        // Assert
        result.Value.Comparisons.Should().AllSatisfy(c =>
        {
            c.GroupId.Should().NotBeNullOrWhiteSpace();
            c.GroupName.Should().NotBeNullOrWhiteSpace();
        });
    }

    // ── RunComplianceChecks ──

    [Fact]
    public async Task RunComplianceChecks_ShouldReturnResults()
    {
        // Arrange
        var (teamRepo, domainRepo, packRepo, waiverRepo) = CreateGovernanceMocks();
        var handler = new RunComplianceChecks.Handler(teamRepo, domainRepo, packRepo, waiverRepo);
        var query = new RunComplianceChecks.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalChecks.Should().BeGreaterThan(0);
        result.Value.Results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RunComplianceChecks_CountsShouldSumToTotal()
    {
        // Arrange
        var (teamRepo, domainRepo, packRepo, waiverRepo) = CreateGovernanceMocks();
        var handler = new RunComplianceChecks.Handler(teamRepo, domainRepo, packRepo, waiverRepo);

        // Act
        var result = await handler.Handle(new RunComplianceChecks.Query(), CancellationToken.None);

        // Assert
        (result.Value.PassedCount + result.Value.FailedCount + result.Value.WarningCount)
            .Should().Be(result.Value.TotalChecks);
    }

    [Fact]
    public async Task RunComplianceChecks_ResultsShouldHaveRequiredFields()
    {
        // Arrange
        var (teamRepo, domainRepo, packRepo, waiverRepo) = CreateGovernanceMocks();
        var handler = new RunComplianceChecks.Handler(teamRepo, domainRepo, packRepo, waiverRepo);

        // Act
        var result = await handler.Handle(new RunComplianceChecks.Query(), CancellationToken.None);

        // Assert
        result.Value.Results.Should().AllSatisfy(r =>
        {
            r.CheckId.Should().NotBeNullOrWhiteSpace();
            r.CheckName.Should().NotBeNullOrWhiteSpace();
            r.PolicyId.Should().NotBeNullOrWhiteSpace();
            r.ServiceId.Should().NotBeNullOrWhiteSpace();
        });
    }
}
