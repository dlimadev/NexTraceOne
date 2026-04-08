using System.Linq;

using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RecordMitigationValidation;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Tests.Incidents.Infrastructure;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Integration;

/// <summary>
/// Testes de integração para verificar a persistência real dos fluxos de mitigação.
/// Usa EF Core InMemory como provider de base de dados para validar o roundtrip completo:
/// CreateMitigationWorkflow → RecordMitigationValidation → GetMitigationHistory.
/// Verifica que EfIncidentStore persiste e lê dados reais (sem dados hardcoded).
/// </summary>
public sealed class MitigationPersistenceIntegrationTests : IDisposable
{
    private readonly IncidentDbContext _db;
    private readonly EfIncidentStore _store;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly IDateTimeProvider _clock;

    public MitigationPersistenceIntegrationTests()
    {
        var dbName = $"MitigationTest_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<IncidentDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        _currentTenant = Substitute.For<ICurrentTenant>();
        _currentTenant.Id.Returns(Guid.NewGuid());
        _currentTenant.Slug.Returns("test-tenant");
        _currentTenant.Name.Returns("Test Tenant");
        _currentTenant.IsActive.Returns(true);

        _currentUser = Substitute.For<ICurrentUser>();
        _currentUser.Id.Returns("test-user@nextraceone.io");
        _currentUser.Name.Returns("Test User");
        _currentUser.Email.Returns("test-user@nextraceone.io");
        _currentUser.IsAuthenticated.Returns(true);

        _clock = Substitute.For<IDateTimeProvider>();
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _db = new IncidentDbContext(options, _currentTenant, _currentUser, _clock);
        _db.Database.EnsureCreated();

        _store = new EfIncidentStore(_db, _clock, _currentUser);
    }

    public void Dispose() => _db.Dispose();

    // ── Helper ──────────────────────────────────────────────────────────

    private string SeedIncident(string serviceId = "payment-service")
    {
        var result = _store.CreateIncident(new CreateIncidentInput(
            Title: "Integration test incident",
            Description: "Created for integration test",
            IncidentType: IncidentType.ServiceDegradation,
            Severity: IncidentSeverity.Major,
            ServiceId: serviceId,
            ServiceDisplayName: "Payment Service",
            OwnerTeam: "payments-team",
            ImpactedDomain: "Finance",
            Environment: "Production",
            DetectedAtUtc: DateTimeOffset.UtcNow.AddHours(-1)));

        return result.IncidentId.ToString();
    }

    // ── CreateMitigationWorkflowAsync ──────────────────────────────────

    [Fact]
    public async Task CreateMitigationWorkflow_ShouldPersistWorkflowToDatabase()
    {
        var incidentId = SeedIncident();

        var response = await _store.CreateMitigationWorkflowAsync(
            incidentId,
            title: "Rollback deployment",
            actionType: MitigationActionType.RollbackCandidate,
            riskLevel: RiskLevel.Medium,
            requiresApproval: true,
            linkedRunbookId: null,
            steps: null,
            ct: CancellationToken.None);

        response.Should().NotBeNull();
        response.Status.Should().Be(MitigationWorkflowStatus.Draft);
        response.WorkflowId.Should().NotBeEmpty();

        // Verify the record was actually saved to the DB
        var persisted = await _db.MitigationWorkflows
            .FirstOrDefaultAsync(w => w.Id.Value == response.WorkflowId);
        persisted.Should().NotBeNull();
        persisted!.IncidentId.Should().Be(incidentId);
        persisted.Title.Should().Be("Rollback deployment");
        persisted.ActionType.Should().Be(MitigationActionType.RollbackCandidate);
    }

    [Fact]
    public async Task CreateMitigationWorkflow_ShouldUseCurrentUserAsCreatedBy()
    {
        var incidentId = SeedIncident();

        var response = await _store.CreateMitigationWorkflowAsync(
            incidentId, "Fix config", MitigationActionType.ValidateChange,
            RiskLevel.Low, false, null, null, CancellationToken.None);

        var persisted = await _db.MitigationWorkflows
            .FirstOrDefaultAsync(w => w.Id.Value == response.WorkflowId);
        persisted.Should().NotBeNull();
        persisted!.CreatedByUser.Should().Be("test-user@nextraceone.io");
    }

    [Fact]
    public async Task CreateMitigationWorkflow_WithSteps_ShouldPersistStepsJson()
    {
        var incidentId = SeedIncident();
        var steps = new[]
        {
            new CreateMitigationWorkflow.CreateStepDto(1, "Rollback deployment", "kubectl rollout undo"),
            new CreateMitigationWorkflow.CreateStepDto(2, "Verify health", "Check /health endpoint"),
        };

        var response = await _store.CreateMitigationWorkflowAsync(
            incidentId, "Rollback with steps", MitigationActionType.RollbackCandidate,
            RiskLevel.High, true, null, steps, CancellationToken.None);

        var persisted = await _db.MitigationWorkflows
            .FirstOrDefaultAsync(w => w.Id.Value == response.WorkflowId);
        persisted.Should().NotBeNull();
        persisted!.StepsJson.Should().NotBeNullOrEmpty();
        persisted.StepsJson.Should().Contain("Rollback deployment");
    }

    // ── RecordMitigationValidationAsync ───────────────────────────────

    [Fact]
    public async Task RecordMitigationValidation_ShouldPersistValidationLog()
    {
        var incidentId = SeedIncident();
        var wfResponse = await _store.CreateMitigationWorkflowAsync(
            incidentId, "Rollback", MitigationActionType.RollbackCandidate,
            RiskLevel.Medium, false, null, null, CancellationToken.None);

        var checks = new[]
        {
            new RecordMitigationValidation.ValidationCheckInput("Error rate check", true, "0.2%"),
            new RecordMitigationValidation.ValidationCheckInput("Latency check", true, "120ms"),
        };

        var valResponse = await _store.RecordMitigationValidationAsync(
            incidentId, wfResponse.WorkflowId.ToString(),
            ValidationStatus.Passed, "All checks passed",
            "ops@nextraceone.io", checks, CancellationToken.None);

        valResponse.Should().NotBeNull();
        valResponse!.Status.Should().Be(ValidationStatus.Passed);

        // Verify the validation was actually saved to the DB
        var persisted = await _db.MitigationValidations
            .FirstOrDefaultAsync(v => v.WorkflowId == wfResponse.WorkflowId);
        persisted.Should().NotBeNull();
        persisted!.IncidentId.Should().Be(incidentId);
        persisted.Status.Should().Be(ValidationStatus.Passed);
        persisted.ObservedOutcome.Should().Be("All checks passed");
        persisted.ValidatedBy.Should().Be("ops@nextraceone.io");
        persisted.ChecksJson.Should().Contain("Error rate check");
    }

    // ── GetMitigationHistoryAsync — roundtrip ─────────────────────────

    [Fact]
    public async Task GetMitigationHistory_ShouldReturnEmptyListWhenNoActions()
    {
        var incidentId = SeedIncident();

        var history = await _store.GetMitigationHistoryAsync(incidentId, CancellationToken.None);

        history.Should().NotBeNull();
        history!.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMitigationHistory_AfterWorkflowAction_ShouldReturnPersistedEntries()
    {
        var incidentId = SeedIncident();
        var wfResponse = await _store.CreateMitigationWorkflowAsync(
            incidentId, "Rollback", MitigationActionType.RollbackCandidate,
            RiskLevel.Medium, false, null, null, CancellationToken.None);

        // Record an action on the workflow
        _store.UpdateMitigationWorkflowAction(
            incidentId, wfResponse.WorkflowId.ToString(),
            action: "approve", newStatus: MitigationWorkflowStatus.Approved,
            performedBy: "tech-lead@nextraceone.io",
            reason: "Approved for execution",
            notes: null);

        var history = await _store.GetMitigationHistoryAsync(incidentId, CancellationToken.None);

        history.Should().NotBeNull();
        history!.IncidentId.Should().Be(Guid.Parse(incidentId));
        history.Entries.Should().HaveCount(1);
        history.Entries[0].Action.Should().Be("approve");
        history.Entries[0].PerformedBy.Should().Be("tech-lead@nextraceone.io");
    }

    [Fact]
    public async Task FullRoundtrip_CreateWorkflow_RecordValidation_GetHistory()
    {
        // 1. Create incident
        var incidentId = SeedIncident();

        // 2. Create mitigation workflow
        var wfResponse = await _store.CreateMitigationWorkflowAsync(
            incidentId, "Rollback to v2.13.2",
            MitigationActionType.RollbackCandidate, RiskLevel.Medium,
            requiresApproval: true, linkedRunbookId: null, steps: null,
            CancellationToken.None);

        wfResponse.Status.Should().Be(MitigationWorkflowStatus.Draft);

        // 3. Approve and execute actions
        _store.UpdateMitigationWorkflowAction(
            incidentId, wfResponse.WorkflowId.ToString(),
            "approve", MitigationWorkflowStatus.Approved,
            "tech-lead@nextraceone.io", "Evidence confirmed", null);

        _store.UpdateMitigationWorkflowAction(
            incidentId, wfResponse.WorkflowId.ToString(),
            "rollback-triggered", MitigationWorkflowStatus.InProgress,
            "ops-engineer@nextraceone.io", null, "Rollback triggered via CI pipeline");

        // 4. Record validation
        var valResponse = await _store.RecordMitigationValidationAsync(
            incidentId, wfResponse.WorkflowId.ToString(),
            ValidationStatus.Passed, "Error rate returned to baseline",
            "ops-engineer@nextraceone.io",
            new[] { new RecordMitigationValidation.ValidationCheckInput("Error rate < 0.5%", true, "0.3%") },
            CancellationToken.None);

        valResponse.Should().NotBeNull();
        valResponse!.Status.Should().Be(ValidationStatus.Passed);

        // 5. Get history — should have 2 action entries from real DB (not hardcoded)
        var history = await _store.GetMitigationHistoryAsync(incidentId, CancellationToken.None);

        history.Should().NotBeNull();
        history!.IncidentId.Should().Be(Guid.Parse(incidentId));
        history.Entries.Should().HaveCount(2);
        history.Entries[0].Action.Should().Be("approve");
        history.Entries[1].Action.Should().Be("rollback-triggered");

        // 6. Verify validation persisted
        var validation = _db.MitigationValidations
            .FirstOrDefault(v => v.IncidentId == incidentId);
        validation.Should().NotBeNull();
        validation!.Status.Should().Be(ValidationStatus.Passed);
    }

    [Fact]
    public async Task GetMitigationHistory_UnknownIncidentId_ShouldReturnNull()
    {
        var unknownId = Guid.NewGuid().ToString();
        var history = await _store.GetMitigationHistoryAsync(unknownId, CancellationToken.None);
        history.Should().BeNull();
    }

    [Fact]
    public async Task GetMitigationHistory_NonGuidIncidentId_ShouldReturnNull()
    {
        var history = await _store.GetMitigationHistoryAsync("not-a-guid", CancellationToken.None);
        history.Should().BeNull();
    }
}
