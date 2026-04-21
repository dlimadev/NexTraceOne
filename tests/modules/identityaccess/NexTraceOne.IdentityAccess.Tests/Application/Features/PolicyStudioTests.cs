using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.CreatePolicyDefinition;
using NexTraceOne.IdentityAccess.Application.Features.EvaluatePolicyDefinition;
using NexTraceOne.IdentityAccess.Application.Features.ListPolicyDefinitions;
using NexTraceOne.IdentityAccess.Application.Features.UpdatePolicyDefinition;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários para o Policy Studio (Wave D.3 — No-code Policy Studio).
/// Cobre: domínio de PolicyDefinition, engine de avaliação de regras JSON e handlers CQRS.
/// </summary>
public sealed class PolicyStudioTests
{
    private readonly IPolicyDefinitionRepository _policyRepo = Substitute.For<IPolicyDefinitionRepository>();
    private readonly IIdentityAccessUnitOfWork _uow = Substitute.For<IIdentityAccessUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset Now = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);

    public PolicyStudioTests()
    {
        _clock.UtcNow.Returns(Now);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    // ── Domain entity tests ───────────────────────────────────────────────

    [Fact]
    public void PolicyDefinition_Create_Sets_Correct_Initial_Values()
    {
        var policy = CreateSamplePolicy();

        policy.TenantId.Should().Be("tenant-1");
        policy.Name.Should().Be("Test Policy");
        policy.PolicyType.Should().Be(PolicyDefinitionType.PromotionGate);
        policy.IsEnabled.Should().BeTrue();
        policy.Version.Should().Be(1);
        policy.AppliesTo.Should().Be("*");
    }

    [Fact]
    public void PolicyDefinition_Enable_Sets_IsEnabled_True()
    {
        var policy = CreateSamplePolicy();
        policy.Disable();
        policy.Enable();

        policy.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void PolicyDefinition_Disable_Sets_IsEnabled_False()
    {
        var policy = CreateSamplePolicy();
        policy.Disable();

        policy.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void PolicyDefinition_UpdateRules_Bumps_Version()
    {
        var policy = CreateSamplePolicy();
        var initialVersion = policy.Version;

        policy.UpdateRules(
            """[{"Field":"env","Operator":"Equals","Value":"production"}]""",
            """{"action":"Block","message":"Not allowed in production"}""",
            Now);

        policy.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void PolicyDefinition_Evaluate_Returns_Allow_When_All_Rules_Pass()
    {
        var policy = PolicyDefinition.Create(
            "tenant-1", "Allow All", null, PolicyDefinitionType.AccessControl,
            """[{"Field":"env","Operator":"Equals","Value":"staging"}]""",
            """{"action":"Block","message":"blocked"}""",
            "*", null, null, Now);

        var result = policy.Evaluate("""{"env":"staging"}""");

        result.Passed.Should().BeTrue();
        result.Action.Should().Be("Allow");
    }

    [Fact]
    public void PolicyDefinition_Evaluate_Returns_Block_When_Rule_Fails()
    {
        var policy = PolicyDefinition.Create(
            "tenant-1", "Block prod", null, PolicyDefinitionType.PromotionGate,
            """[{"Field":"env","Operator":"Equals","Value":"staging"}]""",
            """{"action":"Block","message":"Must be staging"}""",
            "*", null, null, Now);

        var result = policy.Evaluate("""{"env":"production"}""");

        result.Passed.Should().BeFalse();
        result.Action.Should().Be("Block");
        result.Message.Should().Be("Must be staging");
        result.RuleTriggered.Should().Contain("env");
    }

    [Fact]
    public void PolicyDefinition_Evaluate_Handles_GreaterThan_Operator()
    {
        var policy = PolicyDefinition.Create(
            "tenant-1", "Min coverage", null, PolicyDefinitionType.ComplianceCheck,
            """[{"Field":"coverage","Operator":"GreaterThan","Value":"80"}]""",
            """{"action":"Warn","message":"Coverage below threshold"}""",
            "*", null, null, Now);

        var resultPass = policy.Evaluate("""{"coverage":"95"}""");
        var resultFail = policy.Evaluate("""{"coverage":"70"}""");

        resultPass.Passed.Should().BeTrue();
        resultFail.Passed.Should().BeFalse();
        resultFail.Action.Should().Be("Warn");
    }

    [Fact]
    public void PolicyDefinition_Evaluate_Handles_Contains_Operator()
    {
        var policy = PolicyDefinition.Create(
            "tenant-1", "Service filter", null, PolicyDefinitionType.AccessControl,
            """[{"Field":"service","Operator":"Contains","Value":"payment"}]""",
            """{"action":"Block","message":"Service not allowed"}""",
            "*", null, null, Now);

        var resultPass = policy.Evaluate("""{"service":"payment-service"}""");
        var resultFail = policy.Evaluate("""{"service":"catalog-service"}""");

        resultPass.Passed.Should().BeTrue();
        resultFail.Passed.Should().BeFalse();
    }

    // ── Handler tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePolicyDefinition_Handler_Creates_And_Returns_Id()
    {
        var handler = new CreatePolicyDefinition.Handler(_policyRepo, _uow, _clock);
        var result = await handler.Handle(new CreatePolicyDefinition.Command(
            "tenant-1", "Gate Policy", null, PolicyDefinitionType.PromotionGate,
            """[]""", """{"action":"Block","message":"blocked"}""", "*", null, "admin"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Gate Policy");
        result.Value.Version.Should().Be(1);
        _policyRepo.Received(1).Add(Arg.Any<PolicyDefinition>());
    }

    [Fact]
    public async Task UpdatePolicyDefinition_Handler_Returns_NotFound_For_Unknown_Id()
    {
        var unknownId = Guid.NewGuid();
        _policyRepo.GetByIdAsync(Arg.Any<PolicyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns((PolicyDefinition?)null);

        var handler = new UpdatePolicyDefinition.Handler(_policyRepo, _uow, _clock);
        var result = await handler.Handle(new UpdatePolicyDefinition.Command(
            unknownId, "tenant-1", """[]""", """{"action":"Block","message":"x"}""", true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task UpdatePolicyDefinition_Handler_Updates_Rules_And_Bumps_Version()
    {
        var policy = CreateSamplePolicy();
        _policyRepo.GetByIdAsync(Arg.Any<PolicyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new UpdatePolicyDefinition.Handler(_policyRepo, _uow, _clock);
        var result = await handler.Handle(new UpdatePolicyDefinition.Command(
            policy.Id.Value, "tenant-1",
            """[{"Field":"env","Operator":"Equals","Value":"production"}]""",
            """{"action":"Warn","message":"check"}""",
            true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().Be(2);
    }

    [Fact]
    public async Task EvaluatePolicyDefinition_Handler_Returns_NotFound_For_Unknown_Id()
    {
        _policyRepo.GetByIdAsync(Arg.Any<PolicyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns((PolicyDefinition?)null);

        var handler = new EvaluatePolicyDefinition.Handler(_policyRepo);
        var result = await handler.Handle(new EvaluatePolicyDefinition.Query(Guid.NewGuid(), "{}"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task EvaluatePolicyDefinition_Handler_Returns_Disabled_Error_When_Disabled()
    {
        var policy = CreateSamplePolicy();
        policy.Disable();
        _policyRepo.GetByIdAsync(Arg.Any<PolicyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new EvaluatePolicyDefinition.Handler(_policyRepo);
        var result = await handler.Handle(new EvaluatePolicyDefinition.Query(policy.Id.Value, "{}"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Disabled");
    }

    [Fact]
    public async Task EvaluatePolicyDefinition_Handler_Returns_Evaluation_Result()
    {
        var policy = PolicyDefinition.Create(
            "tenant-1", "Eval test", null, PolicyDefinitionType.PromotionGate,
            """[{"Field":"env","Operator":"Equals","Value":"staging"}]""",
            """{"action":"Block","message":"blocked"}""",
            "*", null, null, Now);
        _policyRepo.GetByIdAsync(Arg.Any<PolicyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new EvaluatePolicyDefinition.Handler(_policyRepo);
        var result = await handler.Handle(new EvaluatePolicyDefinition.Query(policy.Id.Value, """{"env":"staging"}"""), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeTrue();
        result.Value.Action.Should().Be("Allow");
    }

    [Fact]
    public async Task ListPolicyDefinitions_Handler_Filters_By_Enabled()
    {
        var enabledPolicy = CreateSamplePolicy();
        var disabledPolicy = CreateSamplePolicy();
        disabledPolicy.Disable();

        _policyRepo.ListByTenantAsync("tenant-1", Arg.Any<PolicyDefinitionType?>(), Arg.Any<CancellationToken>())
            .Returns([enabledPolicy, disabledPolicy]);

        var handler = new ListPolicyDefinitions.Handler(_policyRepo);
        var result = await handler.Handle(new ListPolicyDefinitions.Query("tenant-1", null, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Policies.Should().HaveCount(1);
        result.Value.Policies[0].IsEnabled.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private PolicyDefinition CreateSamplePolicy() =>
        PolicyDefinition.Create(
            tenantId: "tenant-1",
            name: "Test Policy",
            description: null,
            policyType: PolicyDefinitionType.PromotionGate,
            rulesJson: """[]""",
            actionJson: """{"action":"Block","message":"blocked"}""",
            appliesTo: "*",
            environmentFilter: null,
            createdByUserId: "admin",
            now: Now);
}
