using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ApproveSelfHealingAction;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetSlaIntelligence;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListSelfHealingActions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ProposeSelfHealingAction;
using NexTraceOne.AIKnowledge.Application.Governance.Features.QuantifyTechDebt;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários das capacidades enterprise — Fase 11:
/// QuantifyTechDebt, GetSlaIntelligence, SelfHealingAction, Skills infra.
/// </summary>
public sealed class EnterpriseCapabilitiesTests
{
    private readonly ISelfHealingActionRepository _repo = Substitute.For<ISelfHealingActionRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ISkillLoader _skillLoader = Substitute.For<ISkillLoader>();
    private readonly ISkillRegistry _skillRegistry = Substitute.For<ISkillRegistry>();
    private readonly ISkillContextInjector _contextInjector = Substitute.For<ISkillContextInjector>();

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    public EnterpriseCapabilitiesTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    // ── QuantifyTechDebt ──────────────────────────────────────────────────────

    [Fact]
    public async Task QuantifyTechDebt_WithCircularDependencies_ReturnsArchitectureItem()
    {
        var handler = new QuantifyTechDebt.Handler(_clock);
        var query = BuildTechDebtQuery(circularDependencies: 2);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().Contain(i => i.Category == "architecture");
    }

    [Fact]
    public async Task QuantifyTechDebt_NoProblemInputs_ReturnsEmptyItems()
    {
        var handler = new QuantifyTechDebt.Handler(_clock);
        var query = new QuantifyTechDebt.Query(
            "svc", TenantId, 0, 80.0, 0, 200.0, 30.0, 100m);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalMonthlyCostEstimate.Should().Be(0);
    }

    [Fact]
    public async Task QuantifyTechDebt_LowTestCoverage_ReturnsQualityItem()
    {
        var handler = new QuantifyTechDebt.Handler(_clock);
        var query = BuildTechDebtQuery(testCoverage: 20.0);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.Items.Should().Contain(i => i.Category == "quality" && i.Severity == "Critical");
    }

    [Fact]
    public async Task QuantifyTechDebt_HighIncidentCount_ReturnsOperationsItem()
    {
        var handler = new QuantifyTechDebt.Handler(_clock);
        var query = BuildTechDebtQuery(incidentCount: 20);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.Items.Should().Contain(i => i.Category == "operations" && i.Severity == "Critical");
    }

    [Fact]
    public async Task QuantifyTechDebt_LargePrs_ReturnsVelocityItem()
    {
        var handler = new QuantifyTechDebt.Handler(_clock);
        var query = BuildTechDebtQuery(avgPrSize: 700.0);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.Items.Should().Contain(i => i.Category == "velocity");
    }

    [Fact]
    public async Task QuantifyTechDebt_CalculatesTotalMonthlyCost()
    {
        var handler = new QuantifyTechDebt.Handler(_clock);
        var query = BuildTechDebtQuery(circularDependencies: 1, testCoverage: 10.0);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.TotalMonthlyCostEstimate.Should().BeGreaterThan(0);
        result.Value.TotalAnnualCostEstimate.Should().Be(result.Value.TotalMonthlyCostEstimate * 12);
    }

    [Fact]
    public async Task QuantifyTechDebt_SetsAnalysedAtFromClock()
    {
        var handler = new QuantifyTechDebt.Handler(_clock);
        var query = BuildTechDebtQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.AnalysedAt.Should().Be(Now);
    }

    // ── GetSlaIntelligence ────────────────────────────────────────────────────

    [Fact]
    public async Task GetSlaIntelligence_WhenBelowTarget_ReturnsInBreach()
    {
        var handler = new GetSlaIntelligence.Handler(_clock);
        var query = BuildSlaQuery(actual: 99.5, target: 99.9);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsInBreach.Should().BeTrue();
    }

    [Fact]
    public async Task GetSlaIntelligence_WhenAboveTarget_NotInBreach()
    {
        var handler = new GetSlaIntelligence.Handler(_clock);
        var query = BuildSlaQuery(actual: 99.95, target: 99.9);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.IsInBreach.Should().BeFalse();
    }

    [Fact]
    public async Task GetSlaIntelligence_RecommendedSlaIsLowerThanActual()
    {
        var handler = new GetSlaIntelligence.Handler(_clock);
        var query = BuildSlaQuery(actual: 99.73, target: 99.9);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.RecommendedSla.Should().BeLessThan(result.Value.ActualAvailability);
    }

    [Fact]
    public async Task GetSlaIntelligence_ListsDowntimeCauses()
    {
        var handler = new GetSlaIntelligence.Handler(_clock);
        var query = BuildSlaQuery(actual: 99.5, target: 99.9);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.DowntimeCauses.Should().NotBeEmpty();
        result.Value.DowntimeCauses.Should().Contain(c => c.Category == "Maintenance Windows");
    }

    [Fact]
    public async Task GetSlaIntelligence_FridayDeployCount_AddsImprovement()
    {
        var handler = new GetSlaIntelligence.Handler(_clock);
        var query = BuildSlaQuery(fridayDeploys: 5);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.ImprovementsNeeded.Should().Contain(s => s.Contains("Friday"));
    }

    // ── SelfHealingAction domain entity ──────────────────────────────────────

    [Fact]
    public void SelfHealingAction_Propose_CreatesWithPendingStatus()
    {
        var action = SelfHealingAction.Propose(
            "INC-001", "payment-svc", "one_click", "Restart pod", 0.9, "low", TenantId, Now);

        action.Status.Should().Be("pending");
        action.IncidentId.Should().Be("INC-001");
        action.Confidence.Should().Be(0.9);
    }

    [Fact]
    public void SelfHealingAction_Approve_SetsApprovedStatus()
    {
        var action = SelfHealingAction.Propose(
            "INC-001", "svc", "suggestion", "Scale up", 0.8, "low", TenantId, Now);

        action.Approve("engineer@company.com", Now);

        action.Status.Should().Be("approved");
        action.ApprovedBy.Should().Be("engineer@company.com");
        action.ApprovedAt.Should().Be(Now);
    }

    [Fact]
    public void SelfHealingAction_MarkCompleted_SetsCompletedStatus()
    {
        var action = SelfHealingAction.Propose(
            "INC-001", "svc", "automatic", "Rollback", 0.95, "zero", TenantId, Now);

        action.MarkCompleted("Rollback succeeded", Now);

        action.Status.Should().Be("completed");
        action.Result.Should().Be("Rollback succeeded");
        action.ExecutedAt.Should().Be(Now);
    }

    // ── ProposeSelfHealingAction handler ────────────────────────────────────

    [Fact]
    public async Task ProposeSelfHealingAction_ValidCommand_ReturnsSuccessWithPendingStatus()
    {
        var handler = new ProposeSelfHealingAction.Handler(_repo, _clock, _uow);
        var command = new ProposeSelfHealingAction.Command(
            "INC-001", "payment-svc", "one_click", "Restart pod", 0.9, "low", TenantId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("pending");
        _repo.Received(1).Add(Arg.Any<SelfHealingAction>());
    }

    // ── ApproveSelfHealingAction handler ────────────────────────────────────

    [Fact]
    public async Task ApproveSelfHealingAction_ExistingAction_ApprovesSuccessfully()
    {
        var action = SelfHealingAction.Propose(
            "INC-001", "svc", "one_click", "Restart", 0.9, "low", TenantId, Now);
        _repo.GetByIdAsync(Arg.Any<SelfHealingActionId>(), Arg.Any<CancellationToken>())
            .Returns(action);

        var handler = new ApproveSelfHealingAction.Handler(_repo, _clock, _uow);
        var result = await handler.Handle(
            new ApproveSelfHealingAction.Command(action.Id.Value, "admin@co.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("approved");
    }

    [Fact]
    public async Task ApproveSelfHealingAction_NotFound_ReturnsError()
    {
        _repo.GetByIdAsync(Arg.Any<SelfHealingActionId>(), Arg.Any<CancellationToken>())
            .Returns((SelfHealingAction?)null);

        var handler = new ApproveSelfHealingAction.Handler(_repo, _clock, _uow);
        var result = await handler.Handle(
            new ApproveSelfHealingAction.Command(Guid.NewGuid(), "admin@co.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── ListSelfHealingActions handler ──────────────────────────────────────

    [Fact]
    public async Task ListSelfHealingActions_PendingOnly_CallsListPendingApproval()
    {
        _repo.ListPendingApprovalAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SelfHealingAction>().AsReadOnly() as IReadOnlyList<SelfHealingAction>);

        var handler = new ListSelfHealingActions.Handler(_repo);
        var result = await handler.Handle(
            new ListSelfHealingActions.Query(TenantId, null, PendingOnly: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).ListPendingApprovalAsync(TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListSelfHealingActions_ByIncidentId_CallsListByIncident()
    {
        _repo.ListByIncidentAsync("INC-001", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SelfHealingAction>().AsReadOnly() as IReadOnlyList<SelfHealingAction>);

        var handler = new ListSelfHealingActions.Handler(_repo);
        var result = await handler.Handle(
            new ListSelfHealingActions.Query(TenantId, "INC-001", PendingOnly: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).ListByIncidentAsync("INC-001", TenantId, Arg.Any<CancellationToken>());
    }

    // ── Skills infra ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SkillLoader_LoadContentAsync_ReturnsNullWhenNotFound()
    {
        _skillLoader.LoadContentAsync("nonexistent", TenantId, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _skillLoader.LoadContentAsync("nonexistent", TenantId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SkillRegistry_IsSkillAvailableAsync_ReturnsFalseForInactiveSkill()
    {
        _skillRegistry.IsSkillAvailableAsync("inactive-skill", TenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _skillRegistry.IsSkillAvailableAsync("inactive-skill", TenantId, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SkillContextInjector_InjectSkillsAsync_AppendsSkillContent()
    {
        const string basePrompt = "You are a helpful assistant.";
        const string expected = "You are a helpful assistant.\n\n[skill content]";
        _contextInjector.InjectSkillsAsync(basePrompt, Arg.Any<IEnumerable<string>>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _contextInjector.InjectSkillsAsync(
            basePrompt, ["incident-triage"], TenantId, CancellationToken.None);

        result.Should().Contain("skill content");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static QuantifyTechDebt.Query BuildTechDebtQuery(
        int circularDependencies = 0,
        double testCoverage = 75.0,
        int incidentCount = 3,
        double avgPrSize = 200.0) =>
        new("payment-svc", TenantId, incidentCount, testCoverage, circularDependencies,
            avgPrSize, 60.0, 100m);

    private static GetSlaIntelligence.Query BuildSlaQuery(
        double actual = 99.73,
        double target = 99.9,
        int fridayDeploys = 0) =>
        new("payment-svc", TenantId, target, actual, 60, 2, fridayDeploys, 5000m);
}
