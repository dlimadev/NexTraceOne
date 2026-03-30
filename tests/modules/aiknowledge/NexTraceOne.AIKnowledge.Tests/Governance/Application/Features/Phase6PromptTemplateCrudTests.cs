using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreatePromptTemplate;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetPromptTemplate;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListPromptTemplates;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdatePromptTemplate;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class Phase6PromptTemplateCrudTests
{
    private readonly IPromptTemplateRepository _repository = Substitute.For<IPromptTemplateRepository>();

    // ── ListPromptTemplates ─────────────────────────────────────────────

    [Fact]
    public async Task ListPromptTemplates_No_Filters_Returns_All_Active()
    {
        var templates = new List<PromptTemplate>
        {
            CreateTestTemplate("template-1"),
            CreateTestTemplate("template-2")
        };
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(templates.AsReadOnly());

        var handler = new ListPromptTemplates.Handler(_repository);
        var result = await handler.Handle(
            new ListPromptTemplates.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListPromptTemplates_By_Category_Filters_Correctly()
    {
        var templates = new List<PromptTemplate> { CreateTestTemplate("analysis-template") };
        _repository.GetByCategoryAsync("analysis", Arg.Any<CancellationToken>())
            .Returns(templates.AsReadOnly());

        var handler = new ListPromptTemplates.Handler(_repository);
        var result = await handler.Handle(
            new ListPromptTemplates.Query("analysis", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListPromptTemplates_By_Persona_Filters_Correctly()
    {
        var templates = new List<PromptTemplate> { CreateTestTemplate("engineer-template") };
        _repository.GetByPersonaAsync("Engineer", Arg.Any<CancellationToken>())
            .Returns(templates.AsReadOnly());

        var handler = new ListPromptTemplates.Handler(_repository);
        var result = await handler.Handle(
            new ListPromptTemplates.Query(null, "Engineer", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListPromptTemplates_Empty_Returns_Zero()
    {
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PromptTemplate>());

        var handler = new ListPromptTemplates.Handler(_repository);
        var result = await handler.Handle(
            new ListPromptTemplates.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── GetPromptTemplate ───────────────────────────────────────────────

    [Fact]
    public async Task GetPromptTemplate_Existing_Returns_Details()
    {
        var template = CreateTestTemplate("incident-analysis");
        _repository.GetByIdAsync(Arg.Any<PromptTemplateId>(), Arg.Any<CancellationToken>())
            .Returns(template);

        var handler = new GetPromptTemplate.Handler(_repository);
        var result = await handler.Handle(
            new GetPromptTemplate.Query(template.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("incident-analysis");
    }

    [Fact]
    public async Task GetPromptTemplate_NotFound_Returns_Error()
    {
        _repository.GetByIdAsync(Arg.Any<PromptTemplateId>(), Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var handler = new GetPromptTemplate.Handler(_repository);
        var result = await handler.Handle(
            new GetPromptTemplate.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── CreatePromptTemplate ────────────────────────────────────────────

    [Fact]
    public async Task CreatePromptTemplate_Valid_Succeeds()
    {
        _repository.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CreatePromptTemplate.Handler(_repository);
        var command = new CreatePromptTemplate.Command(
            "new-template", "New Template", "Description",
            "analysis", "Analyze {{serviceName}}", "serviceName",
            "Engineer", null, "high", null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TemplateId.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePromptTemplate_DuplicateName_Returns_Error()
    {
        _repository.ExistsByNameAsync("existing-template", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CreatePromptTemplate.Handler(_repository);
        var command = new CreatePromptTemplate.Command(
            "existing-template", "Existing", "Desc",
            "analysis", "Content", "", "", null, "medium", null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("DuplicateName");
    }

    [Fact]
    public async Task CreatePromptTemplate_Validator_Rejects_Empty_Content()
    {
        var validator = new CreatePromptTemplate.Validator();
        var command = new CreatePromptTemplate.Command(
            "test", "Display", "Desc", "cat", "", "", "", null, "medium", null, null, null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePromptTemplate_Validator_Rejects_Invalid_Temperature()
    {
        var validator = new CreatePromptTemplate.Validator();
        var command = new CreatePromptTemplate.Command(
            "test", "Display", "Desc", "cat", "content", "", "", null, "medium", null, 3.0m, null);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePromptTemplate_Validator_Accepts_Valid_Command()
    {
        var validator = new CreatePromptTemplate.Validator();
        var command = new CreatePromptTemplate.Command(
            "test-tpl", "Test Template", "Description",
            "analysis", "Analyze {{service}}", "service",
            "Engineer", null, "high", null, 0.7m, 2048);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }

    // ── UpdatePromptTemplate ────────────────────────────────────────────

    [Fact]
    public async Task UpdatePromptTemplate_Activate_Succeeds()
    {
        var template = CreateTestTemplate("inactive-template");
        template.Deactivate();
        _repository.GetByIdAsync(Arg.Any<PromptTemplateId>(), Arg.Any<CancellationToken>())
            .Returns(template);

        var handler = new UpdatePromptTemplate.Handler(_repository);
        var result = await handler.Handle(
            new UpdatePromptTemplate.Command(template.Id.Value, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePromptTemplate_NotFound_Returns_Error()
    {
        _repository.GetByIdAsync(Arg.Any<PromptTemplateId>(), Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var handler = new UpdatePromptTemplate.Handler(_repository);
        var result = await handler.Handle(
            new UpdatePromptTemplate.Command(Guid.NewGuid(), true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static PromptTemplate CreateTestTemplate(string name) =>
        PromptTemplate.Create(
            name: name, displayName: $"Test {name}", description: "Test template",
            category: "analysis", content: "Analyze {{serviceName}}",
            variables: "serviceName", version: 1, isActive: true, isOfficial: false,
            agentId: null, targetPersonas: "Engineer", scopeHint: null,
            relevance: "medium");
}
