using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateGuardrail;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetGuardrail;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListGuardrails;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateGuardrail;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class Phase6GuardrailCrudTests
{
    private readonly IAiGuardrailRepository _repository = Substitute.For<IAiGuardrailRepository>();

    // ── ListGuardrails ──────────────────────────────────────────────────

    [Fact]
    public async Task ListGuardrails_No_Filters_Returns_All_Active()
    {
        var guardrails = new List<AiGuardrail>
        {
            CreateTestGuardrail("test-guard-1"),
            CreateTestGuardrail("test-guard-2")
        };
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(guardrails.AsReadOnly());

        var handler = new ListGuardrails.Handler(_repository);
        var result = await handler.Handle(
            new ListGuardrails.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListGuardrails_By_Category_Filters_Correctly()
    {
        var guardrails = new List<AiGuardrail> { CreateTestGuardrail("sec-guard") };
        _repository.GetByCategoryAsync("security", Arg.Any<CancellationToken>())
            .Returns(guardrails.AsReadOnly());

        var handler = new ListGuardrails.Handler(_repository);
        var result = await handler.Handle(
            new ListGuardrails.Query("security", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        await _repository.Received(1).GetByCategoryAsync("security", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListGuardrails_By_GuardType_Filters_Correctly()
    {
        var guardrails = new List<AiGuardrail> { CreateTestGuardrail("input-guard") };
        _repository.GetByGuardTypeAsync("input", Arg.Any<CancellationToken>())
            .Returns(guardrails.AsReadOnly());

        var handler = new ListGuardrails.Handler(_repository);
        var result = await handler.Handle(
            new ListGuardrails.Query(null, "input", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListGuardrails_Empty_Returns_Zero()
    {
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiGuardrail>());

        var handler = new ListGuardrails.Handler(_repository);
        var result = await handler.Handle(
            new ListGuardrails.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── GetGuardrail ────────────────────────────────────────────────────

    [Fact]
    public async Task GetGuardrail_Existing_Returns_Details()
    {
        var guardrail = CreateTestGuardrail("pii-detection");
        _repository.GetByIdAsync(Arg.Any<AiGuardrailId>(), Arg.Any<CancellationToken>())
            .Returns(guardrail);

        var handler = new GetGuardrail.Handler(_repository);
        var result = await handler.Handle(
            new GetGuardrail.Query(guardrail.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("pii-detection");
    }

    [Fact]
    public async Task GetGuardrail_NotFound_Returns_Error()
    {
        _repository.GetByIdAsync(Arg.Any<AiGuardrailId>(), Arg.Any<CancellationToken>())
            .Returns((AiGuardrail?)null);

        var handler = new GetGuardrail.Handler(_repository);
        var result = await handler.Handle(
            new GetGuardrail.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── CreateGuardrail ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateGuardrail_Valid_Succeeds()
    {
        _repository.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CreateGuardrail.Handler(_repository);
        var command = new CreateGuardrail.Command(
            "new-guardrail", "New Guardrail", "Test description",
            "security", "input", @"\btest\b", "regex",
            "medium", "warn", null, null, null, 5);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GuardrailId.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Any<AiGuardrail>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateGuardrail_DuplicateName_Returns_Error()
    {
        _repository.ExistsByNameAsync("existing-guard", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CreateGuardrail.Handler(_repository);
        var command = new CreateGuardrail.Command(
            "existing-guard", "Existing", "Description",
            "security", "input", @"\btest\b", "regex",
            "medium", "warn", null, null, null, 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("DuplicateName");
        await _repository.DidNotReceive().AddAsync(Arg.Any<AiGuardrail>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateGuardrail_Validator_Rejects_Empty_Name()
    {
        var validator = new CreateGuardrail.Validator();
        var command = new CreateGuardrail.Command(
            "", "Display", "Desc", "cat", "input", "pattern", "regex",
            "low", "log", null, null, null, 0);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateGuardrail_Validator_Rejects_Invalid_GuardType()
    {
        var validator = new CreateGuardrail.Validator();
        var command = new CreateGuardrail.Command(
            "test", "Display", "Desc", "cat", "invalid", "pattern", "regex",
            "low", "log", null, null, null, 0);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateGuardrail_Validator_Accepts_Valid_Command()
    {
        var validator = new CreateGuardrail.Validator();
        var command = new CreateGuardrail.Command(
            "test-guard", "Test Guard", "Description",
            "security", "input", @"\btest\b", "regex",
            "critical", "block", null, null, null, 1);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }

    // ── UpdateGuardrail ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateGuardrail_Activate_Succeeds()
    {
        var guardrail = CreateTestGuardrail("deactivated-guard");
        guardrail.Deactivate();
        _repository.GetByIdAsync(Arg.Any<AiGuardrailId>(), Arg.Any<CancellationToken>())
            .Returns(guardrail);

        var handler = new UpdateGuardrail.Handler(_repository);
        var result = await handler.Handle(
            new UpdateGuardrail.Command(guardrail.Id.Value, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        guardrail.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateGuardrail_Deactivate_Succeeds()
    {
        var guardrail = CreateTestGuardrail("active-guard");
        _repository.GetByIdAsync(Arg.Any<AiGuardrailId>(), Arg.Any<CancellationToken>())
            .Returns(guardrail);

        var handler = new UpdateGuardrail.Handler(_repository);
        var result = await handler.Handle(
            new UpdateGuardrail.Command(guardrail.Id.Value, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        guardrail.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateGuardrail_NotFound_Returns_Error()
    {
        _repository.GetByIdAsync(Arg.Any<AiGuardrailId>(), Arg.Any<CancellationToken>())
            .Returns((AiGuardrail?)null);

        var handler = new UpdateGuardrail.Handler(_repository);
        var result = await handler.Handle(
            new UpdateGuardrail.Command(Guid.NewGuid(), true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static AiGuardrail CreateTestGuardrail(string name) =>
        AiGuardrail.Create(
            name: name, displayName: $"Test {name}", description: "Test guardrail",
            category: GuardrailCategory.Security, guardType: GuardrailType.Input, pattern: @"\btest\b",
            patternType: GuardrailPatternType.Regex, severity: GuardrailSeverity.Medium, action: GuardrailAction.Warn,
            userMessage: null, isActive: true, isOfficial: false,
            agentId: null, modelId: null, priority: 1);
}
