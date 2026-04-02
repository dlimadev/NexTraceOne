using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetComplianceSummary;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NSubstitute;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes para GetComplianceSummary com foco em classificação e agregados.
/// </summary>
public sealed class ComplianceSummaryFeatureTests
{
    [Fact]
    public async Task GetComplianceSummary_WithTeamScope_ShouldAggregateRolloutsAndWaivers()
    {
        // Arrange
        var packRepository = Substitute.For<IGovernancePackRepository>();
        var waiverRepository = Substitute.For<IGovernanceWaiverRepository>();
        var rolloutRepository = Substitute.For<IGovernanceRolloutRecordRepository>();

        var contractsPack = GovernancePack.Create("contracts-baseline", "Contracts Baseline", "desc", GovernanceRuleCategory.Contracts);
        contractsPack.Publish("1.0.0");

        var changesPack = GovernancePack.Create("changes-baseline", "Changes Baseline", "desc", GovernanceRuleCategory.Changes);
        changesPack.Publish("1.0.0");

        packRepository.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack> { contractsPack, changesPack });

        var pendingWaiver = GovernanceWaiver.Create(
            contractsPack.Id,
            null,
            "team-alpha",
            GovernanceScopeType.Team,
            "pending exception",
            "owner",
            null,
            []);

        var approvedWaiver = GovernanceWaiver.Create(
            changesPack.Id,
            null,
            "team-alpha",
            GovernanceScopeType.Team,
            "approved exception",
            "owner",
            null,
            []);
        approvedWaiver.Approve("reviewer");

        waiverRepository.ListAsync(default, default, default)
            .ReturnsForAnyArgs(new List<GovernanceWaiver> { pendingWaiver, approvedWaiver });

        var contractsVersionId = new GovernancePackVersionId(Guid.NewGuid());
        var changesVersionId = new GovernancePackVersionId(Guid.NewGuid());

        var completedRollout = GovernanceRolloutRecord.Create(
            contractsPack.Id,
            contractsVersionId,
            "team-alpha",
            GovernanceScopeType.Team,
            EnforcementMode.Required,
            "deployer");
        completedRollout.MarkCompleted();

        var failedRollout = GovernanceRolloutRecord.Create(
            changesPack.Id,
            changesVersionId,
            "team-alpha",
            GovernanceScopeType.Team,
            EnforcementMode.Required,
            "deployer");
        failedRollout.MarkFailed();

        rolloutRepository.ListAsync(default, default, default, default, default)
            .ReturnsForAnyArgs(new List<GovernanceRolloutRecord> { completedRollout, failedRollout });

        var handler = new GetComplianceSummary.Handler(packRepository, waiverRepository, rolloutRepository);

        // Act
        var result = await handler.Handle(new GetComplianceSummary.Query(TeamId: "team-alpha"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPacksAssessed.Should().Be(2);
        result.Value.NonCompliantCount.Should().Be(1);
        result.Value.PartiallyCompliantCount.Should().Be(1);
        result.Value.CompliantCount.Should().Be(0);

        result.Value.TotalWaivers.Should().Be(2);
        result.Value.PendingWaivers.Should().Be(1);
        result.Value.ApprovedWaivers.Should().Be(1);

        result.Value.TotalRollouts.Should().Be(2);
        result.Value.CompletedRollouts.Should().Be(1);
        result.Value.FailedRollouts.Should().Be(1);

        result.Value.OverallScore.Should().Be(50.0m);
    }

    [Fact]
    public async Task GetComplianceSummary_WithoutWaivers_ShouldMarkPackAsCompliant()
    {
        // Arrange
        var packRepository = Substitute.For<IGovernancePackRepository>();
        var waiverRepository = Substitute.For<IGovernanceWaiverRepository>();
        var rolloutRepository = Substitute.For<IGovernanceRolloutRecordRepository>();

        var reliabilityPack = GovernancePack.Create("reliability-baseline", "Reliability Baseline", "desc", GovernanceRuleCategory.Reliability);
        reliabilityPack.Publish("1.0.0");

        packRepository.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack> { reliabilityPack });

        waiverRepository.ListAsync(default, default, default)
            .ReturnsForAnyArgs(new List<GovernanceWaiver>());

        rolloutRepository.ListAsync(default, default, default, default, default)
            .ReturnsForAnyArgs(new List<GovernanceRolloutRecord>());

        var handler = new GetComplianceSummary.Handler(packRepository, waiverRepository, rolloutRepository);

        // Act
        var result = await handler.Handle(new GetComplianceSummary.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPacksAssessed.Should().Be(1);
        result.Value.CompliantCount.Should().Be(1);
        result.Value.PartiallyCompliantCount.Should().Be(0);
        result.Value.NonCompliantCount.Should().Be(0);
        result.Value.Packs.Should().ContainSingle(p => p.Status == ComplianceStatus.Compliant);
        result.Value.OverallScore.Should().Be(100.0m);
    }
}
