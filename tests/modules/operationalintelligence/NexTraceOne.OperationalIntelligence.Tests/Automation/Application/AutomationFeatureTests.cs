using System.Linq;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.CreateAutomationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.EvaluatePreconditions;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationAction;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationAuditTrail;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationValidation;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.ListAutomationActions;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.ListAutomationWorkflows;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.RecordAutomationValidation;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.UpdateAutomationWorkflowAction;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Automation.Application;

/// <summary>
/// Testes unitários para as features de Automation Workflows do subdomínio Automation.
/// Verificam handlers, validators e respostas de todas as queries e commands de automação operacional.
/// </summary>
public sealed class AutomationFeatureTests
{
    // ── ListAutomationActions ────────────────────────────────────────

    [Fact]
    public async Task ListAutomationActions_NoFilter_ShouldReturnAll8Actions()
    {
        var handler = new ListAutomationActions.Handler();
        var query = new ListAutomationActions.Query(null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(8);
        result.Value.Items.Select(a => a.ActionId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task ListAutomationActions_FilterBySearchTerm_ShouldReturnMatchingActions()
    {
        var handler = new ListAutomationActions.Handler();
        var query = new ListAutomationActions.Query("Restart");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.Items.Should().AllSatisfy(a =>
            (a.Name + a.DisplayName + a.Description)
                .Contains("Restart", StringComparison.OrdinalIgnoreCase)
                .Should().BeTrue());
    }

    // ── GetAutomationAction ──────────────────────────────────────────

    [Fact]
    public async Task GetAutomationAction_KnownActionId_ShouldReturnActionDetail()
    {
        var handler = new GetAutomationAction.Handler();
        var query = new GetAutomationAction.Query("action-restart-controlled");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActionId.Should().Be("action-restart-controlled");
        result.Value.DisplayName.Should().Be("Controlled Service Restart");
        result.Value.ActionType.Should().Be(AutomationActionType.RestartControlled);
        result.Value.RiskLevel.Should().Be(RiskLevel.Medium);
        result.Value.RequiresApproval.Should().BeTrue();
        result.Value.AllowedPersonas.Should().NotBeEmpty();
        result.Value.AllowedEnvironments.Should().NotBeEmpty();
        result.Value.PreconditionTypes.Should().NotBeEmpty();
        result.Value.HasPostValidation.Should().BeTrue();
    }

    [Fact]
    public async Task GetAutomationAction_UnknownActionId_ShouldReturnError()
    {
        var handler = new GetAutomationAction.Handler();
        var query = new GetAutomationAction.Query("nonexistent-action-id");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void GetAutomationAction_Validator_ShouldRejectEmptyActionId()
    {
        var validator = new GetAutomationAction.Validator();
        var query = new GetAutomationAction.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── CreateAutomationWorkflow ─────────────────────────────────────

    [Fact]
    public async Task CreateAutomationWorkflow_ValidData_ShouldReturnDraftStatus()
    {
        var handler = new CreateAutomationWorkflow.Handler();
        var command = new CreateAutomationWorkflow.Command(
            ActionId: "action-restart-controlled",
            ServiceId: "svc-payment-gateway",
            IncidentId: null,
            ChangeId: null,
            Rationale: "Error rate exceeded threshold — controlled restart needed.",
            RequestedBy: "ops-engineer@nextraceone.io",
            TargetScope: "pod group A",
            TargetEnvironment: "Production");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(AutomationWorkflowStatus.Draft);
        result.Value.WorkflowId.Should().NotBeEmpty();
        result.Value.ActionId.Should().Be("action-restart-controlled");
    }

    [Fact]
    public async Task CreateAutomationWorkflow_UnknownActionId_ShouldReturnError()
    {
        var handler = new CreateAutomationWorkflow.Handler();
        var command = new CreateAutomationWorkflow.Command(
            ActionId: "nonexistent-action",
            ServiceId: null,
            IncidentId: null,
            ChangeId: null,
            Rationale: "Test rationale",
            RequestedBy: "user@nextraceone.io",
            TargetScope: null,
            TargetEnvironment: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void CreateAutomationWorkflow_Validator_ShouldRejectEmptyActionId()
    {
        var validator = new CreateAutomationWorkflow.Validator();
        var command = new CreateAutomationWorkflow.Command(
            "", null, null, null, "Rationale", "user@nextraceone.io", null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateAutomationWorkflow_Validator_ShouldRejectEmptyRationale()
    {
        var validator = new CreateAutomationWorkflow.Validator();
        var command = new CreateAutomationWorkflow.Command(
            "action-restart-controlled", null, null, null, "", "user@nextraceone.io", null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    // ── GetAutomationWorkflow ────────────────────────────────────────

    [Fact]
    public async Task GetAutomationWorkflow_KnownWorkflowId_ShouldReturnWorkflowDetail()
    {
        var handler = new GetAutomationWorkflow.Handler();
        var query = new GetAutomationWorkflow.Query("aw-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActionId.Should().Be("action-restart-controlled");
        result.Value.ActionDisplayName.Should().Be("Controlled Service Restart");
        result.Value.Status.Should().Be(AutomationWorkflowStatus.Executing);
        result.Value.RiskLevel.Should().Be(RiskLevel.Medium);
        result.Value.Rationale.Should().NotBeNullOrEmpty();
        result.Value.RequestedBy.Should().Be("ops-engineer@nextraceone.io");
        result.Value.ApproverInfo.Should().NotBeNull();
        result.Value.ApproverInfo!.ApprovedBy.Should().Be("tech-lead@nextraceone.io");
        result.Value.ApproverInfo.ApprovalStatus.Should().Be(AutomationApprovalStatus.Approved);
        result.Value.Preconditions.Should().HaveCount(3);
        result.Value.ExecutionSteps.Should().HaveCount(4);
        result.Value.ExecutionSteps[0].Status.Should().Be("Completed");
        result.Value.ExecutionSteps[2].Status.Should().Be("InProgress");
        result.Value.AuditEntries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAutomationWorkflow_UnknownWorkflowId_ShouldReturnError()
    {
        var handler = new GetAutomationWorkflow.Handler();
        var query = new GetAutomationWorkflow.Query("nonexistent-workflow-id");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void GetAutomationWorkflow_Validator_ShouldRejectEmptyWorkflowId()
    {
        var validator = new GetAutomationWorkflow.Validator();
        var query = new GetAutomationWorkflow.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── ListAutomationWorkflows ──────────────────────────────────────

    [Fact]
    public async Task ListAutomationWorkflows_NoFilters_ShouldReturnPreviewOnlyError()
    {
        var handler = new ListAutomationWorkflows.Handler();
        var query = new ListAutomationWorkflows.Query(null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PreviewOnly");
    }

    [Fact]
    public void ListAutomationWorkflows_Validator_ShouldRejectInvalidPageSize_Zero()
    {
        var validator = new ListAutomationWorkflows.Validator();
        var query = new ListAutomationWorkflows.Query(null, null, Page: 1, PageSize: 0);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListAutomationWorkflows_Validator_ShouldRejectInvalidPageSize_Above100()
    {
        var validator = new ListAutomationWorkflows.Validator();
        var query = new ListAutomationWorkflows.Query(null, null, Page: 1, PageSize: 101);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── UpdateAutomationWorkflowAction ───────────────────────────────

    [Fact]
    public async Task UpdateAutomationWorkflowAction_ApproveAction_ShouldReturnApprovedStatus()
    {
        var handler = new UpdateAutomationWorkflowAction.Handler();
        var command = new UpdateAutomationWorkflowAction.Command(
            WorkflowId: "b0a10001-0001-0000-0000-000000000001",
            Action: "approve",
            PerformedBy: "tech-lead@nextraceone.io",
            Reason: "Low blast radius, safe to proceed.",
            Notes: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NewStatus.Should().Be(AutomationWorkflowStatus.Approved);
        result.Value.ActionPerformed.Should().Be("approve");
    }

    [Fact]
    public async Task UpdateAutomationWorkflowAction_RejectAction_ShouldReturnRejectedStatus()
    {
        var handler = new UpdateAutomationWorkflowAction.Handler();
        var command = new UpdateAutomationWorkflowAction.Command(
            WorkflowId: "b0a10001-0001-0000-0000-000000000001",
            Action: "reject",
            PerformedBy: "architect@nextraceone.io",
            Reason: "Risk too high — requires further analysis.",
            Notes: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NewStatus.Should().Be(AutomationWorkflowStatus.Rejected);
        result.Value.ActionPerformed.Should().Be("reject");
    }

    [Fact]
    public async Task UpdateAutomationWorkflowAction_InvalidAction_ShouldReturnError()
    {
        var handler = new UpdateAutomationWorkflowAction.Handler();
        var command = new UpdateAutomationWorkflowAction.Command(
            WorkflowId: "b0a10001-0001-0000-0000-000000000001",
            Action: "invalid-action",
            PerformedBy: "user@nextraceone.io",
            Reason: null,
            Notes: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidAction");
    }

    [Fact]
    public async Task UpdateAutomationWorkflowAction_UnknownWorkflow_ValidAction_ShouldStillProcessAction()
    {
        var handler = new UpdateAutomationWorkflowAction.Handler();
        var command = new UpdateAutomationWorkflowAction.Command(
            WorkflowId: "nonexistent-workflow-id",
            Action: "approve",
            PerformedBy: "tech-lead@nextraceone.io",
            Reason: null,
            Notes: null);

        var result = await handler.Handle(command, CancellationToken.None);

        // Handler currently validates action type only; workflow existence check is deferred to integration layer.
        result.IsSuccess.Should().BeTrue();
        result.Value.NewStatus.Should().Be(AutomationWorkflowStatus.Approved);
    }

    [Fact]
    public void UpdateAutomationWorkflowAction_Validator_ShouldRejectEmptyWorkflowId()
    {
        var validator = new UpdateAutomationWorkflowAction.Validator();
        var command = new UpdateAutomationWorkflowAction.Command("", "approve", "user", null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateAutomationWorkflowAction_Validator_ShouldRejectEmptyAction()
    {
        var validator = new UpdateAutomationWorkflowAction.Validator();
        var command = new UpdateAutomationWorkflowAction.Command("wf-001", "", "user", null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    // ── EvaluatePreconditions ────────────────────────────────────────

    [Fact]
    public async Task EvaluatePreconditions_KnownWorkflowId_ShouldReturnEvaluatedPreconditions()
    {
        var handler = new EvaluatePreconditions.Handler();
        var command = new EvaluatePreconditions.Command(
            WorkflowId: "b0a10001-0001-0000-0000-000000000001",
            EvaluatedBy: "system");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllPassed.Should().BeTrue();
        result.Value.Results.Should().HaveCount(5);
        result.Value.Results.Should().AllSatisfy(r => r.Passed.Should().BeTrue());
        result.Value.Results.Select(r => r.Type).Should().Contain(PreconditionType.ServiceHealthCheck);
        result.Value.Results.Select(r => r.Type).Should().Contain(PreconditionType.ApprovalPresence);
        result.Value.Results.Select(r => r.Type).Should().Contain(PreconditionType.BlastRadiusConstraint);
    }

    [Fact]
    public async Task EvaluatePreconditions_UnknownWorkflowId_ShouldStillReturnPreconditions()
    {
        var handler = new EvaluatePreconditions.Handler();
        var command = new EvaluatePreconditions.Command(
            WorkflowId: "nonexistent-workflow-id",
            EvaluatedBy: "system");

        var result = await handler.Handle(command, CancellationToken.None);

        // Handler currently returns simulated preconditions for any workflow; existence check deferred to integration layer.
        result.IsSuccess.Should().BeTrue();
        result.Value.Results.Should().NotBeEmpty();
    }

    [Fact]
    public void EvaluatePreconditions_Validator_ShouldRejectEmptyWorkflowId()
    {
        var validator = new EvaluatePreconditions.Validator();
        var command = new EvaluatePreconditions.Command("", "system");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    // ── RecordAutomationValidation ───────────────────────────────────

    [Fact]
    public async Task RecordAutomationValidation_ValidInputs_ShouldRecordSuccessfully()
    {
        var handler = new RecordAutomationValidation.Handler();
        var command = new RecordAutomationValidation.Command(
            WorkflowId: "b0a10001-0001-0000-0000-000000000001",
            Status: ValidationStatus.Passed,
            ObservedOutcome: "Error rate dropped to 0.4% — within threshold.",
            ValidatedBy: "ops-engineer@nextraceone.io",
            Checks: new[]
            {
                new RecordAutomationValidation.ValidationCheckInput("Error rate check", true, "0.4%"),
                new RecordAutomationValidation.ValidationCheckInput("Response time check", true, "P95 at 120ms"),
            });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ValidationStatus.Should().Be(ValidationStatus.Passed);
        result.Value.WorkflowId.Should().NotBeEmpty();
    }

    [Fact]
    public void RecordAutomationValidation_Validator_ShouldRejectEmptyWorkflowId()
    {
        var validator = new RecordAutomationValidation.Validator();
        var command = new RecordAutomationValidation.Command(
            "", ValidationStatus.Passed, null, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    // ── GetAutomationValidation ──────────────────────────────────────

    [Fact]
    public async Task GetAutomationValidation_KnownWorkflowId_ShouldReturnValidationData()
    {
        var handler = new GetAutomationValidation.Handler();
        var query = new GetAutomationValidation.Query("aw-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ValidationStatus.InProgress);
        result.Value.Checks.Should().HaveCount(4);
        result.Value.Checks[0].IsPassed.Should().BeTrue();
        result.Value.Checks[3].IsPassed.Should().BeFalse();
        result.Value.ObservedOutcome.Should().NotBeNullOrEmpty();
        result.Value.ValidatedBy.Should().Be("ops-engineer@nextraceone.io");
    }

    [Fact]
    public async Task GetAutomationValidation_UnknownWorkflowId_ShouldReturnError()
    {
        var handler = new GetAutomationValidation.Handler();
        var query = new GetAutomationValidation.Query("nonexistent-workflow-id");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── GetAutomationAuditTrail ──────────────────────────────────────

    [Fact]
    public async Task GetAutomationAuditTrail_KnownWorkflowId_ShouldReturnAuditEntries()
    {
        var handler = new GetAutomationAuditTrail.Handler();
        var query = new GetAutomationAuditTrail.Query(
            WorkflowId: "b0a10001-0001-0000-0000-000000000001",
            ServiceId: null,
            TeamId: null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().NotBeEmpty();
        result.Value.Entries.Should().AllSatisfy(e =>
            e.WorkflowId.Should().Be(Guid.Parse("b0a10001-0001-0000-0000-000000000001")));
        result.Value.Entries[0].Action.Should().Be(AutomationAuditAction.WorkflowCreated);
        result.Value.Entries[0].PerformedBy.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetAutomationAuditTrail_Validator_ShouldRejectWhenAllFiltersAreNullOrEmpty()
    {
        var validator = new GetAutomationAuditTrail.Validator();
        var query = new GetAutomationAuditTrail.Query(null, null, null);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
