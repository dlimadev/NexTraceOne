using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.ExpireGovernanceWaivers;
using NexTraceOne.Governance.Application.Features.GetPolicyAsCode;
using NexTraceOne.Governance.Application.Features.RegisterPolicyAsCode;
using NexTraceOne.Governance.Application.Features.SimulatePolicyApplication;
using NexTraceOne.Governance.Application.Features.TransitionEnforcementMode;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade Phase 5.5 — Governance Policy Engine V2.
/// </summary>
public sealed class PolicyEngineV2Tests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 5, 15, 0, 0, TimeSpan.Zero);

    private readonly IPolicyAsCodeRepository _policyRepo = Substitute.For<IPolicyAsCodeRepository>();
    private readonly IGovernanceWaiverRepository _waiverRepo = Substitute.For<IGovernanceWaiverRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public PolicyEngineV2Tests()
    {
        _tenant.Id.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── RegisterPolicyAsCode ──────────────────────────────────────────────

    [Fact]
    public async Task RegisterPolicyAsCode_WhenNameDoesNotExist_ShouldCreate()
    {
        _policyRepo.GetByNameAsync("require-openapi", Arg.Any<CancellationToken>())
            .Returns((PolicyAsCodeDefinition?)null);

        var handler = new RegisterPolicyAsCode.Handler(_policyRepo, _tenant, _uow);

        var result = await handler.Handle(new RegisterPolicyAsCode.Command(
            Name: "require-openapi",
            DisplayName: "Require OpenAPI Contract",
            Description: null,
            Version: "1.0.0",
            Format: PolicyDefinitionFormat.Yaml,
            DefinitionContent: "rules:\n  - require-openapi",
            EnforcementMode: PolicyEnforcementMode.Advisory,
            RegisteredBy: "admin"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("require-openapi");
        result.Value.Status.Should().Be(PolicyDefinitionStatus.Draft);
        await _policyRepo.Received(1).AddAsync(Arg.Any<PolicyAsCodeDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterPolicyAsCode_WhenNameAlreadyExists_ShouldReturnConflict()
    {
        var existing = PolicyAsCodeDefinition.Create(
            _tenant.Id, "existing-policy", "Existing", null, "1.0.0",
            PolicyDefinitionFormat.Json, "{}", PolicyEnforcementMode.Advisory, "admin");

        _policyRepo.GetByNameAsync("existing-policy", Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new RegisterPolicyAsCode.Handler(_policyRepo, _tenant, _uow);

        var result = await handler.Handle(new RegisterPolicyAsCode.Command(
            "existing-policy", "Existing", null, "1.0.0",
            PolicyDefinitionFormat.Json, "{}", PolicyEnforcementMode.Advisory, "admin"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _policyRepo.DidNotReceive().AddAsync(Arg.Any<PolicyAsCodeDefinition>(), Arg.Any<CancellationToken>());
    }

    // ── GetPolicyAsCode ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPolicyAsCode_WhenExists_ShouldReturnDetails()
    {
        var policy = PolicyAsCodeDefinition.Create(
            _tenant.Id, "my-policy", "My Policy", "Desc", "1.0.0",
            PolicyDefinitionFormat.Yaml, "rules: []", PolicyEnforcementMode.Advisory, "user1");

        _policyRepo.GetByNameAsync("my-policy", Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new GetPolicyAsCode.Handler(_policyRepo);

        var result = await handler.Handle(new GetPolicyAsCode.Query("my-policy"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("my-policy");
        result.Value.Format.Should().Be(PolicyDefinitionFormat.Yaml);
        result.Value.Status.Should().Be(PolicyDefinitionStatus.Draft);
    }

    [Fact]
    public async Task GetPolicyAsCode_WhenNotFound_ShouldReturnNotFound()
    {
        _policyRepo.GetByNameAsync("ghost", Arg.Any<CancellationToken>())
            .Returns((PolicyAsCodeDefinition?)null);

        var handler = new GetPolicyAsCode.Handler(_policyRepo);

        var result = await handler.Handle(new GetPolicyAsCode.Query("ghost"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── SimulatePolicyApplication ─────────────────────────────────────────

    [Fact]
    public async Task SimulatePolicyApplication_WithSomeNonCompliant_ShouldRecordResult()
    {
        var policy = PolicyAsCodeDefinition.Create(
            _tenant.Id, "sim-policy", "Sim", null, "1.0.0",
            PolicyDefinitionFormat.Yaml, "rules: []", PolicyEnforcementMode.Advisory, "admin");
        policy.Activate();

        _policyRepo.GetByNameAsync("sim-policy", Arg.Any<CancellationToken>()).Returns(policy);

        var handler = new SimulatePolicyApplication.Handler(_policyRepo, _uow, _clock);

        var result = await handler.Handle(new SimulatePolicyApplication.Command(
            PolicyName: "sim-policy",
            ServiceIds: new[] { "svc-a", "svc-b", "svc-c" },
            NonCompliantServiceIds: new[] { "svc-c" }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AffectedServices.Should().Be(3);
        result.Value.NonCompliantServices.Should().Be(1);
        result.Value.CompliancePercent.Should().Be(66.7m);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SimulatePolicyApplication_WhenAllCompliant_ShouldReturn100Percent()
    {
        var policy = PolicyAsCodeDefinition.Create(
            _tenant.Id, "all-good", "All Good", null, "1.0.0",
            PolicyDefinitionFormat.Json, "{}", PolicyEnforcementMode.Advisory, "admin");

        _policyRepo.GetByNameAsync("all-good", Arg.Any<CancellationToken>()).Returns(policy);

        var handler = new SimulatePolicyApplication.Handler(_policyRepo, _uow, _clock);

        var result = await handler.Handle(new SimulatePolicyApplication.Command(
            "all-good", new[] { "svc-a", "svc-b" }, Array.Empty<string>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompliancePercent.Should().Be(100m);
    }

    [Fact]
    public async Task SimulatePolicyApplication_WhenNonCompliantExceedsTotal_ShouldReturnValidationError()
    {
        var policy = PolicyAsCodeDefinition.Create(
            _tenant.Id, "bad-input", "Bad", null, "1.0.0",
            PolicyDefinitionFormat.Json, "{}", PolicyEnforcementMode.Advisory, "admin");

        _policyRepo.GetByNameAsync("bad-input", Arg.Any<CancellationToken>()).Returns(policy);

        var handler = new SimulatePolicyApplication.Handler(_policyRepo, _uow, _clock);

        var result = await handler.Handle(new SimulatePolicyApplication.Command(
            "bad-input", new[] { "svc-a" }, new[] { "svc-a", "svc-b" }),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── TransitionEnforcementMode ─────────────────────────────────────────

    [Fact]
    public async Task TransitionEnforcementMode_AdvisoryToSoftEnforce_ShouldSucceed()
    {
        var policy = PolicyAsCodeDefinition.Create(
            _tenant.Id, "transition-policy", "Trans", null, "1.0.0",
            PolicyDefinitionFormat.Yaml, "rules: []", PolicyEnforcementMode.Advisory, "admin");
        policy.Activate();

        _policyRepo.GetByNameAsync("transition-policy", Arg.Any<CancellationToken>()).Returns(policy);

        var handler = new TransitionEnforcementMode.Handler(_policyRepo, _uow);

        var result = await handler.Handle(new TransitionEnforcementMode.Command(
            PolicyName: "transition-policy",
            TargetMode: PolicyEnforcementMode.SoftEnforce),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviousMode.Should().Be(PolicyEnforcementMode.Advisory);
        result.Value.NewMode.Should().Be(PolicyEnforcementMode.SoftEnforce);
    }

    [Fact]
    public async Task TransitionEnforcementMode_BackwardTransition_ShouldFail()
    {
        var policy = PolicyAsCodeDefinition.Create(
            _tenant.Id, "hard-policy", "Hard", null, "1.0.0",
            PolicyDefinitionFormat.Yaml, "rules: []", PolicyEnforcementMode.HardEnforce, "admin");
        policy.Activate();

        _policyRepo.GetByNameAsync("hard-policy", Arg.Any<CancellationToken>()).Returns(policy);

        var handler = new TransitionEnforcementMode.Handler(_policyRepo, _uow);

        var result = await handler.Handle(new TransitionEnforcementMode.Command(
            "hard-policy", PolicyEnforcementMode.Advisory),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task TransitionEnforcementMode_DraftPolicy_ShouldFail()
    {
        var policy = PolicyAsCodeDefinition.Create(
            _tenant.Id, "draft-policy", "Draft", null, "1.0.0",
            PolicyDefinitionFormat.Yaml, "rules: []", PolicyEnforcementMode.Advisory, "admin");
        // Not activated, stays Draft

        _policyRepo.GetByNameAsync("draft-policy", Arg.Any<CancellationToken>()).Returns(policy);

        var handler = new TransitionEnforcementMode.Handler(_policyRepo, _uow);

        var result = await handler.Handle(new TransitionEnforcementMode.Command(
            "draft-policy", PolicyEnforcementMode.SoftEnforce),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── ExpireGovernanceWaivers ───────────────────────────────────────────

    [Fact]
    public async Task ExpireGovernanceWaivers_WhenWaiversHaveExpired_ShouldRevokeAll()
    {
        var packId = new GovernancePackId(Guid.NewGuid());

        var expired1 = GovernanceWaiver.Create(
            packId, null, "team-a", GovernanceScopeType.Team,
            "Justification A", "user-a",
            FixedNow - TimeSpan.FromDays(1), // expired yesterday
            new List<string>());
        expired1.Approve("approver");

        var expired2 = GovernanceWaiver.Create(
            packId, "rule-1", "team-b", GovernanceScopeType.Team,
            "Justification B", "user-b",
            FixedNow - TimeSpan.FromHours(1), // expired 1 hour ago
            new List<string>());
        expired2.Approve("approver");

        var notExpired = GovernanceWaiver.Create(
            packId, null, "team-c", GovernanceScopeType.Team,
            "Justification C", "user-c",
            FixedNow + TimeSpan.FromDays(30), // expires in the future
            new List<string>());
        notExpired.Approve("approver");

        _waiverRepo.ListAsync(null, WaiverStatus.Approved, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver> { expired1, expired2, notExpired });

        var handler = new ExpireGovernanceWaivers.Handler(_waiverRepo, _uow, _clock);

        var result = await handler.Handle(
            new ExpireGovernanceWaivers.Command("system:waiver-expiry-job"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiredCount.Should().Be(2);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExpireGovernanceWaivers_WhenNoExpiredWaivers_ShouldReturn0AndNotCommit()
    {
        var packId = new GovernancePackId(Guid.NewGuid());
        var active = GovernanceWaiver.Create(
            packId, null, "team-a", GovernanceScopeType.Team,
            "Valid", "user-a", FixedNow + TimeSpan.FromDays(10), new List<string>());
        active.Approve("approver");

        _waiverRepo.ListAsync(null, WaiverStatus.Approved, Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver> { active });

        var handler = new ExpireGovernanceWaivers.Handler(_waiverRepo, _uow, _clock);

        var result = await handler.Handle(
            new ExpireGovernanceWaivers.Command("system:waiver-expiry-job"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiredCount.Should().Be(0);
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── PolicyAsCodeDefinition domain ────────────────────────────────────

    [Fact]
    public void PolicyAsCodeDefinition_Create_ShouldBeInDraftStatus()
    {
        var definition = PolicyAsCodeDefinition.Create(
            Guid.NewGuid(), "test-policy", "Test Policy", null, "1.0.0",
            PolicyDefinitionFormat.Yaml, "rules: []", PolicyEnforcementMode.Advisory, "admin");

        definition.Status.Should().Be(PolicyDefinitionStatus.Draft);
        definition.EnforcementMode.Should().Be(PolicyEnforcementMode.Advisory);
    }

    [Fact]
    public void PolicyAsCodeDefinition_Activate_ShouldTransitionToActive()
    {
        var definition = PolicyAsCodeDefinition.Create(
            Guid.NewGuid(), "test2", "Test2", null, "1.0.0",
            PolicyDefinitionFormat.Json, "{}", PolicyEnforcementMode.Advisory, "admin");

        definition.Activate();

        definition.Status.Should().Be(PolicyDefinitionStatus.Active);
    }

    [Fact]
    public void PolicyAsCodeDefinition_TransitionEnforcement_ForwardOnly_ShouldSucceed()
    {
        var definition = PolicyAsCodeDefinition.Create(
            Guid.NewGuid(), "forward", "Forward", null, "1.0.0",
            PolicyDefinitionFormat.Yaml, "rules: []", PolicyEnforcementMode.Advisory, "admin");

        definition.TransitionEnforcement(PolicyEnforcementMode.SoftEnforce);
        definition.EnforcementMode.Should().Be(PolicyEnforcementMode.SoftEnforce);

        definition.TransitionEnforcement(PolicyEnforcementMode.HardEnforce);
        definition.EnforcementMode.Should().Be(PolicyEnforcementMode.HardEnforce);
    }

    [Fact]
    public void PolicyAsCodeDefinition_TransitionEnforcement_Backward_ShouldThrow()
    {
        var definition = PolicyAsCodeDefinition.Create(
            Guid.NewGuid(), "backward", "Backward", null, "1.0.0",
            PolicyDefinitionFormat.Yaml, "rules: []", PolicyEnforcementMode.HardEnforce, "admin");

        Action act = () => definition.TransitionEnforcement(PolicyEnforcementMode.Advisory);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PolicyAsCodeDefinition_RecordSimulation_ShouldPersistResults()
    {
        var definition = PolicyAsCodeDefinition.Create(
            Guid.NewGuid(), "sim-test", "Sim Test", null, "1.0.0",
            PolicyDefinitionFormat.Json, "{}", PolicyEnforcementMode.Advisory, "admin");

        definition.RecordSimulationResult(10, 3, FixedNow);

        definition.SimulatedAffectedServices.Should().Be(10);
        definition.SimulatedNonCompliantServices.Should().Be(3);
        definition.LastSimulatedAt.Should().Be(FixedNow);
    }
}
