using System.Linq;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.AcknowledgeGuardianAlert;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CalculateChangeConfidence;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateWarRoom;
using NexTraceOne.AIKnowledge.Application.Governance.Features.DismissGuardianAlert;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetMemoryNodeDetails;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetWarRoom;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListGuardianAlerts;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListWarRooms;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ProcessNaturalLanguageQuery;
using NexTraceOne.AIKnowledge.Application.Governance.Features.QueryOrganizationalMemory;
using NexTraceOne.AIKnowledge.Application.Governance.Features.RecordMemoryNode;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ResolveWarRoom;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários das capacidades enterprise da Fase 11:
/// War Room Sessions, Change Confidence Score, Guardian Alerts, Organizational Memory Engine.
/// </summary>
public sealed class EnterpriseFeaturesTests
{
    private readonly IAiWarRoomRepository _warRoomRepo = Substitute.For<IAiWarRoomRepository>();
    private readonly IAiChangeConfidenceRepository _confidenceRepo = Substitute.For<IAiChangeConfidenceRepository>();
    private readonly IGuardianAlertRepository _guardianRepo = Substitute.For<IGuardianAlertRepository>();
    private readonly IOrganizationalMemoryRepository _memoryRepo = Substitute.For<IOrganizationalMemoryRepository>();
    private readonly ISkillRegistry _skillRegistry = Substitute.For<ISkillRegistry>();
    private readonly ISkillContextInjector _contextInjector = Substitute.For<ISkillContextInjector>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static readonly Guid _tenantId = Guid.NewGuid();

    // ── WarRoomSession domain entity ──────────────────────────────────────

    [Fact]
    public void WarRoomSession_Create_ValidInputs_ReturnsSessionWithOpenStatus()
    {
        var session = WarRoomSession.Create(
            "INC-001", "Payment service down", "P0", "payment-service",
            "agent-1", "incident-triage", _tenantId, DateTimeOffset.UtcNow);

        session.IncidentId.Should().Be("INC-001");
        session.IncidentTitle.Should().Be("Payment service down");
        session.Severity.Should().Be("P0");
        session.Status.Should().Be("Open");
        session.TenantId.Should().Be(_tenantId);
        session.ServiceAffected.Should().Be("payment-service");
        session.SkillUsed.Should().Be("incident-triage");
    }

    [Fact]
    public void WarRoomSession_Create_EmptyTenantId_ThrowsArgumentException()
    {
        var act = () => WarRoomSession.Create(
            "INC-001", "Down", "P0", "svc",
            "agent", "skill", Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WarRoomSession_AddParticipant_AddsUserToList()
    {
        var session = CreateWarRoom();

        session.AddParticipant("user-abc");

        session.ParticipantUserIds.Should().Contain("user-abc");
    }

    [Fact]
    public void WarRoomSession_AddParticipant_Duplicate_DoesNotAddTwice()
    {
        var session = CreateWarRoom();
        session.AddParticipant("user-1");
        session.AddParticipant("user-1");

        session.ParticipantUserIds.ToList().FindAll(u => u == "user-1").Should().HaveCount(1);
    }

    [Fact]
    public void WarRoomSession_Resolve_SetsStatusToResolvedWithPostMortem()
    {
        var session = CreateWarRoom();
        var resolvedAt = DateTimeOffset.UtcNow;

        session.Resolve("Root cause: DB deadlock", resolvedAt);

        session.Status.Should().Be("Resolved");
        session.PostMortemDraft.Should().Be("Root cause: DB deadlock");
        session.ResolvedAt.Should().Be(resolvedAt);
    }

    [Fact]
    public void WarRoomSession_AppendTimelineEvent_AddsEventJson()
    {
        var session = CreateWarRoom();

        session.AppendTimelineEvent("{\"type\":\"deploy\",\"time\":\"2026-01-01\"}");

        session.TimelineJson.Should().Contain("deploy");
    }

    [Fact]
    public void WarRoomSession_Close_SetsStatusToClosed()
    {
        var session = CreateWarRoom();

        session.Close();

        session.Status.Should().Be("Closed");
    }

    // ── CreateWarRoom handler ────────────────────────────────────────────

    [Fact]
    public async Task CreateWarRoom_ValidCommand_CreatesSessionAndCommits()
    {
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var command = new CreateWarRoom.Command(
            "INC-001", "Payment Down", "P1", "payment-svc",
            "agent-1", "incident-triage", _tenantId, "user-creator",
            ["user-eng-1", "user-eng-2"]);

        var handler = new CreateWarRoom.Handler(_warRoomRepo, _uow);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be("INC-001");
        result.Value.Status.Should().Be("Open");
        _warRoomRepo.Received(1).Add(Arg.Any<WarRoomSession>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── GetWarRoom handler ────────────────────────────────────────────────

    [Fact]
    public async Task GetWarRoom_ExistingId_ReturnsSessionDetails()
    {
        var session = CreateWarRoom();
        _warRoomRepo.GetByIdAsync(Arg.Any<WarRoomSessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var handler = new GetWarRoom.Handler(_warRoomRepo);
        var result = await handler.Handle(new GetWarRoom.Query(session.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be(session.IncidentId);
        result.Value.Severity.Should().Be(session.Severity);
    }

    [Fact]
    public async Task GetWarRoom_NotFound_ReturnsError()
    {
        _warRoomRepo.GetByIdAsync(Arg.Any<WarRoomSessionId>(), Arg.Any<CancellationToken>())
            .Returns((WarRoomSession?)null);

        var handler = new GetWarRoom.Handler(_warRoomRepo);
        var result = await handler.Handle(new GetWarRoom.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("WarRoom.NotFound");
    }

    // ── ListWarRooms handler ──────────────────────────────────────────────

    [Fact]
    public async Task ListWarRooms_OpenFilter_CallsListOpenAsync()
    {
        _warRoomRepo.ListOpenAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<WarRoomSession> { CreateWarRoom() }.AsReadOnly());

        var handler = new ListWarRooms.Handler(_warRoomRepo);
        var result = await handler.Handle(new ListWarRooms.Query(_tenantId, "open"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        await _warRoomRepo.Received(1).ListOpenAsync(_tenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListWarRooms_NoFilter_CallsListByTenantAsync()
    {
        _warRoomRepo.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<WarRoomSession>().AsReadOnly());

        var handler = new ListWarRooms.Handler(_warRoomRepo);
        var result = await handler.Handle(new ListWarRooms.Query(_tenantId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _warRoomRepo.Received(1).ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>());
    }

    // ── ResolveWarRoom handler ────────────────────────────────────────────

    [Fact]
    public async Task ResolveWarRoom_ExistingSession_SetsStatusResolved()
    {
        var session = CreateWarRoom();
        _warRoomRepo.GetByIdAsync(Arg.Any<WarRoomSessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new ResolveWarRoom.Handler(_warRoomRepo, _uow);
        var result = await handler.Handle(
            new ResolveWarRoom.Command(session.Id.Value, "Root cause identified."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Resolved");
        result.Value.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveWarRoom_NotFound_ReturnsError()
    {
        _warRoomRepo.GetByIdAsync(Arg.Any<WarRoomSessionId>(), Arg.Any<CancellationToken>())
            .Returns((WarRoomSession?)null);

        var handler = new ResolveWarRoom.Handler(_warRoomRepo, _uow);
        var result = await handler.Handle(
            new ResolveWarRoom.Command(Guid.NewGuid(), "PM"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("WarRoom.NotFound");
    }

    // ── ChangeConfidenceScore domain entity ───────────────────────────────

    [Fact]
    public void ChangeConfidenceScore_Calculate_HealthyInputs_ReturnsSafeVerdict()
    {
        var score = ChangeConfidenceScore.Calculate(
            "change-001", "payment-svc", _tenantId,
            blastRadiusScore: 0.9,
            testCoverageScore: 0.9,
            incidentHistoryScore: 0.9,
            timeOfDayScore: 0.9,
            deployerExperienceScore: 0.9,
            changeSizeScore: 0.9,
            dependencyStabilityScore: 0.9,
            "ci-pipeline", DateTimeOffset.UtcNow);

        score.Score.Should().BeGreaterThanOrEqualTo(80);
        score.Verdict.Should().Be("SAFE");
    }

    [Fact]
    public void ChangeConfidenceScore_Calculate_PoorInputs_ReturnsBlockVerdict()
    {
        var score = ChangeConfidenceScore.Calculate(
            "change-002", "payment-svc", _tenantId,
            blastRadiusScore: 0.0,
            testCoverageScore: 0.0,
            incidentHistoryScore: 0.0,
            timeOfDayScore: 0.0,
            deployerExperienceScore: 0.0,
            changeSizeScore: 0.0,
            dependencyStabilityScore: 0.0,
            "ci-pipeline", DateTimeOffset.UtcNow);

        score.Score.Should().Be(0);
        score.Verdict.Should().Be("BLOCK");
    }

    [Fact]
    public void ChangeConfidenceScore_Calculate_ScoreBreakdownJsonNotEmpty()
    {
        var score = ChangeConfidenceScore.Calculate(
            "change-003", "svc", _tenantId,
            0.7, 0.7, 0.7, 0.7, 0.7, 0.7, 0.7,
            "user", DateTimeOffset.UtcNow);

        score.ScoreBreakdownJson.Should().Contain("blastRadius");
        score.ScoreBreakdownJson.Should().Contain("testCoverage");
    }

    // ── CalculateChangeConfidence handler ─────────────────────────────────

    [Fact]
    public async Task CalculateChangeConfidence_ValidCommand_ReturnsScoredResult()
    {
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var command = new CalculateChangeConfidence.Command(
            "change-xyz", "auth-service", _tenantId,
            AffectedServicesCount: 2,
            TestCoverageDelta: 5.0,
            IncidentCountLast30Days: 1,
            IsWeekend: false,
            IsBusinessHours: true,
            DeployerSuccessfulDeploysCount: 30,
            ChangedLinesCount: 200,
            DependenciesWithOpenIncidents: 0,
            "ci-user");

        var handler = new CalculateChangeConfidence.Handler(_confidenceRepo, _uow);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeId.Should().Be("change-xyz");
        result.Value.ServiceName.Should().Be("auth-service");
        result.Value.Verdict.Should().NotBeEmpty();
        _confidenceRepo.Received(1).Add(Arg.Any<ChangeConfidenceScore>());
    }

    [Fact]
    public async Task CalculateChangeConfidence_WeekendDeploy_LowTimeOfDayScore()
    {
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var command = new CalculateChangeConfidence.Command(
            "ch-weekend", "svc", _tenantId,
            0, 0, 0, IsWeekend: true, IsBusinessHours: false,
            50, 0, 0, "user");

        var handler = new CalculateChangeConfidence.Handler(_confidenceRepo, _uow);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TimeOfDayScore.Should().Be(0.2);
    }

    // ── GuardianAlert domain entity ───────────────────────────────────────

    [Fact]
    public void GuardianAlert_Emit_ValidInputs_CreatesOpenAlert()
    {
        var alert = GuardianAlert.Emit(
            "auth-service", "performance", "Latency spike detected",
            "Scale up instances", 0.87, "high", _tenantId, DateTimeOffset.UtcNow);

        alert.ServiceName.Should().Be("auth-service");
        alert.PatternDetected.Should().Be("Latency spike detected");
        alert.Confidence.Should().Be(0.87);
        alert.Status.Should().Be("open");
    }

    [Fact]
    public void GuardianAlert_Acknowledge_SetsStatusAndUserId()
    {
        var alert = CreateGuardianAlert();
        var acknowledgedAt = DateTimeOffset.UtcNow;

        alert.Acknowledge("eng-user-1", acknowledgedAt);

        alert.Status.Should().Be("acknowledged");
        alert.AcknowledgedBy.Should().Be("eng-user-1");
        alert.AcknowledgedAt.Should().Be(acknowledgedAt);
    }

    [Fact]
    public void GuardianAlert_Dismiss_SetsStatusAndReason()
    {
        var alert = CreateGuardianAlert();

        alert.Dismiss("False positive — expected load pattern");

        alert.Status.Should().Be("dismissed");
        alert.DismissReason.Should().Contain("False positive");
    }

    [Fact]
    public void GuardianAlert_Resolve_WithActualIssue_SetsFlag()
    {
        var alert = CreateGuardianAlert();

        alert.Resolve(wasActualIssue: true);

        alert.Status.Should().Be("resolved");
        alert.WasActualIssue.Should().BeTrue();
    }

    // ── AcknowledgeGuardianAlert handler ──────────────────────────────────

    [Fact]
    public async Task AcknowledgeGuardianAlert_ExistingAlert_SetsAcknowledgedStatus()
    {
        var alert = CreateGuardianAlert();
        _guardianRepo.GetByIdAsync(Arg.Any<GuardianAlertId>(), Arg.Any<CancellationToken>())
            .Returns(alert);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new AcknowledgeGuardianAlert.Handler(_guardianRepo, _uow);
        var result = await handler.Handle(
            new AcknowledgeGuardianAlert.Command(alert.Id.Value, "on-call-engineer"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("acknowledged");
        result.Value.AcknowledgedBy.Should().Be("on-call-engineer");
    }

    [Fact]
    public async Task AcknowledgeGuardianAlert_NotFound_ReturnsError()
    {
        _guardianRepo.GetByIdAsync(Arg.Any<GuardianAlertId>(), Arg.Any<CancellationToken>())
            .Returns((GuardianAlert?)null);

        var handler = new AcknowledgeGuardianAlert.Handler(_guardianRepo, _uow);
        var result = await handler.Handle(
            new AcknowledgeGuardianAlert.Command(Guid.NewGuid(), "user"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Guardian.AlertNotFound");
    }

    // ── DismissGuardianAlert handler ──────────────────────────────────────

    [Fact]
    public async Task DismissGuardianAlert_ExistingAlert_SetsStatusDismissed()
    {
        var alert = CreateGuardianAlert();
        _guardianRepo.GetByIdAsync(Arg.Any<GuardianAlertId>(), Arg.Any<CancellationToken>())
            .Returns(alert);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new DismissGuardianAlert.Handler(_guardianRepo, _uow);
        var result = await handler.Handle(
            new DismissGuardianAlert.Command(alert.Id.Value, "Not relevant"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("dismissed");
    }

    [Fact]
    public async Task DismissGuardianAlert_NotFound_ReturnsError()
    {
        _guardianRepo.GetByIdAsync(Arg.Any<GuardianAlertId>(), Arg.Any<CancellationToken>())
            .Returns((GuardianAlert?)null);

        var handler = new DismissGuardianAlert.Handler(_guardianRepo, _uow);
        var result = await handler.Handle(
            new DismissGuardianAlert.Command(Guid.NewGuid(), "reason"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── ListGuardianAlerts handler ────────────────────────────────────────

    [Fact]
    public async Task ListGuardianAlerts_ByService_CallsListByServiceAsync()
    {
        _guardianRepo.ListByServiceAsync("auth-service", _tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<GuardianAlert> { CreateGuardianAlert() }.AsReadOnly());

        var handler = new ListGuardianAlerts.Handler(_guardianRepo);
        var result = await handler.Handle(
            new ListGuardianAlerts.Query(_tenantId, "auth-service", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        await _guardianRepo.Received(1).ListByServiceAsync("auth-service", _tenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListGuardianAlerts_OpenFilter_CallsListOpenAsync()
    {
        _guardianRepo.ListOpenAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<GuardianAlert>().AsReadOnly());

        var handler = new ListGuardianAlerts.Handler(_guardianRepo);
        var result = await handler.Handle(
            new ListGuardianAlerts.Query(_tenantId, null, "open"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _guardianRepo.Received(1).ListOpenAsync(_tenantId, Arg.Any<CancellationToken>());
    }

    // ── OrganizationalMemoryNode domain entity ────────────────────────────

    [Fact]
    public void OrganizationalMemoryNode_Create_ValidInputs_HasDefaultRelevanceScore()
    {
        var node = OrganizationalMemoryNode.Create(
            "decision", "auth-service", "Decision to use JWT",
            "We chose JWT for stateless auth", "Security review",
            "architect-1", ["auth", "security"],
            "meeting", "meeting-123", _tenantId, DateTimeOffset.UtcNow);

        node.NodeType.Should().Be("decision");
        node.Subject.Should().Be("auth-service");
        node.RelevanceScore.Should().Be(1.0);
        node.Tags.Should().Contain("auth");
    }

    [Fact]
    public void OrganizationalMemoryNode_LinkTo_AddsNodeIdToList()
    {
        var node = CreateMemoryNode();
        var relatedId = Guid.NewGuid();

        node.LinkTo(relatedId);

        node.LinkedNodeIds.Should().Contain(relatedId);
    }

    [Fact]
    public void OrganizationalMemoryNode_LinkTo_Duplicate_DoesNotAddTwice()
    {
        var node = CreateMemoryNode();
        var relatedId = Guid.NewGuid();

        node.LinkTo(relatedId);
        node.LinkTo(relatedId);

        node.LinkedNodeIds.ToList().FindAll(id => id == relatedId).Should().HaveCount(1);
    }

    [Fact]
    public void OrganizationalMemoryNode_UpdateRelevanceScore_ClampsToRange()
    {
        var node = CreateMemoryNode();

        node.UpdateRelevanceScore(1.5);
        node.RelevanceScore.Should().Be(1.0);

        node.UpdateRelevanceScore(-0.5);
        node.RelevanceScore.Should().Be(0.0);
    }

    // ── RecordMemoryNode handler ──────────────────────────────────────────

    [Fact]
    public async Task RecordMemoryNode_ValidCommand_CreatesNodeAndCommits()
    {
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var command = new RecordMemoryNode.Command(
            "decision", "payment-service", "Use Stripe SDK",
            "We selected Stripe for payments", "Architecture decision",
            "architect-1", ["payments", "external"],
            "adr", "adr-001", _tenantId);

        var handler = new RecordMemoryNode.Handler(_memoryRepo, _uow);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NodeType.Should().Be("decision");
        result.Value.Subject.Should().Be("payment-service");
        _memoryRepo.Received(1).Add(Arg.Any<OrganizationalMemoryNode>());
    }

    // ── QueryOrganizationalMemory handler ────────────────────────────────

    [Fact]
    public async Task QueryOrganizationalMemory_ReturnsMatchingNodes()
    {
        var nodes = new List<OrganizationalMemoryNode>
        {
            CreateMemoryNode("incident", "auth"),
            CreateMemoryNode("decision", "auth"),
        };
        _memoryRepo.SearchAsync("auth", _tenantId, 10, Arg.Any<CancellationToken>())
            .Returns(nodes.AsReadOnly());

        var handler = new QueryOrganizationalMemory.Handler(_memoryRepo);
        var result = await handler.Handle(
            new QueryOrganizationalMemory.Query("auth", _tenantId, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFound.Should().Be(2);
    }

    // ── GetMemoryNodeDetails handler ──────────────────────────────────────

    [Fact]
    public async Task GetMemoryNodeDetails_ExistingNode_ReturnsFullDetails()
    {
        var node = CreateMemoryNode();
        _memoryRepo.GetByIdAsync(Arg.Any<OrganizationalMemoryNodeId>(), Arg.Any<CancellationToken>())
            .Returns(node);
        _memoryRepo.GetLinkedNodesAsync(Arg.Any<OrganizationalMemoryNodeId>(), Arg.Any<CancellationToken>())
            .Returns(new List<OrganizationalMemoryNode>().AsReadOnly());

        var handler = new GetMemoryNodeDetails.Handler(_memoryRepo);
        var result = await handler.Handle(new GetMemoryNodeDetails.Query(node.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NodeType.Should().Be(node.NodeType);
        result.Value.Title.Should().Be(node.Title);
    }

    [Fact]
    public async Task GetMemoryNodeDetails_NotFound_ReturnsError()
    {
        _memoryRepo.GetByIdAsync(Arg.Any<OrganizationalMemoryNodeId>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationalMemoryNode?)null);

        var handler = new GetMemoryNodeDetails.Handler(_memoryRepo);
        var result = await handler.Handle(new GetMemoryNodeDetails.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Memory.NodeNotFound");
    }

    // ── ProcessNaturalLanguageQuery handler ───────────────────────────────

    [Fact]
    public async Task ProcessNaturalLanguageQuery_IncidentKeyword_ClassifiesAsOperations()
    {
        _skillRegistry.IsSkillAvailableAsync("incident-triage", _tenantId, Arg.Any<CancellationToken>())
            .Returns(true);
        _contextInjector.BuildSkillsSummaryBlockAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns("## Available Skills\n- incident-triage\n");

        var handler = new ProcessNaturalLanguageQuery.Handler(_skillRegistry, _contextInjector);
        var result = await handler.Handle(
            new ProcessNaturalLanguageQuery.Command(
                "There is an incident with payment service", _tenantId, "user-1", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("operations");
        result.Value.SkillSelected.Should().Be("incident-triage");
    }

    [Fact]
    public async Task ProcessNaturalLanguageQuery_ContractKeyword_ClassifiesAsArchitecture()
    {
        _skillRegistry.IsSkillAvailableAsync("architecture-fitness", _tenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new ProcessNaturalLanguageQuery.Handler(_skillRegistry, _contextInjector);
        var result = await handler.Handle(
            new ProcessNaturalLanguageQuery.Command(
                "What API contracts does auth service expose?", _tenantId, "user-1", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("architecture");
        result.Value.SkillSelected.Should().BeNull();
    }

    [Fact]
    public async Task ProcessNaturalLanguageQuery_CostKeyword_ClassifiesAsBusiness()
    {
        _skillRegistry.IsSkillAvailableAsync("tech-debt-quantifier", _tenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new ProcessNaturalLanguageQuery.Handler(_skillRegistry, _contextInjector);
        var result = await handler.Handle(
            new ProcessNaturalLanguageQuery.Command(
                "What is the token budget consumption?", _tenantId, "user-1", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("business");
    }

    [Fact]
    public async Task ProcessNaturalLanguageQuery_UnknownKeyword_ClassifiesAsGeneral()
    {
        _skillRegistry.IsSkillAvailableAsync(Arg.Any<string>(), _tenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new ProcessNaturalLanguageQuery.Handler(_skillRegistry, _contextInjector);
        var result = await handler.Handle(
            new ProcessNaturalLanguageQuery.Command(
                "Hello world", _tenantId, "user-1", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("general");
    }

    // ── Strongly typed ID tests ───────────────────────────────────────────

    [Fact]
    public void WarRoomSessionId_New_CreatesUniqueId()
    {
        var id1 = WarRoomSessionId.New();
        var id2 = WarRoomSessionId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ChangeConfidenceScoreId_From_RoundTrips()
    {
        var guid = Guid.NewGuid();
        var id = ChangeConfidenceScoreId.From(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void GuardianAlertId_From_RoundTrips()
    {
        var guid = Guid.NewGuid();
        var id = GuardianAlertId.From(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void OrganizationalMemoryNodeId_New_CreatesUniqueId()
    {
        var id1 = OrganizationalMemoryNodeId.New();
        var id2 = OrganizationalMemoryNodeId.New();

        id1.Should().NotBe(id2);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static WarRoomSession CreateWarRoom()
        => WarRoomSession.Create(
            "INC-TEST", "Test incident", "P1", "test-service",
            "agent-test", "incident-triage", _tenantId, DateTimeOffset.UtcNow);

    private static GuardianAlert CreateGuardianAlert()
        => GuardianAlert.Emit(
            "test-service", "performance", "CPU over 90%",
            "Scale service", 0.95, "high", _tenantId, DateTimeOffset.UtcNow);

    private static OrganizationalMemoryNode CreateMemoryNode(
        string nodeType = "decision", string subject = "auth-service")
        => OrganizationalMemoryNode.Create(
            nodeType, subject, $"Title for {subject}",
            "Content", "Context", "actor-1",
            ["tag1"], "manual", null, _tenantId, DateTimeOffset.UtcNow);
}
