using System.Linq;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
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
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
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

    private static CreateAutomationWorkflow.Handler CreateAutomationWorkflowHandler()
    {
        var workflowRepo = Substitute.For<IAutomationWorkflowRepository>();
        var auditRepo = Substitute.For<IAutomationAuditRepository>();
        var unitOfWork = Substitute.For<IAutomationUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        return new CreateAutomationWorkflow.Handler(workflowRepo, auditRepo, unitOfWork, clock);
    }

    [Fact]
    public async Task CreateAutomationWorkflow_ValidData_ShouldReturnDraftStatus()
    {
        var handler = CreateAutomationWorkflowHandler();
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
        var handler = CreateAutomationWorkflowHandler();
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
        var workflowId = Guid.Parse("a0a10001-0001-0000-0000-000000000001");
        var typedId = new AutomationWorkflowRecordId(workflowId);
        var utcNow = DateTimeOffset.UtcNow;

        var workflow = AutomationWorkflowRecord.Create(
            actionId: "action-restart-controlled",
            serviceId: "svc-payment-gateway",
            incidentId: null,
            changeId: null,
            rationale: "Error rate exceeded threshold — controlled restart needed.",
            requestedBy: "ops-engineer@nextraceone.io",
            targetScope: "pod group A",
            targetEnvironment: "Production",
            riskLevel: RiskLevel.Medium,
            utcNow: utcNow);
        workflow.Approve("tech-lead@nextraceone.io", utcNow);
        workflow.UpdateStatus(AutomationWorkflowStatus.Executing, utcNow);

        var workflowRepo = Substitute.For<IAutomationWorkflowRepository>();
        workflowRepo.GetByIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns(workflow);

        var auditRepo = Substitute.For<IAutomationAuditRepository>();
        auditRepo.GetByWorkflowIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns(new List<AutomationAuditRecord>
            {
                AutomationAuditRecord.Create(
                    workflow.Id, AutomationAuditAction.WorkflowCreated,
                    "ops-engineer@nextraceone.io", "Workflow created.", utcNow)
            });

        var validationRepo = Substitute.For<IAutomationValidationRepository>();
        validationRepo.GetByWorkflowIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns((AutomationValidationRecord?)null);

        var handler = new GetAutomationWorkflow.Handler(workflowRepo, auditRepo, validationRepo);
        var query = new GetAutomationWorkflow.Query(workflow.Id.Value.ToString());

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
        result.Value.Preconditions.Should().HaveCount(0);
        result.Value.ExecutionSteps.Should().HaveCount(0);
        result.Value.AuditEntries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAutomationWorkflow_UnknownWorkflowId_ShouldReturnError()
    {
        var workflowRepo = Substitute.For<IAutomationWorkflowRepository>();
        workflowRepo.GetByIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns((AutomationWorkflowRecord?)null);
        var auditRepo = Substitute.For<IAutomationAuditRepository>();
        var validationRepo = Substitute.For<IAutomationValidationRepository>();

        var handler = new GetAutomationWorkflow.Handler(workflowRepo, auditRepo, validationRepo);
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
    public async Task ListAutomationWorkflows_NoFilters_ShouldReturnEmptyList()
    {
        var workflowRepo = Substitute.For<IAutomationWorkflowRepository>();
        workflowRepo.ListAsync(null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<NexTraceOne.OperationalIntelligence.Domain.Automation.Entities.AutomationWorkflowRecord>());
        workflowRepo.CountAsync(null, null, Arg.Any<CancellationToken>()).Returns(0);
        var handler = new ListAutomationWorkflows.Handler(workflowRepo);
        var query = new ListAutomationWorkflows.Query(null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
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

    private static (UpdateAutomationWorkflowAction.Handler handler, IAutomationWorkflowRepository workflowRepo) CreateUpdateActionHandler(AutomationWorkflowRecord? workflow = null)
    {
        var workflowRepo = Substitute.For<IAutomationWorkflowRepository>();
        workflowRepo.GetByIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns(workflow);
        var auditRepo = Substitute.For<IAutomationAuditRepository>();
        var unitOfWork = Substitute.For<IAutomationUnitOfWork>();
        unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        return (new UpdateAutomationWorkflowAction.Handler(workflowRepo, auditRepo, unitOfWork, clock), workflowRepo);
    }

    [Fact]
    public async Task UpdateAutomationWorkflowAction_ApproveAction_ShouldReturnApprovedStatus()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var workflow = AutomationWorkflowRecord.Create(
            actionId: "action-restart-controlled",
            serviceId: "svc-payment-gateway",
            incidentId: null, changeId: null,
            rationale: "Controlled restart needed.",
            requestedBy: "ops-engineer@nextraceone.io",
            targetScope: "pod group A",
            targetEnvironment: "Production",
            riskLevel: RiskLevel.Medium,
            utcNow: utcNow);
        workflow.UpdateStatus(AutomationWorkflowStatus.AwaitingApproval, utcNow);

        var (handler, _) = CreateUpdateActionHandler(workflow);
        var command = new UpdateAutomationWorkflowAction.Command(
            WorkflowId: workflow.Id.Value.ToString(),
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
        var utcNow = DateTimeOffset.UtcNow;
        var workflow = AutomationWorkflowRecord.Create(
            actionId: "action-restart-controlled",
            serviceId: "svc-payment-gateway",
            incidentId: null, changeId: null,
            rationale: "Controlled restart needed.",
            requestedBy: "ops-engineer@nextraceone.io",
            targetScope: "pod group A",
            targetEnvironment: "Production",
            riskLevel: RiskLevel.Medium,
            utcNow: utcNow);
        workflow.UpdateStatus(AutomationWorkflowStatus.AwaitingApproval, utcNow);

        var (handler, _) = CreateUpdateActionHandler(workflow);
        var command = new UpdateAutomationWorkflowAction.Command(
            WorkflowId: workflow.Id.Value.ToString(),
            Action: "reject",
            PerformedBy: "architect@nextraceone.io",
            Reason: "Risk too high — requires further analysis.",
            Notes: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NewStatus.Should().Be(AutomationWorkflowStatus.Failed);
        result.Value.ActionPerformed.Should().Be("reject");
    }

    [Fact]
    public async Task UpdateAutomationWorkflowAction_InvalidAction_ShouldReturnError()
    {
        var (handler, _) = CreateUpdateActionHandler();
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
    public async Task UpdateAutomationWorkflowAction_UnknownWorkflow_ValidAction_ShouldReturnNotFoundError()
    {
        var (handler, _) = CreateUpdateActionHandler(null);
        var command = new UpdateAutomationWorkflowAction.Command(
            WorkflowId: "nonexistent-workflow-id",
            Action: "approve",
            PerformedBy: "tech-lead@nextraceone.io",
            Reason: null,
            Notes: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
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
        var utcNow = DateTimeOffset.UtcNow;
        var workflow = AutomationWorkflowRecord.Create(
            actionId: "action-restart-controlled",
            serviceId: "svc-payment-gateway",
            incidentId: null, changeId: null,
            rationale: "Error rate exceeded threshold.",
            requestedBy: "ops-engineer@nextraceone.io",
            targetScope: "pod group A",
            targetEnvironment: "Production",
            riskLevel: RiskLevel.Medium,
            utcNow: utcNow);
        workflow.Approve("tech-lead@nextraceone.io", utcNow);

        var workflowRepo = Substitute.For<IAutomationWorkflowRepository>();
        workflowRepo.GetByIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns(workflow);
        var auditRepo = Substitute.For<IAutomationAuditRepository>();
        var unitOfWork = Substitute.For<IAutomationUnitOfWork>();
        unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(utcNow);

        var handler = new EvaluatePreconditions.Handler(workflowRepo, auditRepo, unitOfWork, clock);
        var command = new EvaluatePreconditions.Command(
            WorkflowId: workflow.Id.Value.ToString(),
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
    public async Task EvaluatePreconditions_UnknownWorkflowId_ShouldReturnNotFoundError()
    {
        var workflowRepo = Substitute.For<IAutomationWorkflowRepository>();
        workflowRepo.GetByIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns((AutomationWorkflowRecord?)null);
        var auditRepo = Substitute.For<IAutomationAuditRepository>();
        var unitOfWork = Substitute.For<IAutomationUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = new EvaluatePreconditions.Handler(workflowRepo, auditRepo, unitOfWork, clock);
        var command = new EvaluatePreconditions.Command(
            WorkflowId: "nonexistent-workflow-id",
            EvaluatedBy: "system");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
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
        var utcNow = DateTimeOffset.UtcNow;
        var workflow = AutomationWorkflowRecord.Create(
            actionId: "action-restart-controlled",
            serviceId: "svc-payment-gateway",
            incidentId: null, changeId: null,
            rationale: "Error rate exceeded threshold.",
            requestedBy: "ops-engineer@nextraceone.io",
            targetScope: "pod group A",
            targetEnvironment: "Production",
            riskLevel: RiskLevel.Medium,
            utcNow: utcNow);

        var workflowRepo = Substitute.For<IAutomationWorkflowRepository>();
        workflowRepo.GetByIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns(workflow);
        var validationRepo = Substitute.For<IAutomationValidationRepository>();
        var auditRepo = Substitute.For<IAutomationAuditRepository>();
        var unitOfWork = Substitute.For<IAutomationUnitOfWork>();
        unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(utcNow);

        var handler = new RecordAutomationValidation.Handler(workflowRepo, validationRepo, auditRepo, unitOfWork, clock);
        var command = new RecordAutomationValidation.Command(
            WorkflowId: workflow.Id.Value.ToString(),
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
        var utcNow = DateTimeOffset.UtcNow;
        var workflow = AutomationWorkflowRecord.Create(
            actionId: "action-restart-controlled",
            serviceId: "svc-payment-gateway",
            incidentId: null, changeId: null,
            rationale: "Error rate exceeded threshold.",
            requestedBy: "ops-engineer@nextraceone.io",
            targetScope: "pod group A",
            targetEnvironment: "Production",
            riskLevel: RiskLevel.Medium,
            utcNow: utcNow);

        var validation = AutomationValidationRecord.Create(
            workflowId: workflow.Id,
            outcome: AutomationOutcome.Inconclusive,
            validatedBy: "ops-engineer@nextraceone.io",
            notes: "Validation in progress.",
            observedOutcome: "Error rate stabilizing — need more data.",
            utcNow: utcNow);

        var workflowRepo = Substitute.For<IAutomationWorkflowRepository>();
        workflowRepo.GetByIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns(workflow);
        var validationRepo = Substitute.For<IAutomationValidationRepository>();
        validationRepo.GetByWorkflowIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns(validation);

        var handler = new GetAutomationValidation.Handler(workflowRepo, validationRepo);
        var query = new GetAutomationValidation.Query(workflow.Id.Value.ToString());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ValidationStatus.InProgress);
        result.Value.Checks.Should().HaveCount(0);
        result.Value.ObservedOutcome.Should().NotBeNullOrEmpty();
        result.Value.ValidatedBy.Should().Be("ops-engineer@nextraceone.io");
    }

    [Fact]
    public async Task GetAutomationValidation_UnknownWorkflowId_ShouldReturnError()
    {
        var workflowRepo = Substitute.For<IAutomationWorkflowRepository>();
        workflowRepo.GetByIdAsync(Arg.Any<AutomationWorkflowRecordId>(), Arg.Any<CancellationToken>())
            .Returns((AutomationWorkflowRecord?)null);
        var validationRepo = Substitute.For<IAutomationValidationRepository>();

        var handler = new GetAutomationValidation.Handler(workflowRepo, validationRepo);
        var query = new GetAutomationValidation.Query("nonexistent-workflow-id");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── GetAutomationAuditTrail ──────────────────────────────────────

    [Fact]
    public async Task GetAutomationAuditTrail_KnownWorkflowId_ShouldReturnAuditEntries()
    {
        var auditRepo = Substitute.For<IAutomationAuditRepository>();
        var workflowId = new AutomationWorkflowRecordId(Guid.Parse("b0a10001-0001-0000-0000-000000000001"));
        var records = new List<AutomationAuditRecord>
        {
            AutomationAuditRecord.Create(
                workflowId,
                AutomationAuditAction.WorkflowCreated,
                "ops-engineer@nextraceone.io",
                "Workflow created for controlled restart.",
                DateTimeOffset.UtcNow)
        };
        auditRepo.GetByWorkflowIdAsync(workflowId, Arg.Any<CancellationToken>())
            .Returns(records);
        var handler = new GetAutomationAuditTrail.Handler(auditRepo);
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
