using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RecordMitigationValidation;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Tests.Incidents.Infrastructure;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes de ciclo de vida completo para os handlers de mitigação refatorados.
/// Verificam: persistência via IMitigationWorkflowRepository, IMitigationValidationRepository,
/// leitura de histórico via repositório, e comportamento correto de guard clauses.
/// </summary>
public sealed class MitigationLifecycleTests
{
    private readonly InMemoryIncidentStore _store = new();
    private static readonly string KnownIncidentId = "a1b2c3d4-0001-0000-0000-000000000001";
    private static readonly Guid KnownWorkflowGuid = Guid.Parse("bb000001-0000-0000-0000-000000000001");

    // ── CreateMitigationWorkflow ─────────────────────────────────────

    [Fact]
    public async Task CreateMitigationWorkflow_Persist_CallsRepository_WithCorrectEntity()
    {
        var workflowRepo = Substitute.For<IMitigationWorkflowRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("engineer@nextraceone.io");
        var clock = Substitute.For<IDateTimeProvider>();
        var fixedNow = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
        clock.UtcNow.Returns(fixedNow);

        MitigationWorkflowRecord? captured = null;
        await workflowRepo.AddAsync(
            Arg.Do<MitigationWorkflowRecord>(r => captured = r),
            Arg.Any<CancellationToken>());

        var handler = new CreateMitigationWorkflow.Handler(_store, workflowRepo, currentUser, clock);
        var command = new CreateMitigationWorkflow.Command(
            IncidentId: KnownIncidentId,
            Title: "Rollback to v2.13.2",
            ActionType: MitigationActionType.RollbackCandidate,
            RiskLevel: RiskLevel.High,
            RequiresApproval: true,
            LinkedRunbookId: Guid.NewGuid(),
            Steps: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(MitigationWorkflowStatus.Draft);
        captured.Should().NotBeNull();
        captured!.IncidentId.Should().Be(KnownIncidentId);
        captured.Title.Should().Be("Rollback to v2.13.2");
        captured.ActionType.Should().Be(MitigationActionType.RollbackCandidate);
        captured.RiskLevel.Should().Be(RiskLevel.High);
        captured.RequiresApproval.Should().BeTrue();
        captured.CreatedByUser.Should().Be("engineer@nextraceone.io");
        captured.LinkedRunbookId.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMitigationWorkflow_WithSteps_SerializesStepsJson()
    {
        var workflowRepo = Substitute.For<IMitigationWorkflowRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("engineer@nextraceone.io");
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        MitigationWorkflowRecord? captured = null;
        await workflowRepo.AddAsync(
            Arg.Do<MitigationWorkflowRecord>(r => captured = r),
            Arg.Any<CancellationToken>());

        var handler = new CreateMitigationWorkflow.Handler(_store, workflowRepo, currentUser, clock);
        var command = new CreateMitigationWorkflow.Command(
            IncidentId: KnownIncidentId,
            Title: "Rollback",
            ActionType: MitigationActionType.RollbackCandidate,
            RiskLevel: RiskLevel.Medium,
            RequiresApproval: false,
            LinkedRunbookId: null,
            Steps:
            [
                new CreateMitigationWorkflow.CreateStepDto(1, "Notify team", "Notify on-call"),
                new CreateMitigationWorkflow.CreateStepDto(2, "Execute rollback", null),
            ]);

        await handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.StepsJson.Should().NotBeNullOrEmpty();
        captured.StepsJson.Should().Contain("Notify team");
        captured.StepsJson.Should().Contain("Execute rollback");
    }

    [Fact]
    public async Task CreateMitigationWorkflow_IncidentNotFound_DoesNotCallRepository()
    {
        var workflowRepo = Substitute.For<IMitigationWorkflowRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("engineer@nextraceone.io");
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = new CreateMitigationWorkflow.Handler(_store, workflowRepo, currentUser, clock);
        var command = new CreateMitigationWorkflow.Command(
            IncidentId: Guid.NewGuid().ToString(),
            Title: "Rollback",
            ActionType: MitigationActionType.RollbackCandidate,
            RiskLevel: RiskLevel.Medium,
            RequiresApproval: false,
            LinkedRunbookId: null,
            Steps: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await workflowRepo.DidNotReceive().AddAsync(Arg.Any<MitigationWorkflowRecord>(), Arg.Any<CancellationToken>());
    }

    // ── RecordMitigationValidation ───────────────────────────────────

    [Fact]
    public async Task RecordMitigationValidation_Persist_CallsRepository_WithCorrectEntity()
    {
        var validationRepo = Substitute.For<IMitigationValidationRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        var fixedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        clock.UtcNow.Returns(fixedAt);

        MitigationValidationLog? captured = null;
        await validationRepo.AddAsync(
            Arg.Do<MitigationValidationLog>(l => captured = l),
            Arg.Any<CancellationToken>());

        var handler = new RecordMitigationValidation.Handler(_store, validationRepo, clock);
        var command = new RecordMitigationValidation.Command(
            IncidentId: KnownIncidentId,
            WorkflowId: KnownWorkflowGuid.ToString(),
            Status: ValidationStatus.Passed,
            ObservedOutcome: "Error rate returned to baseline",
            ValidatedBy: "ops@nextraceone.io",
            Checks:
            [
                new RecordMitigationValidation.ValidationCheckInput("Error rate < 0.5%", true, "0.3%"),
                new RecordMitigationValidation.ValidationCheckInput("Latency < 200ms", true, "145ms"),
            ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ValidationStatus.Passed);
        result.Value.ValidatedAt.Should().Be(fixedAt);
        captured.Should().NotBeNull();
        captured!.IncidentId.Should().Be(KnownIncidentId);
        captured.WorkflowId.Should().Be(KnownWorkflowGuid);
        captured.Status.Should().Be(ValidationStatus.Passed);
        captured.ObservedOutcome.Should().Be("Error rate returned to baseline");
        captured.ValidatedBy.Should().Be("ops@nextraceone.io");
        captured.ChecksJson.Should().Contain("Error rate");
        captured.ChecksJson.Should().Contain("0.3%");
    }

    [Fact]
    public async Task RecordMitigationValidation_NonGuidWorkflowId_ShouldReturnError()
    {
        var validationRepo = Substitute.For<IMitigationValidationRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = new RecordMitigationValidation.Handler(_store, validationRepo, clock);
        var command = new RecordMitigationValidation.Command(
            IncidentId: KnownIncidentId,
            WorkflowId: "not-a-guid",
            Status: ValidationStatus.Passed,
            ObservedOutcome: null,
            ValidatedBy: null,
            Checks: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await validationRepo.DidNotReceive().AddAsync(Arg.Any<MitigationValidationLog>(), Arg.Any<CancellationToken>());
    }

    // ── GetMitigationHistory ─────────────────────────────────────────

    [Fact]
    public async Task GetMitigationHistory_EmptyIncident_ReturnsEmptyList()
    {
        var workflowRepo = Substitute.For<IMitigationWorkflowRepository>();
        workflowRepo.GetByIncidentIdAsync(KnownIncidentId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MitigationWorkflowRecord>());

        var handler = new GetMitigationHistory.Handler(_store, workflowRepo);
        var result = await handler.Handle(new GetMitigationHistory.Query(KnownIncidentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMitigationHistory_MultipleWorkflows_MapsToAuditEntries()
    {
        var workflowRepo = Substitute.For<IMitigationWorkflowRepository>();
        var w1 = MitigationWorkflowRecord.Create(
            MitigationWorkflowRecordId.New(), KnownIncidentId, "Rollback",
            MitigationWorkflowStatus.Draft, MitigationActionType.RollbackCandidate,
            RiskLevel.Medium, true, "eng1@nextraceone.io");
        var w2 = MitigationWorkflowRecord.Create(
            MitigationWorkflowRecordId.New(), KnownIncidentId, "Restart",
            MitigationWorkflowStatus.Draft, MitigationActionType.Investigate,
            RiskLevel.Low, false, "eng2@nextraceone.io");

        workflowRepo.GetByIncidentIdAsync(KnownIncidentId, Arg.Any<CancellationToken>())
            .Returns([w1, w2]);

        var handler = new GetMitigationHistory.Handler(_store, workflowRepo);
        var result = await handler.Handle(new GetMitigationHistory.Query(KnownIncidentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().HaveCount(2);
        result.Value.Entries[0].Action.Should().Be(MitigationActionType.RollbackCandidate.ToString());
        result.Value.Entries[0].PerformedBy.Should().Be("eng1@nextraceone.io");
        result.Value.Entries[1].Action.Should().Be(MitigationActionType.Investigate.ToString());
        result.Value.Entries[1].PerformedBy.Should().Be("eng2@nextraceone.io");
    }

    [Fact]
    public async Task GetMitigationHistory_NonGuidIncidentId_ReturnsError()
    {
        var workflowRepo = Substitute.For<IMitigationWorkflowRepository>();
        var handler = new GetMitigationHistory.Handler(_store, workflowRepo);

        var result = await handler.Handle(new GetMitigationHistory.Query("not-a-guid"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
