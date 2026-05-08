using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.DeprecateSkill;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ExecuteSkill;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetSkillDetails;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListSkills;
using NexTraceOne.AIKnowledge.Application.Governance.Features.PublishSkill;
using NexTraceOne.AIKnowledge.Application.Governance.Features.RateSkillExecution;
using NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterSkill;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultSkills;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários do sistema de skills de IA (Phase 9).
/// Cobre criação, ciclo de vida, execução, feedback, listagem e seed do catálogo.
/// </summary>
public sealed class SkillsSystemTests
{
    private readonly IAiSkillRepository _skillRepo = Substitute.For<IAiSkillRepository>();
    private readonly IAiSkillExecutionRepository _executionRepo = Substitute.For<IAiSkillExecutionRepository>();
    private readonly IAiSkillFeedbackRepository _feedbackRepo = Substitute.For<IAiSkillFeedbackRepository>();
    private readonly ISkillExecutor _skillExecutor = Substitute.For<ISkillExecutor>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly Guid _tenantId = Guid.NewGuid();

    public SkillsSystemTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _skillExecutor.ExecuteAsync(Arg.Any<AiSkill>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SkillExecutionOutput(true, """{"result":"ok"}""", "gpt-4o", "openai",
                100, 50, TimeSpan.FromMilliseconds(320)));
    }

    // ── AiSkill.CreateSystem ───────────────────────────────────────────────

    [Fact]
    public void CreateSystem_ValidInputs_CreatesSkillCorrectly()
    {
        var skill = AiSkill.CreateSystem(
            name: "incident-triage",
            displayName: "Triagem de Incidentes",
            description: "Analisa incidentes",
            skillContent: "# Skill Content",
            tags: ["ops", "incident"],
            requiredTools: ["search_incidents"],
            preferredModels: [],
            isComposable: true);

        skill.Name.Should().Be("incident-triage");
        skill.DisplayName.Should().Be("Triagem de Incidentes");
        skill.OwnershipType.Should().Be(SkillOwnershipType.System);
        skill.Status.Should().Be(SkillStatus.Active);
        skill.Visibility.Should().Be(SkillVisibility.Public);
        skill.Tags.Should().Be("ops,incident");
        skill.RequiredTools.Should().Be("search_incidents");
        skill.IsComposable.Should().BeTrue();
        skill.OwnerId.Should().Be("system");
        skill.TenantId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void CreateSystem_NoTags_HasEmptyTags()
    {
        var skill = AiSkill.CreateSystem("my-skill", "My Skill", "Desc", "Content");

        skill.Tags.Should().BeEmpty();
        skill.RequiredTools.Should().BeEmpty();
        skill.PreferredModels.Should().BeEmpty();
    }

    // ── AiSkill.CreateCustom ───────────────────────────────────────────────

    [Fact]
    public void CreateCustom_ValidInputs_CreatesSkillInDraft()
    {
        var skill = AiSkill.CreateCustom(
            name: "custom-skill",
            displayName: "Custom Skill",
            description: "A custom skill",
            skillContent: "# Custom",
            ownershipType: SkillOwnershipType.Tenant,
            visibility: SkillVisibility.TeamOnly,
            ownerId: "user-123",
            tenantId: _tenantId);

        skill.Status.Should().Be(SkillStatus.Draft);
        skill.OwnershipType.Should().Be(SkillOwnershipType.Tenant);
        skill.Visibility.Should().Be(SkillVisibility.TeamOnly);
        skill.OwnerId.Should().Be("user-123");
        skill.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void CreateCustom_WithSystemOwnership_ThrowsArgumentException()
    {
        var act = () => AiSkill.CreateCustom(
            "skill", "Skill", "Desc", "Content",
            SkillOwnershipType.System, SkillVisibility.Public,
            "owner", _tenantId);

        act.Should().Throw<ArgumentException>();
    }

    // ── AiSkill.Activate ──────────────────────────────────────────────────

    [Fact]
    public void Activate_DraftSkill_ChangesStatusToActive()
    {
        var skill = CreateDraftSkill();

        skill.Activate();

        skill.Status.Should().Be(SkillStatus.Active);
    }

    // ── AiSkill.Deprecate ─────────────────────────────────────────────────

    [Fact]
    public void Deprecate_ActiveSkill_ChangesStatusToDeprecated()
    {
        var skill = CreateSystemSkill();

        skill.Deprecate();

        skill.Status.Should().Be(SkillStatus.Deprecated);
    }

    // ── AiSkill.IncrementExecutionCount ───────────────────────────────────

    [Fact]
    public void IncrementExecutionCount_IncrementsCounter()
    {
        var skill = CreateSystemSkill();

        skill.IncrementExecutionCount();
        skill.IncrementExecutionCount();

        skill.ExecutionCount.Should().Be(2);
    }

    // ── AiSkill.Update ────────────────────────────────────────────────────

    [Fact]
    public void Update_ValidInputs_UpdatesDisplayNameAndDescription()
    {
        var skill = CreateSystemSkill();

        skill.Update("New Display", "New Desc", "New Content", null, null, null, null, null, null);

        skill.DisplayName.Should().Be("New Display");
        skill.Description.Should().Be("New Desc");
    }

    // ── RegisterSkill handler ─────────────────────────────────────────────

    [Fact]
    public async Task RegisterSkill_NewName_CreatesSkillSuccessfully()
    {
        _skillRepo.ExistsByNameAsync("new-skill", _tenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new RegisterSkill.Command(
            Name: "new-skill",
            DisplayName: "New Skill",
            Description: "Description",
            SkillContent: "# Content",
            OwnershipType: "Tenant",
            Visibility: "TeamOnly",
            OwnerId: "user-1",
            TenantId: _tenantId,
            Tags: null,
            RequiredTools: null,
            PreferredModels: null,
            InputSchema: null,
            OutputSchema: null,
            IsComposable: false,
            ParentAgentId: null);

        var handler = new RegisterSkill.Handler(_skillRepo);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("new-skill");
        result.Value.Status.Should().Be("Draft");
        _skillRepo.Received(1).Add(Arg.Any<AiSkill>());
    }

    [Fact]
    public async Task RegisterSkill_DuplicateName_ReturnsError()
    {
        _skillRepo.ExistsByNameAsync("existing-skill", _tenantId, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new RegisterSkill.Command(
            "existing-skill", "Existing", "Desc", "Content",
            "Tenant", "Public", "user-1", _tenantId,
            null, null, null, null, null, false, null);

        var handler = new RegisterSkill.Handler(_skillRepo);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameAlreadyExists");
    }

    // ── ListSkills handler ────────────────────────────────────────────────

    [Fact]
    public async Task ListSkills_NoFilters_ReturnsAllSkills()
    {
        var skills = new List<AiSkill>
        {
            CreateSystemSkill("skill-1", "Skill One"),
            CreateSystemSkill("skill-2", "Skill Two"),
        };
        _skillRepo.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(skills.AsReadOnly());

        var handler = new ListSkills.Handler(_skillRepo);
        var result = await handler.Handle(new ListSkills.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListSkills_FilterByStatus_PassesFilterToRepository()
    {
        _skillRepo.ListAsync(SkillStatus.Active, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<AiSkill> { CreateSystemSkill() }.AsReadOnly());

        var handler = new ListSkills.Handler(_skillRepo);
        var result = await handler.Handle(new ListSkills.Query("Active", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    // ── GetSkillDetails handler ───────────────────────────────────────────

    [Fact]
    public async Task GetSkillDetails_ExistingSkill_ReturnsFullDetails()
    {
        var skillId = Guid.NewGuid();
        var skill = CreateSystemSkill("incident-triage", "Triagem");
        _skillRepo.GetByIdAsync(Arg.Is<AiSkillId>(x => x == AiSkillId.From(skillId)), Arg.Any<CancellationToken>())
            .Returns(skill);

        var handler = new GetSkillDetails.Handler(_skillRepo);
        var result = await handler.Handle(new GetSkillDetails.Query(skillId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("incident-triage");
        result.Value.DisplayName.Should().Be("Triagem");
        result.Value.OwnershipType.Should().Be("System");
        result.Value.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetSkillDetails_NotFound_ReturnsError()
    {
        var skillId = Guid.NewGuid();
        _skillRepo.GetByIdAsync(Arg.Any<AiSkillId>(), Arg.Any<CancellationToken>())
            .Returns((AiSkill?)null);

        var handler = new GetSkillDetails.Handler(_skillRepo);
        var result = await handler.Handle(new GetSkillDetails.Query(skillId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Skill.NotFound");
    }

    // ── ExecuteSkill handler ──────────────────────────────────────────────

    [Fact]
    public async Task ExecuteSkill_ActiveSkill_LogsExecutionAndIncrementsCount()
    {
        var skillId = Guid.NewGuid();
        var skill = CreateSystemSkill();
        _skillRepo.GetByIdAsync(Arg.Is<AiSkillId>(x => x == AiSkillId.From(skillId)), Arg.Any<CancellationToken>())
            .Returns(skill);

        var command = new ExecuteSkill.Command(
            SkillId: skillId,
            InputJson: """{"input": "test"}""",
            ModelOverride: null,
            AgentId: null,
            ExecutedBy: "user-1",
            TenantId: _tenantId);

        var handler = new ExecuteSkill.Handler(_skillRepo, _executionRepo, _skillExecutor, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Completed");
        _executionRepo.Received(1).Add(Arg.Any<AiSkillExecution>());
        skill.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteSkill_SkillNotFound_ReturnsError()
    {
        _skillRepo.GetByIdAsync(Arg.Any<AiSkillId>(), Arg.Any<CancellationToken>())
            .Returns((AiSkill?)null);

        var command = new ExecuteSkill.Command(
            Guid.NewGuid(), """{"input":"test"}""", null, null, "user-1", _tenantId);

        var handler = new ExecuteSkill.Handler(_skillRepo, _executionRepo, _skillExecutor, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Skill.NotFound");
    }

    [Fact]
    public async Task ExecuteSkill_InactiveSkill_ReturnsError()
    {
        var skillId = Guid.NewGuid();
        var skill = CreateDraftSkill();
        _skillRepo.GetByIdAsync(Arg.Is<AiSkillId>(x => x == AiSkillId.From(skillId)), Arg.Any<CancellationToken>())
            .Returns(skill);

        var command = new ExecuteSkill.Command(
            skillId, """{"input":"test"}""", null, null, "user-1", _tenantId);

        var handler = new ExecuteSkill.Handler(_skillRepo, _executionRepo, _skillExecutor, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Skill.NotActive");
    }

    // ── RateSkillExecution handler ────────────────────────────────────────

    [Fact]
    public async Task RateSkillExecution_ValidExecution_CreatesFeedback()
    {
        var executionId = Guid.NewGuid();
        var skillId = AiSkillId.New();
        var execution = AiSkillExecution.Log(
            skillId, "user-1", "gpt-4", "{}", "{}", 100, 10, 20, true, null, _tenantId, DateTimeOffset.UtcNow);
        var skill = CreateSystemSkill();

        _executionRepo.GetByIdAsync(
            Arg.Is<AiSkillExecutionId>(x => x == AiSkillExecutionId.From(executionId)),
            Arg.Any<CancellationToken>()).Returns(execution);
        _skillRepo.GetByIdAsync(Arg.Any<AiSkillId>(), Arg.Any<CancellationToken>()).Returns(skill);
        _feedbackRepo.GetAverageRatingBySkillAsync(Arg.Any<AiSkillId>(), Arg.Any<CancellationToken>())
            .Returns(4.5);

        var command = new RateSkillExecution.Command(
            ExecutionId: executionId,
            Rating: 5,
            Outcome: "resolved",
            Comment: "Great skill!",
            ActualOutcome: null,
            WasCorrect: true,
            SubmittedBy: "user-1",
            TenantId: _tenantId);

        var handler = new RateSkillExecution.Handler(_executionRepo, _skillRepo, _feedbackRepo);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NewAverageRating.Should().Be(4.5);
        _feedbackRepo.Received(1).Add(Arg.Any<AiSkillFeedback>());
    }

    [Fact]
    public async Task RateSkillExecution_ExecutionNotFound_ReturnsError()
    {
        _executionRepo.GetByIdAsync(Arg.Any<AiSkillExecutionId>(), Arg.Any<CancellationToken>())
            .Returns((AiSkillExecution?)null);

        var command = new RateSkillExecution.Command(
            Guid.NewGuid(), 5, "resolved", null, null, true, "user-1", _tenantId);

        var handler = new RateSkillExecution.Handler(_executionRepo, _skillRepo, _feedbackRepo);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ExecutionNotFound");
    }

    // ── PublishSkill handler ──────────────────────────────────────────────

    [Fact]
    public async Task PublishSkill_DraftSkill_ActivatesSkill()
    {
        var skillId = Guid.NewGuid();
        var skill = CreateDraftSkill();
        _skillRepo.GetByIdAsync(Arg.Is<AiSkillId>(x => x == AiSkillId.From(skillId)), Arg.Any<CancellationToken>())
            .Returns(skill);

        var handler = new PublishSkill.Handler(_skillRepo);
        var result = await handler.Handle(new PublishSkill.Command(skillId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Active");
        skill.Status.Should().Be(SkillStatus.Active);
    }

    [Fact]
    public async Task PublishSkill_NotFound_ReturnsError()
    {
        _skillRepo.GetByIdAsync(Arg.Any<AiSkillId>(), Arg.Any<CancellationToken>())
            .Returns((AiSkill?)null);

        var handler = new PublishSkill.Handler(_skillRepo);
        var result = await handler.Handle(new PublishSkill.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Skill.NotFound");
    }

    // ── DeprecateSkill handler ────────────────────────────────────────────

    [Fact]
    public async Task DeprecateSkill_ActiveSkill_MarksAsDeprecated()
    {
        var skillId = Guid.NewGuid();
        var skill = CreateSystemSkill();
        _skillRepo.GetByIdAsync(Arg.Is<AiSkillId>(x => x == AiSkillId.From(skillId)), Arg.Any<CancellationToken>())
            .Returns(skill);

        var handler = new DeprecateSkill.Handler(_skillRepo);
        var result = await handler.Handle(new DeprecateSkill.Command(skillId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Deprecated");
        skill.Status.Should().Be(SkillStatus.Deprecated);
    }

    [Fact]
    public async Task DeprecateSkill_NotFound_ReturnsError()
    {
        _skillRepo.GetByIdAsync(Arg.Any<AiSkillId>(), Arg.Any<CancellationToken>())
            .Returns((AiSkill?)null);

        var handler = new DeprecateSkill.Handler(_skillRepo);
        var result = await handler.Handle(new DeprecateSkill.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── SeedDefaultSkills handler ─────────────────────────────────────────

    [Fact]
    public async Task SeedDefaultSkills_EmptyRepository_Seeds12Skills()
    {
        _skillRepo.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<AiSkill>().AsReadOnly());

        var handler = new SeedDefaultSkills.Handler(_skillRepo);
        var result = await handler.Handle(new SeedDefaultSkills.Command(_tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeededCount.Should().Be(15);
        _skillRepo.Received(15).Add(Arg.Any<AiSkill>());
    }

    [Fact]
    public async Task SeedDefaultSkills_ExistingSkills_SkipsDuplicates()
    {
        var existing = new List<AiSkill>
        {
            CreateSystemSkill("incident-triage", "Triagem de Incidentes"),
            CreateSystemSkill("change-blast-radius", "Análise de Blast Radius"),
        };
        _skillRepo.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(existing.AsReadOnly());

        var handler = new SeedDefaultSkills.Handler(_skillRepo);
        var result = await handler.Handle(new SeedDefaultSkills.Command(_tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeededCount.Should().Be(13);
    }

    [Fact]
    public async Task SeedDefaultSkills_AllExisting_SeedsZero()
    {
        var catalog = DefaultSkillCatalog.GetAll();
        _skillRepo.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(catalog);

        var handler = new SeedDefaultSkills.Handler(_skillRepo);
        var result = await handler.Handle(new SeedDefaultSkills.Command(_tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeededCount.Should().Be(0);
    }

    // ── DefaultSkillCatalog ───────────────────────────────────────────────

    [Fact]
    public void DefaultSkillCatalog_GetAll_Returns12Skills()
    {
        var skills = DefaultSkillCatalog.GetAll();

        skills.Should().HaveCount(15);
    }

    [Fact]
    public void DefaultSkillCatalog_AllSkillsAreSystemAndActive()
    {
        var skills = DefaultSkillCatalog.GetAll();

        skills.Should().AllSatisfy(s =>
        {
            s.OwnershipType.Should().Be(SkillOwnershipType.System);
            s.Status.Should().Be(SkillStatus.Active);
            s.Visibility.Should().Be(SkillVisibility.Public);
        });
    }

    [Fact]
    public void DefaultSkillCatalog_AllSkillsHaveRequiredTools()
    {
        var skills = DefaultSkillCatalog.GetAll();

        skills.Should().AllSatisfy(s => s.RequiredTools.Should().NotBeEmpty());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AiSkill CreateSystemSkill(
        string name = "test-skill",
        string displayName = "Test Skill") =>
        AiSkill.CreateSystem(
            name: name,
            displayName: displayName,
            description: $"Description for {name}",
            skillContent: "# Skill Content",
            tags: ["test"],
            requiredTools: ["get_service_health"]);

    private static AiSkill CreateDraftSkill() =>
        AiSkill.CreateCustom(
            name: "draft-skill",
            displayName: "Draft Skill",
            description: "A draft skill",
            skillContent: "# Draft Content",
            ownershipType: SkillOwnershipType.Tenant,
            visibility: SkillVisibility.Private,
            ownerId: "user-1",
            tenantId: _tenantId);
}
