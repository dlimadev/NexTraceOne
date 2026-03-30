using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateToolDefinition;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetToolDefinition;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListToolDefinitions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateToolDefinition;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class Phase6ToolDefinitionCrudTests
{
    private readonly IAiToolDefinitionRepository _repository = Substitute.For<IAiToolDefinitionRepository>();

    // ── ListToolDefinitions ─────────────────────────────────────────────

    [Fact]
    public async Task ListToolDefinitions_No_Filters_Returns_All_Active()
    {
        var tools = new List<AiToolDefinition>
        {
            CreateTestTool("tool-1"),
            CreateTestTool("tool-2")
        };
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(tools.AsReadOnly());

        var handler = new ListToolDefinitions.Handler(_repository);
        var result = await handler.Handle(
            new ListToolDefinitions.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListToolDefinitions_By_Category_Filters_Correctly()
    {
        var tools = new List<AiToolDefinition> { CreateTestTool("catalog-tool") };
        _repository.GetByCategoryAsync("service_catalog", Arg.Any<CancellationToken>())
            .Returns(tools.AsReadOnly());

        var handler = new ListToolDefinitions.Handler(_repository);
        var result = await handler.Handle(
            new ListToolDefinitions.Query("service_catalog", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListToolDefinitions_Empty_Returns_Zero()
    {
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiToolDefinition>());

        var handler = new ListToolDefinitions.Handler(_repository);
        var result = await handler.Handle(
            new ListToolDefinitions.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── GetToolDefinition ───────────────────────────────────────────────

    [Fact]
    public async Task GetToolDefinition_Existing_Returns_Details()
    {
        var tool = CreateTestTool("list_services");
        _repository.GetByIdAsync(Arg.Any<AiToolDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns(tool);

        var handler = new GetToolDefinition.Handler(_repository);
        var result = await handler.Handle(
            new GetToolDefinition.Query(tool.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("list_services");
    }

    [Fact]
    public async Task GetToolDefinition_NotFound_Returns_Error()
    {
        _repository.GetByIdAsync(Arg.Any<AiToolDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns((AiToolDefinition?)null);

        var handler = new GetToolDefinition.Handler(_repository);
        var result = await handler.Handle(
            new GetToolDefinition.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── CreateToolDefinition ────────────────────────────────────────────

    [Fact]
    public async Task CreateToolDefinition_Valid_Succeeds()
    {
        _repository.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CreateToolDefinition.Handler(_repository);
        var command = new CreateToolDefinition.Command(
            "new-tool", "New Tool", "Test tool",
            "service_catalog", "{}", false, 1, 30000);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToolId.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Any<AiToolDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateToolDefinition_DuplicateName_Returns_Error()
    {
        _repository.ExistsByNameAsync("existing-tool", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CreateToolDefinition.Handler(_repository);
        var command = new CreateToolDefinition.Command(
            "existing-tool", "Existing", "Desc", "cat", "{}", false, 0, 30000);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("DuplicateName");
    }

    [Fact]
    public async Task CreateToolDefinition_Validator_Rejects_Invalid_RiskLevel()
    {
        var validator = new CreateToolDefinition.Validator();
        var command = new CreateToolDefinition.Command(
            "test", "Display", "Desc", "cat", "{}", false, 5, 30000);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateToolDefinition_Validator_Accepts_Valid_Command()
    {
        var validator = new CreateToolDefinition.Validator();
        var command = new CreateToolDefinition.Command(
            "my-tool", "My Tool", "Description",
            "service_catalog", "{}", true, 2, 60000);

        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeTrue();
    }

    // ── UpdateToolDefinition ────────────────────────────────────────────

    [Fact]
    public async Task UpdateToolDefinition_Deactivate_Succeeds()
    {
        var tool = CreateTestTool("active-tool");
        _repository.GetByIdAsync(Arg.Any<AiToolDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns(tool);

        var handler = new UpdateToolDefinition.Handler(_repository);
        var result = await handler.Handle(
            new UpdateToolDefinition.Command(tool.Id.Value, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tool.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateToolDefinition_NotFound_Returns_Error()
    {
        _repository.GetByIdAsync(Arg.Any<AiToolDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns((AiToolDefinition?)null);

        var handler = new UpdateToolDefinition.Handler(_repository);
        var result = await handler.Handle(
            new UpdateToolDefinition.Command(Guid.NewGuid(), true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static AiToolDefinition CreateTestTool(string name) =>
        AiToolDefinition.Create(
            name: name, displayName: $"Test {name}", description: "Test tool",
            category: "service_catalog", parametersSchema: "{}",
            version: 1, isActive: true, requiresApproval: false,
            riskLevel: 1, isOfficial: false, timeoutMs: 30000);
}
