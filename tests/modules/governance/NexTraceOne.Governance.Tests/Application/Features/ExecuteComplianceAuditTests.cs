using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.ExecuteComplianceAudit;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para a feature ExecuteComplianceAudit.
/// Verifica a lógica de auditoria de compliance, persistência de gaps e publicação de eventos.
/// </summary>
public sealed class ExecuteComplianceAuditTests
{
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();
    private readonly IGovernanceDomainRepository _domainRepo = Substitute.For<IGovernanceDomainRepository>();
    private readonly IGovernancePackRepository _packRepo = Substitute.For<IGovernancePackRepository>();
    private readonly IGovernanceWaiverRepository _waiverRepo = Substitute.For<IGovernanceWaiverRepository>();
    private readonly IComplianceGapRepository _gapRepo = Substitute.For<IComplianceGapRepository>();
    private readonly IGovernanceUnitOfWork _unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private ExecuteComplianceAudit.Handler CreateHandler()
        => new(_teamRepo, _domainRepo, _packRepo, _waiverRepo, _gapRepo, _unitOfWork, _eventBus);

    private static GovernancePack CreatePublishedPack()
    {
        var pack = GovernancePack.Create("api-standards", "API Standards", null, GovernanceRuleCategory.ChangeManagement);
        pack.Publish("1.0.0");
        return pack;
    }

    // ── Test 1: All checks passed ──────────────────────────────────────────────

    [Fact]
    public async Task ExecuteComplianceAudit_AllChecksPassed_ShouldReturnZeroFailures()
    {
        // Arrange
        var activeTeam = Team.Create("platform", "Platform Team");
        var domain = GovernanceDomain.Create("payments", "Payments Domain", criticality: DomainCriticality.High);
        var publishedPack = CreatePublishedPack();

        _teamRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<Team> { activeTeam });
        _domainRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceDomain> { domain });
        _packRepo.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack> { publishedPack });
        _waiverRepo.ListAsync(null, WaiverStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ExecuteComplianceAudit.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FailedCount.Should().Be(0);
        result.Value.PassedCount.Should().BeGreaterThan(0);

        // Only RiskReportGenerated should be published — no ComplianceGapsDetected
        await _eventBus.Received(1).PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _gapRepo.DidNotReceive().AddAsync(Arg.Any<ComplianceGap>(), Arg.Any<CancellationToken>());
    }

    // ── Test 2: Inactive team → gap persisted ─────────────────────────────────

    [Fact]
    public async Task ExecuteComplianceAudit_InactiveTeam_ShouldPersistGap()
    {
        // Arrange
        var inactiveTeam = Team.Create("legacy", "Legacy Team");
        inactiveTeam.Deactivate();
        var domain = GovernanceDomain.Create("core", "Core Domain", criticality: DomainCriticality.Medium);
        var publishedPack = CreatePublishedPack();

        _teamRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<Team> { inactiveTeam });
        _domainRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceDomain> { domain });
        _packRepo.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack> { publishedPack });
        _waiverRepo.ListAsync(null, WaiverStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ExecuteComplianceAudit.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FailedCount.Should().Be(1);
        result.Value.GapsPersisted.Should().Be(1);

        await _gapRepo.Received(1).AddAsync(Arg.Any<ComplianceGap>(), Arg.Any<CancellationToken>());
    }

    // ── Test 3: No published packs → critical gap ─────────────────────────────

    [Fact]
    public async Task ExecuteComplianceAudit_NoPublishedPacks_ShouldPersistCriticalGap()
    {
        // Arrange
        var activeTeam = Team.Create("platform", "Platform Team");
        var domain = GovernanceDomain.Create("core", "Core Domain", criticality: DomainCriticality.High);
        var draftPack = GovernancePack.Create("draft-pack", "Draft Pack", null, GovernanceRuleCategory.ChangeManagement);
        // draftPack stays in Draft status — never published

        _teamRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<Team> { activeTeam });
        _domainRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceDomain> { domain });
        _packRepo.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack> { draftPack });
        _waiverRepo.ListAsync(null, WaiverStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ExecuteComplianceAudit.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FailedCount.Should().Be(1);

        // The gap for pol-pack-published must be persisted with Critical severity
        await _gapRepo.Received(1).AddAsync(
            Arg.Is<ComplianceGap>(g => g.Severity == PolicySeverity.Critical),
            Arg.Any<CancellationToken>());
    }

    // ── Test 4: Excessive pending waivers → gap ───────────────────────────────

    [Fact]
    public async Task ExecuteComplianceAudit_ExcessivePendingWaivers_ShouldPersistGap()
    {
        // Arrange — 4 pending waivers exceeds the limit of 3
        var activeTeam = Team.Create("platform", "Platform Team");
        var domain = GovernanceDomain.Create("core", "Core Domain", criticality: DomainCriticality.High);
        var publishedPack = CreatePublishedPack();

        _teamRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<Team> { activeTeam });
        _domainRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceDomain> { domain });
        _packRepo.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack> { publishedPack });

        var packId = new GovernancePackId(Guid.NewGuid());
        var pendingWaivers = Enumerable.Range(0, 4)
            .Select(_ => GovernanceWaiver.Create(
                packId, null, "platform-team", GovernanceScopeType.Team,
                "Temporary exception for Q3", "admin@company.com",
                null, Array.Empty<string>()))
            .ToList();
        _waiverRepo.ListAsync(null, WaiverStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(pendingWaivers);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ExecuteComplianceAudit.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FailedCount.Should().BeGreaterThan(0);
        await _gapRepo.Received(1).AddAsync(Arg.Any<ComplianceGap>(), Arg.Any<CancellationToken>());
    }

    // ── Test 5: Multiple failures → both events published ─────────────────────

    [Fact]
    public async Task ExecuteComplianceAudit_MultipleFailures_ShouldPublishBothEvents()
    {
        // Arrange — inactive team + no published packs = 2 failures
        var inactiveTeam = Team.Create("legacy", "Legacy Team");
        inactiveTeam.Deactivate();
        var domain = GovernanceDomain.Create("core", "Core Domain", criticality: DomainCriticality.High);
        var draftPack = GovernancePack.Create("draft-pack", "Draft Pack", null, GovernanceRuleCategory.ChangeManagement);

        _teamRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<Team> { inactiveTeam });
        _domainRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceDomain> { domain });
        _packRepo.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack> { draftPack });
        _waiverRepo.ListAsync(null, WaiverStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ExecuteComplianceAudit.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FailedCount.Should().Be(2);

        // Both ComplianceGapsDetected and RiskReportGenerated must be published
        await _eventBus.Received(2).PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _gapRepo.Received(2).AddAsync(Arg.Any<ComplianceGap>(), Arg.Any<CancellationToken>());
    }
}
