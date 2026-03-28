using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendations;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RecordMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateMitigationWorkflowAction;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para as features de Mitigation Workflows do subdomínio Incidents.
/// Verificam handlers, validators e respostas de todas as queries e commands de mitigação.
/// </summary>
public sealed class MitigationFeatureTests
{
    private readonly InMemoryIncidentStore _store = new();

    // ── GetMitigationRecommendations ─────────────────────────────────

    [Fact]
    public async Task GetMitigationRecommendations_KnownIncident_ShouldReturnRecommendations()
    {
        var handler = new GetMitigationRecommendations.Handler(_store);
        var query = new GetMitigationRecommendations.Query("a1b2c3d4-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recommendations.Should().HaveCount(3);
        result.Value.Recommendations[0].Title.Should().Be("Rollback deployment to v2.13.2");
        result.Value.Recommendations[0].RecommendedActionType.Should().Be(MitigationActionType.RollbackCandidate);
        result.Value.Recommendations[0].RiskLevel.Should().Be(RiskLevel.Medium);
        result.Value.Recommendations[0].RequiresApproval.Should().BeTrue();
        result.Value.Recommendations[0].LinkedRunbookIds.Should().NotBeEmpty();
        result.Value.Recommendations[0].SuggestedValidationSteps.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMitigationRecommendations_UnknownIncident_ShouldReturnError()
    {
        var handler = new GetMitigationRecommendations.Handler(_store);
        var query = new GetMitigationRecommendations.Query("nonexistent-incident-id");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void GetMitigationRecommendations_Validator_ShouldRejectEmptyIncidentId()
    {
        var validator = new GetMitigationRecommendations.Validator();
        var query = new GetMitigationRecommendations.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── GetMitigationWorkflow ────────────────────────────────────────

    [Fact]
    public async Task GetMitigationWorkflow_KnownIncidentAndWorkflow_ShouldReturnWorkflowDetail()
    {
        var handler = new GetMitigationWorkflow.Handler(_store);
        var query = new GetMitigationWorkflow.Query("a1b2c3d4-0001-0000-0000-000000000001", "wf-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Rollback payment-service to v2.13.2");
        result.Value.Status.Should().Be(MitigationWorkflowStatus.InProgress);
        result.Value.ActionType.Should().Be(MitigationActionType.RollbackCandidate);
        result.Value.RiskLevel.Should().Be(RiskLevel.Medium);
        result.Value.RequiresApproval.Should().BeTrue();
        result.Value.ApprovedBy.Should().Be("tech-lead@nextraceone.io");
        result.Value.Steps.Should().HaveCount(4);
        result.Value.Steps[0].IsCompleted.Should().BeTrue();
        result.Value.Steps[1].IsCompleted.Should().BeTrue();
        result.Value.Steps[2].IsCompleted.Should().BeFalse();
        result.Value.Decisions.Should().HaveCount(1);
        result.Value.Decisions[0].DecisionType.Should().Be(MitigationDecisionType.Approved);
    }

    [Fact]
    public async Task GetMitigationWorkflow_UnknownIncident_ShouldReturnError()
    {
        var handler = new GetMitigationWorkflow.Handler(_store);
        var query = new GetMitigationWorkflow.Query("nonexistent", "wf-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void GetMitigationWorkflow_Validator_ShouldRejectEmptyIds()
    {
        var validator = new GetMitigationWorkflow.Validator();

        var emptyIncidentId = validator.Validate(new GetMitigationWorkflow.Query("", "wf-001"));
        var emptyWorkflowId = validator.Validate(new GetMitigationWorkflow.Query("inc-001", ""));

        emptyIncidentId.IsValid.Should().BeFalse();
        emptyWorkflowId.IsValid.Should().BeFalse();
    }

    // ── CreateMitigationWorkflow ─────────────────────────────────────

    [Fact]
    public async Task CreateMitigationWorkflow_ValidInputs_ShouldReturnDraftWorkflow()
    {
        var workflowRepo = Substitute.For<IMitigationWorkflowRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("test-user@nextraceone.io");
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = new CreateMitigationWorkflow.Handler(_store, workflowRepo, currentUser, clock);
        var command = new CreateMitigationWorkflow.Command(
            IncidentId: "a1b2c3d4-0001-0000-0000-000000000001",
            Title: "Test workflow",
            ActionType: MitigationActionType.RollbackCandidate,
            RiskLevel: RiskLevel.Medium,
            RequiresApproval: true,
            LinkedRunbookId: null,
            Steps: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(MitigationWorkflowStatus.Draft);
        result.Value.WorkflowId.Should().NotBeEmpty();
        await workflowRepo.Received(1).AddAsync(Arg.Any<MitigationWorkflowRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMitigationWorkflow_UnknownIncident_ShouldReturnError()
    {
        var workflowRepo = Substitute.For<IMitigationWorkflowRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("test-user@nextraceone.io");
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = new CreateMitigationWorkflow.Handler(_store, workflowRepo, currentUser, clock);
        var command = new CreateMitigationWorkflow.Command(
            IncidentId: "nonexistent-incident-id",
            Title: "Test workflow",
            ActionType: MitigationActionType.Investigate,
            RiskLevel: RiskLevel.Low,
            RequiresApproval: false,
            LinkedRunbookId: null,
            Steps: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
        await workflowRepo.DidNotReceive().AddAsync(Arg.Any<MitigationWorkflowRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateMitigationWorkflow_Validator_ShouldRejectEmptyIncidentId()
    {
        var validator = new CreateMitigationWorkflow.Validator();
        var command = new CreateMitigationWorkflow.Command(
            "", "Title", MitigationActionType.Investigate, RiskLevel.Low, false, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateMitigationWorkflow_Validator_ShouldRejectEmptyTitle()
    {
        var validator = new CreateMitigationWorkflow.Validator();
        var command = new CreateMitigationWorkflow.Command(
            "inc-001", "", MitigationActionType.Investigate, RiskLevel.Low, false, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    // ── UpdateMitigationWorkflowAction ───────────────────────────────

    [Fact]
    public async Task UpdateMitigationWorkflowAction_ValidApproveAction_ShouldReturnApprovedStatus()
    {
        var handler = new UpdateMitigationWorkflowAction.Handler(_store);
        var command = new UpdateMitigationWorkflowAction.Command(
            IncidentId: "a1b2c3d4-0001-0000-0000-000000000001",
            WorkflowId: "wf-0001-0000-0000-000000000001",
            Action: "approve",
            PerformedBy: "tech-lead@nextraceone.io",
            Reason: "Correlation evidence is strong",
            Notes: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NewStatus.Should().Be(MitigationWorkflowStatus.Approved);
        result.Value.ActionPerformed.Should().Be("approve");
    }

    [Fact]
    public async Task UpdateMitigationWorkflowAction_InvalidAction_ShouldReturnError()
    {
        var handler = new UpdateMitigationWorkflowAction.Handler(_store);
        var command = new UpdateMitigationWorkflowAction.Command(
            IncidentId: "a1b2c3d4-0001-0000-0000-000000000001",
            WorkflowId: "wf-0001-0000-0000-000000000001",
            Action: "invalid-action",
            PerformedBy: "user",
            Reason: null,
            Notes: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateMitigationWorkflowAction_Validator_ShouldRejectEmptyFields()
    {
        var validator = new UpdateMitigationWorkflowAction.Validator();

        var emptyIncidentId = validator.Validate(new UpdateMitigationWorkflowAction.Command("", "wf-001", "approve", null, null, null));
        var emptyWorkflowId = validator.Validate(new UpdateMitigationWorkflowAction.Command("inc-001", "", "approve", null, null, null));
        var emptyAction = validator.Validate(new UpdateMitigationWorkflowAction.Command("inc-001", "wf-001", "", null, null, null));

        emptyIncidentId.IsValid.Should().BeFalse();
        emptyWorkflowId.IsValid.Should().BeFalse();
        emptyAction.IsValid.Should().BeFalse();
    }

    // ── GetMitigationHistory ─────────────────────────────────────────

    [Fact]
    public async Task GetMitigationHistory_KnownIncident_ShouldReturnWorkflowEntries()
    {
        var workflowRepo = Substitute.For<IMitigationWorkflowRepository>();
        var knownIncidentId = "a1b2c3d4-0001-0000-0000-000000000001";
        var now = DateTimeOffset.UtcNow;
        var workflow = MitigationWorkflowRecord.Create(
            MitigationWorkflowRecordId.New(), knownIncidentId, "Rollback",
            MitigationWorkflowStatus.Draft, MitigationActionType.RollbackCandidate,
            RiskLevel.Medium, false, "test-user@nextraceone.io");
        workflowRepo.GetByIncidentIdAsync(knownIncidentId, Arg.Any<CancellationToken>())
            .Returns([workflow]);

        var handler = new GetMitigationHistory.Handler(_store, workflowRepo);
        var query = new GetMitigationHistory.Query(knownIncidentId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be(Guid.Parse(knownIncidentId));
        result.Value.Entries.Should().HaveCount(1);
        result.Value.Entries[0].Action.Should().Be(MitigationActionType.RollbackCandidate.ToString());
        result.Value.Entries[0].PerformedBy.Should().Be("test-user@nextraceone.io");
    }

    [Fact]
    public async Task GetMitigationHistory_UnknownIncident_ShouldReturnError()
    {
        var workflowRepo = Substitute.For<IMitigationWorkflowRepository>();
        var handler = new GetMitigationHistory.Handler(_store, workflowRepo);
        var query = new GetMitigationHistory.Query("nonexistent");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
        await workflowRepo.DidNotReceive()
            .GetByIncidentIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetMitigationHistory_Validator_ShouldRejectEmptyIncidentId()
    {
        var validator = new GetMitigationHistory.Validator();
        var query = new GetMitigationHistory.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── GetMitigationValidation ──────────────────────────────────────

    [Fact]
    public async Task GetMitigationValidation_KnownWorkflow_ShouldReturnValidationData()
    {
        var handler = new GetMitigationValidation.Handler(_store);
        var query = new GetMitigationValidation.Query("a1b2c3d4-0001-0000-0000-000000000001", "wf-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ValidationStatus.InProgress);
        result.Value.ExpectedChecks.Should().HaveCount(4);
        result.Value.ExpectedChecks[0].IsPassed.Should().BeTrue();
        result.Value.ExpectedChecks[3].IsPassed.Should().BeFalse();
        result.Value.ObservedOutcome.Should().NotBeNullOrEmpty();
        result.Value.PostMitigationSignalsSummary.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMitigationValidation_UnknownWorkflow_ShouldReturnError()
    {
        var handler = new GetMitigationValidation.Handler(_store);
        var query = new GetMitigationValidation.Query("nonexistent", "wf-nonexistent");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── RecordMitigationValidation ───────────────────────────────────

    [Fact]
    public async Task RecordMitigationValidation_ValidInputs_ShouldRecordSuccessfully()
    {
        var validationRepo = Substitute.For<IMitigationValidationRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = new RecordMitigationValidation.Handler(_store, validationRepo, clock);
        var command = new RecordMitigationValidation.Command(
            IncidentId: "a1b2c3d4-0001-0000-0000-000000000001",
            WorkflowId: "00000001-0001-0000-0000-000000000001",
            Status: ValidationStatus.Passed,
            ObservedOutcome: "All checks passed",
            ValidatedBy: "ops-engineer@nextraceone.io",
            Checks: new[]
            {
                new RecordMitigationValidation.ValidationCheckInput("Error rate check", true, "0.2%"),
            });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ValidationStatus.Passed);
        await validationRepo.Received(1).AddAsync(Arg.Any<MitigationValidationLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordMitigationValidation_UnknownIncident_ShouldReturnError()
    {
        var validationRepo = Substitute.For<IMitigationValidationRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = new RecordMitigationValidation.Handler(_store, validationRepo, clock);
        var command = new RecordMitigationValidation.Command(
            IncidentId: "nonexistent",
            WorkflowId: "wf-001",
            Status: ValidationStatus.Passed,
            ObservedOutcome: null,
            ValidatedBy: null,
            Checks: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
        await validationRepo.DidNotReceive().AddAsync(Arg.Any<MitigationValidationLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RecordMitigationValidation_Validator_ShouldRejectEmptyFields()
    {
        var validator = new RecordMitigationValidation.Validator();

        var emptyIncidentId = validator.Validate(new RecordMitigationValidation.Command(
            "", "wf-001", ValidationStatus.Passed, null, null, null));
        var emptyWorkflowId = validator.Validate(new RecordMitigationValidation.Command(
            "inc-001", "", ValidationStatus.Passed, null, null, null));

        emptyIncidentId.IsValid.Should().BeFalse();
        emptyWorkflowId.IsValid.Should().BeFalse();
    }
}
