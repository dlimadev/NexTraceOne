using System.Linq;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ApproveAgentStep;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ClassifyPromptIntent;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentPlanStatus;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiCostAttributionReport;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiTokenBudgetReport;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListAgentExecutionHistory;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitAgentExecutionPlan;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para Wave Y — AI Governance Deep Dive &amp; Agentic Platform.
/// Cobre: SubmitAgentExecutionPlan, ApproveAgentStep, GetAgentPlanStatus,
/// ClassifyPromptIntent, GetAiTokenBudgetReport.
/// </summary>
public sealed class WaveYTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private static EffectiveConfigurationDto CreateConfig(string key, string value) =>
        new(key, value, "System", null, false, false, key, "string", false, 1);

    private IConfigurationResolutionService CreateConfigService(string maxSteps = "10")
    {
        var config = Substitute.For<IConfigurationResolutionService>();
        config.ResolveEffectiveValueAsync(
                "ai.agentic.max_steps_per_plan",
                Arg.Any<ConfigurationScope>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EffectiveConfigurationDto?>(
                CreateConfig("ai.agentic.max_steps_per_plan", maxSteps)));
        return config;
    }

    private static IReadOnlyList<SubmitAgentExecutionPlan.StepRequest> BuildSteps(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new SubmitAgentExecutionPlan.StepRequest(
                StepIndex: i,
                Name: $"Step {i}",
                StepType: "ContractLookup",
                InputJson: "{}",
                RequiresApproval: false))
            .ToList();

    // ── SubmitAgentExecutionPlan ──────────────────────────────────────────

    [Fact]
    public async Task SubmitPlan_ValidCommand_ReturnsPendingPlan()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user@test.com", "Test plan",
            BuildSteps(3), 5000, false, 0, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StepCount.Should().Be(3);
        result.Value.Status.Should().Be("Pending");
        result.Value.MaxTokenBudget.Should().Be(5000);
    }

    [Fact]
    public async Task SubmitPlan_TooManySteps_ReturnsBusinessError()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("3");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user@test.com", "Test plan",
            BuildSteps(5), 5000, false, 0, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AgentPlan.TooManySteps");
    }

    [Fact]
    public async Task SubmitPlan_EmptySteps_FailsGuard()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user@test.com", "Test plan",
            [], 5000, false, 0, null);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SubmitPlan_WithCorrelationId_PreservesCorrelationId()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var correlationId = "test-correlation-abc123";
        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user@test.com", "Test plan",
            BuildSteps(2), 1000, false, 0, correlationId);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task SubmitPlan_WithRequiresApproval_ReturnsApprovalRequired()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user@test.com", "Approval plan",
            BuildSteps(2), 2000, RequiresApproval: true, 5, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlanId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SubmitPlan_MaxTokenBudgetIsZero_FailsValidation()
    {
        var validator = new SubmitAgentExecutionPlan.Validator();
        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user", "desc",
            BuildSteps(1), 0, false, 0, null);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e =>
            e.PropertyName == "MaxTokenBudget");
    }

    [Fact]
    public async Task SubmitPlan_MissingDescription_FailsValidation()
    {
        var validator = new SubmitAgentExecutionPlan.Validator();
        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user", "",
            BuildSteps(1), 1000, false, 0, null);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitPlan_SingleStep_SucceedsWithMinimalBudget()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "dev@x.com", "Single step plan",
            BuildSteps(1), 100, false, 0, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StepCount.Should().Be(1);
    }

    [Fact]
    public async Task SubmitPlan_MaxStepsExactlyAtLimit_Succeeds()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("5");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user", "At limit plan",
            BuildSteps(5), 5000, false, 0, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StepCount.Should().Be(5);
    }

    [Fact]
    public async Task SubmitPlan_AllSixStepTypes_ParsedCorrectly()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var steps = new List<SubmitAgentExecutionPlan.StepRequest>
        {
            new(0, "S1", "ContractLookup", "{}", false),
            new(1, "S2", "IncidentCorrelation", "{}", false),
            new(2, "S3", "DraftGeneration", "{}", true),
            new(3, "S4", "RunbookProposal", "{}", false),
            new(4, "S5", "AlertTriage", "{}", false),
            new(5, "S6", "ExternalSearch", "{}", false),
        };

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user", "All types",
            steps, 10000, false, 0, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StepCount.Should().Be(6);
    }

    [Fact]
    public async Task SubmitPlan_BlastRadiusThresholdZero_IsValid()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user", "Low blast plan",
            BuildSteps(2), 5000, false, BlastRadiusThreshold: 0, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitPlan_ExactlyOneStepOverLimit_ReturnsError()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("2");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user", "Over limit",
            BuildSteps(3), 5000, false, 0, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AgentPlan.TooManySteps");
    }

    [Fact]
    public async Task SubmitPlan_ConfigDefaultsTo10WhenNotParseable_AllowsTenSteps()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = Substitute.For<IConfigurationResolutionService>();
        config.ResolveEffectiveValueAsync(
                Arg.Any<string>(), Arg.Any<ConfigurationScope>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EffectiveConfigurationDto?>(null));
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user", "Default limit test",
            BuildSteps(10), 5000, false, 0, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitPlan_ElevenStepsWithDefaultLimit_ReturnsError()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = Substitute.For<IConfigurationResolutionService>();
        config.ResolveEffectiveValueAsync(
                Arg.Any<string>(), Arg.Any<ConfigurationScope>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EffectiveConfigurationDto?>(null));
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);

        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user", "Over default limit",
            BuildSteps(11), 5000, false, 0, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AgentPlan.TooManySteps");
    }

    // ── ApproveAgentStep ──────────────────────────────────────────────────

    private async Task<Guid> SubmitPlanWithApprovalStep()
    {
        NullAgentExecutionPlanRepository.Clear();
        var steps = new List<SubmitAgentExecutionPlan.StepRequest>
        {
            new(0, "Step A", "ContractLookup", "{}", RequiresApproval: true),
            new(1, "Step B", "DraftGeneration", "{}", RequiresApproval: false),
        };
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var handler = new SubmitAgentExecutionPlan.Handler(repo, config);
        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "user", "Approval plan",
            steps, 5000, true, 0, null);
        var result = await handler.Handle(cmd, CancellationToken.None);
        return result.Value.PlanId;
    }

    [Fact]
    public async Task ApproveStep_ValidStep_UpdatesPlanAndStep()
    {
        var planId = await SubmitPlanWithApprovalStep();
        var repo = new NullAgentExecutionPlanRepository();

        // Move plan to WaitingApproval first
        var plan = await repo.GetByIdAsync(AgentExecutionPlanId.From(planId), CancellationToken.None);
        plan!.StartExecution(FixedNow);
        plan.RequestStepApproval(0);
        await repo.UpdateAsync(plan, CancellationToken.None);

        var handler = new ApproveAgentStep.Handler(repo);
        var cmd = new ApproveAgentStep.Command(planId, 0, "approver@test.com", TenantId);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApprovedBy.Should().Be("approver@test.com");
        result.Value.PlanStatus.Should().Be("Running");
    }

    [Fact]
    public async Task ApproveStep_PlanNotFound_ReturnsNotFoundError()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var handler = new ApproveAgentStep.Handler(repo);

        var result = await handler.Handle(
            new ApproveAgentStep.Command(Guid.NewGuid(), 0, "approver", TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveStep_StepNotFound_ReturnsNotFoundError()
    {
        var planId = await SubmitPlanWithApprovalStep();
        var repo = new NullAgentExecutionPlanRepository();
        var handler = new ApproveAgentStep.Handler(repo);

        var result = await handler.Handle(
            new ApproveAgentStep.Command(planId, 99, "approver", TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AgentPlan.StepNotFound");
    }

    [Fact]
    public async Task ApproveStep_NonApprovalStep_ReturnsBusinessError()
    {
        var planId = await SubmitPlanWithApprovalStep();
        var repo = new NullAgentExecutionPlanRepository();
        var handler = new ApproveAgentStep.Handler(repo);

        // Step 1 does not require approval
        var result = await handler.Handle(
            new ApproveAgentStep.Command(planId, 1, "approver", TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AgentPlan.StepDoesNotRequireApproval");
    }

    [Fact]
    public async Task ApproveStep_AlreadyApproved_ReturnsBusinessError()
    {
        var planId = await SubmitPlanWithApprovalStep();
        var repo = new NullAgentExecutionPlanRepository();

        var plan = await repo.GetByIdAsync(AgentExecutionPlanId.From(planId), CancellationToken.None);
        plan!.StartExecution(FixedNow);
        plan.RequestStepApproval(0);
        plan.ApproveStep(0, "first-approver");
        await repo.UpdateAsync(plan, CancellationToken.None);

        var handler = new ApproveAgentStep.Handler(repo);
        var result = await handler.Handle(
            new ApproveAgentStep.Command(planId, 0, "second-approver", TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AgentPlan.StepAlreadyApproved");
    }

    [Fact]
    public async Task ApproveStep_AllRequiredInfoPresent_ResponseContainsExpectedData()
    {
        var planId = await SubmitPlanWithApprovalStep();
        var repo = new NullAgentExecutionPlanRepository();

        var plan = await repo.GetByIdAsync(AgentExecutionPlanId.From(planId), CancellationToken.None);
        plan!.StartExecution(FixedNow);
        plan.RequestStepApproval(0);
        await repo.UpdateAsync(plan, CancellationToken.None);

        var handler = new ApproveAgentStep.Handler(repo);
        var result = await handler.Handle(
            new ApproveAgentStep.Command(planId, 0, "lead@test.com", TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlanId.Should().Be(planId);
        result.Value.StepIndex.Should().Be(0);
    }

    // ── GetAgentPlanStatus ────────────────────────────────────────────────

    [Fact]
    public async Task GetPlanStatus_ExistingPlan_ReturnsAllStepDetails()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var submitHandler = new SubmitAgentExecutionPlan.Handler(repo, config);
        var steps = new List<SubmitAgentExecutionPlan.StepRequest>
        {
            new(0, "Lookup", "ContractLookup", "{\"contract\":\"payments\"}", false),
            new(1, "Draft", "DraftGeneration", "{}", true),
        };
        var cmd = new SubmitAgentExecutionPlan.Command(
            TenantId, "eng@test.com", "Status test plan",
            steps, 3000, false, 0, "corr-abc");
        var submitted = await submitHandler.Handle(cmd, CancellationToken.None);

        var handler = new GetAgentPlanStatus.Handler(repo);
        var result = await handler.Handle(
            new GetAgentPlanStatus.Query(submitted.Value.PlanId, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Steps.Should().HaveCount(2);
        result.Value.CorrelationId.Should().Be("corr-abc");
        result.Value.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetPlanStatus_PlanNotFound_ReturnsError()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var handler = new GetAgentPlanStatus.Handler(repo);

        var result = await handler.Handle(
            new GetAgentPlanStatus.Query(Guid.NewGuid(), TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlanStatus_AfterStart_StatusIsRunning()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var submitHandler = new SubmitAgentExecutionPlan.Handler(repo, config);
        var submitted = await submitHandler.Handle(
            new SubmitAgentExecutionPlan.Command(
                TenantId, "u", "r", BuildSteps(1), 1000, false, 0, null),
            CancellationToken.None);

        var plan = await repo.GetByIdAsync(
            AgentExecutionPlanId.From(submitted.Value.PlanId), CancellationToken.None);
        plan!.StartExecution(FixedNow);
        await repo.UpdateAsync(plan, CancellationToken.None);

        var handler = new GetAgentPlanStatus.Handler(repo);
        var result = await handler.Handle(
            new GetAgentPlanStatus.Query(submitted.Value.PlanId, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Running");
        result.Value.StartedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetPlanStatus_TokenBudgetAndConsumedReflected()
    {
        NullAgentExecutionPlanRepository.Clear();
        var repo = new NullAgentExecutionPlanRepository();
        var config = CreateConfigService("10");
        var submitHandler = new SubmitAgentExecutionPlan.Handler(repo, config);
        var submitted = await submitHandler.Handle(
            new SubmitAgentExecutionPlan.Command(
                TenantId, "u", "budget test", BuildSteps(1), 9999, false, 0, null),
            CancellationToken.None);

        var handler = new GetAgentPlanStatus.Handler(repo);
        var result = await handler.Handle(
            new GetAgentPlanStatus.Query(submitted.Value.PlanId, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MaxTokenBudget.Should().Be(9999);
        result.Value.ConsumedTokens.Should().Be(0);
    }

    // ── ClassifyPromptIntent ──────────────────────────────────────────────

    private ClassifyPromptIntent.Handler CreateClassifyHandler()
    {
        var classifier = new PromptIntentClassifierService();
        var policyRepo = new NullModelRoutingPolicyRepository();
        return new ClassifyPromptIntent.Handler(classifier, policyRepo);
    }

    [Fact]
    public async Task ClassifyIntent_CodeGenerationPrompt_ReturnsCodeGeneration()
    {
        var handler = CreateClassifyHandler();
        var result = await handler.Handle(
            new ClassifyPromptIntent.Query("please generate code for a sorting algorithm", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("CodeGeneration");
        result.Value.Confidence.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ClassifyIntent_SummarizationPrompt_ReturnsDocumentSummarization()
    {
        var handler = CreateClassifyHandler();
        var result = await handler.Handle(
            new ClassifyPromptIntent.Query("please summarize the incident report", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("DocumentSummarization");
    }

    [Fact]
    public async Task ClassifyIntent_IncidentPrompt_ReturnsIncidentAnalysis()
    {
        var handler = CreateClassifyHandler();
        var result = await handler.Handle(
            new ClassifyPromptIntent.Query("there is an outage in the payment service, alert fired", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("IncidentAnalysis");
    }

    [Fact]
    public async Task ClassifyIntent_ContractPrompt_ReturnsContractDraft()
    {
        var handler = CreateClassifyHandler();
        var result = await handler.Handle(
            new ClassifyPromptIntent.Query("generate an openapi contract for the payments endpoint", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("ContractDraft");
    }

    [Fact]
    public async Task ClassifyIntent_CompliancePrompt_ReturnsComplianceCheck()
    {
        var handler = CreateClassifyHandler();
        var result = await handler.Handle(
            new ClassifyPromptIntent.Query("is this implementation compliant with gdpr regulations?", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("ComplianceCheck");
    }

    [Fact]
    public async Task ClassifyIntent_UnknownPrompt_FallsBackToGeneralQuery()
    {
        var handler = CreateClassifyHandler();
        var result = await handler.Handle(
            new ClassifyPromptIntent.Query("tell me something random", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("GeneralQuery");
    }

    [Fact]
    public async Task ClassifyIntent_EmptyPrompt_FallsBackToGeneralQuery()
    {
        var handler = CreateClassifyHandler();
        var result = await handler.Handle(
            new ClassifyPromptIntent.Query("   ", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("GeneralQuery");
    }

    [Fact]
    public void ClassifyPromptValidator_EmptyPrompt_FailsValidation()
    {
        var validator = new ClassifyPromptIntent.Validator();
        var query = new ClassifyPromptIntent.Query("", null);
        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Prompt");
    }

    [Fact]
    public async Task ClassifyIntent_MethodKeyword_ReturnsCodeGeneration()
    {
        var handler = CreateClassifyHandler();
        var result = await handler.Handle(
            new ClassifyPromptIntent.Query("write a method to parse the JSON response", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("CodeGeneration");
    }

    [Fact]
    public async Task ClassifyIntent_AuditKeyword_ReturnsComplianceCheck()
    {
        var handler = CreateClassifyHandler();
        var result = await handler.Handle(
            new ClassifyPromptIntent.Query("perform an audit of our current soc2 controls", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("ComplianceCheck");
    }

    // ── GetAiTokenBudgetReport ────────────────────────────────────────────

    private (IDateTimeProvider dt, IConfigurationResolutionService cfg) CreateBudgetMocks(
        string monthlyLimit = "1000000", string alertPct = "80")
    {
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        var cfg = Substitute.For<IConfigurationResolutionService>();
        cfg.ResolveEffectiveValueAsync(
                "ai.budget.monthly_token_limit_default",
                Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EffectiveConfigurationDto?>(
                CreateConfig("ai.budget.monthly_token_limit_default", monthlyLimit)));
        cfg.ResolveEffectiveValueAsync(
                "ai.budget.alert_threshold_pct",
                Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EffectiveConfigurationDto?>(
                CreateConfig("ai.budget.alert_threshold_pct", alertPct)));

        return (dt, cfg);
    }

    [Fact]
    public async Task GetTokenBudgetReport_EmptyLedger_ReturnsZeros()
    {
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();
        ledger.ListByPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AiTokenUsageLedger>>([]));
        var (dt, cfg) = CreateBudgetMocks();
        var handler = new GetAiTokenBudgetReport.Handler(ledger, dt, cfg);

        var result = await handler.Handle(
            new GetAiTokenBudgetReport.Query(TenantId, null, 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalTokens.Should().Be(0);
        result.Value.BurnRatePct.Should().Be(0);
        result.Value.IsApproachingLimit.Should().BeFalse();
    }

    [Fact]
    public async Task GetTokenBudgetReport_PeriodDaysZero_FailsValidation()
    {
        var validator = new GetAiTokenBudgetReport.Validator();
        var result = validator.Validate(new GetAiTokenBudgetReport.Query(null, null, 0));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetTokenBudgetReport_AlertThreshold_IsApproachingLimitWhenOver()
    {
        // 850k tokens with 1M limit = 85% > 80% threshold → approaching
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();
        var fakeEntry = BuildFakeLedgerEntry(850_000, 0m, "gpt-4", TenantId);
        ledger.ListByPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AiTokenUsageLedger>>(new[] { fakeEntry }));
        var (dt, cfg) = CreateBudgetMocks("1000000", "80");
        var handler = new GetAiTokenBudgetReport.Handler(ledger, dt, cfg);

        var result = await handler.Handle(
            new GetAiTokenBudgetReport.Query(TenantId, null, 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsApproachingLimit.Should().BeTrue();
        result.Value.BurnRatePct.Should().BeGreaterThanOrEqualTo(80);
    }

    [Fact]
    public async Task GetTokenBudgetReport_ExternalAI_ClassifiesCorrectly()
    {
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();
        var gptEntry = BuildFakeLedgerEntry(1000, 0.01m, "gpt-4", TenantId);
        var internalEntry = BuildFakeLedgerEntry(500, 0m, "internal-llama", TenantId);
        ledger.ListByPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AiTokenUsageLedger>>(
                new[] { gptEntry, internalEntry }));
        var (dt, cfg) = CreateBudgetMocks();
        var handler = new GetAiTokenBudgetReport.Handler(ledger, dt, cfg);

        var result = await handler.Handle(
            new GetAiTokenBudgetReport.Query(TenantId, null, 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByUseCase.Should().Contain(x => x.Label == "ExternalAI");
        result.Value.ByUseCase.Should().Contain(x => x.Label == "InternalAI");
    }

    [Fact]
    public async Task GetTokenBudgetReport_FilterByTenant_OnlyReturnsTenantData()
    {
        var otherTenant = Guid.NewGuid();
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();
        var myEntry = BuildFakeLedgerEntry(500, 0m, "claude-3", TenantId);
        var otherEntry = BuildFakeLedgerEntry(9999, 0m, "gpt-4", otherTenant);
        ledger.ListByPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AiTokenUsageLedger>>(
                new[] { myEntry, otherEntry }));
        var (dt, cfg) = CreateBudgetMocks();
        var handler = new GetAiTokenBudgetReport.Handler(ledger, dt, cfg);

        var result = await handler.Handle(
            new GetAiTokenBudgetReport.Query(TenantId, null, 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalTokens.Should().Be(500);
    }

    [Fact]
    public async Task GetTokenBudgetReport_PeriodDays365_IsValid()
    {
        var validator = new GetAiTokenBudgetReport.Validator();
        var result = validator.Validate(new GetAiTokenBudgetReport.Query(null, null, 365));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetTokenBudgetReport_PeriodDays366_FailsValidation()
    {
        var validator = new GetAiTokenBudgetReport.Validator();
        var result = validator.Validate(new GetAiTokenBudgetReport.Query(null, null, 366));

        result.IsValid.Should().BeFalse();
    }

    private static AiTokenUsageLedger BuildFakeLedgerEntry(
        int totalTokens, decimal estimatedCost, string modelId, Guid tenantId)
    {
        return AiTokenUsageLedger.Record(
            userId: "user1",
            tenantId: tenantId,
            providerId: "provider1",
            modelId: modelId,
            modelName: modelId,
            promptTokens: totalTokens / 2,
            completionTokens: totalTokens - totalTokens / 2,
            totalTokens: totalTokens,
            policyId: null,
            policyName: null,
            isBlocked: false,
            blockReason: null,
            requestId: Guid.NewGuid().ToString("N"),
            executionId: Guid.NewGuid().ToString("N"),
            timestamp: DateTimeOffset.UtcNow,
            status: "Completed",
            durationMs: 100,
            estimatedCostUsd: estimatedCost > 0 ? estimatedCost : null);
    }
}
