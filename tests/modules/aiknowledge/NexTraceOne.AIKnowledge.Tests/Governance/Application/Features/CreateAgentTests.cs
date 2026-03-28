using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateAgent;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para a feature CreateAgent.
/// Valida criação de agents Tenant e User, rejeição de System via API,
/// e fluxo de persistência no repositório.
/// </summary>
public sealed class CreateAgentTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IAiAgentRepository _repository = Substitute.For<IAiAgentRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CreateAgent.Handler _handler;

    public CreateAgentTests()
    {
        _currentUser.Id.Returns("user-test-id");
        _currentUser.Name.Returns("Test User");
        _handler = new CreateAgent.Handler(_repository, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidTenantAgent_ShouldPersistAndReturnResponse()
    {
        var addedAgents = new List<AiAgent>();
        _repository.AddAsync(Arg.Do<AiAgent>(a => addedAgents.Add(a)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = new CreateAgent.Command(
            Name: "my-tenant-agent",
            DisplayName: "My Tenant Agent",
            Description: "Agent for tenant use",
            Category: "ChangeIntelligence",
            SystemPrompt: "You are a tenant agent for NexTraceOne.",
            Objective: "Assist with operational queries",
            OwnershipType: "Tenant",
            Visibility: "Tenant",
            PreferredModelId: null,
            AllowedModelIds: null,
            AllowedTools: null,
            Capabilities: "chat,analysis",
            TargetPersona: "Engineer",
            InputSchema: null,
            OutputSchema: null,
            Icon: "🤖");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AgentId.Should().NotBe(Guid.Empty);
        result.Value.Slug.Should().NotBeNullOrWhiteSpace();
        addedAgents.Should().ContainSingle();
        addedAgents[0].Name.Should().Be("my-tenant-agent");
    }

    [Fact]
    public async Task Handle_ValidUserAgent_ShouldAssignCurrentUserId()
    {
        AiAgent? persisted = null;
        _repository.AddAsync(Arg.Do<AiAgent>(a => persisted = a), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = new CreateAgent.Command(
            Name: "my-user-agent",
            DisplayName: "My User Agent",
            Description: "Personal agent",
            Category: "ApiDesign",
            SystemPrompt: "You are a personal API design agent.",
            Objective: "Design REST contracts",
            OwnershipType: "User",
            Visibility: "Private",
            PreferredModelId: null,
            AllowedModelIds: null,
            AllowedTools: null,
            Capabilities: null,
            TargetPersona: null,
            InputSchema: null,
            OutputSchema: null,
            Icon: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        persisted.Should().NotBeNull();
        persisted!.OwnerId.Should().Be("user-test-id");
    }

    [Fact]
    public async Task Handle_SystemOwnershipType_ShouldReturnFailure()
    {
        var command = new CreateAgent.Command(
            Name: "system-agent",
            DisplayName: "System Agent",
            Description: "Should be blocked",
            Category: "ApiDesign",
            SystemPrompt: "System prompt",
            Objective: "Blocked objective",
            OwnershipType: "System",
            Visibility: "Tenant",
            PreferredModelId: null,
            AllowedModelIds: null,
            AllowedTools: null,
            Capabilities: null,
            TargetPersona: null,
            InputSchema: null,
            OutputSchema: null,
            Icon: null);

        var validator = new CreateAgent.Validator();
        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().ContainSingle(e =>
            e.PropertyName == "OwnershipType" &&
            e.ErrorMessage.Contains("System agents cannot be created via API."));

        await _repository.DidNotReceive().AddAsync(Arg.Any<AiAgent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldFailValidation()
    {
        var command = new CreateAgent.Command(
            Name: "",
            DisplayName: "Display",
            Description: "desc",
            Category: "ApiDesign",
            SystemPrompt: "prompt",
            Objective: "obj",
            OwnershipType: "User",
            Visibility: "Private",
            PreferredModelId: null,
            AllowedModelIds: null,
            AllowedTools: null,
            Capabilities: null,
            TargetPersona: null,
            InputSchema: null,
            OutputSchema: null,
            Icon: null);

        var validator = new CreateAgent.Validator();
        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Handle_EmptySystemPrompt_ShouldFailValidation()
    {
        var command = new CreateAgent.Command(
            Name: "my-agent",
            DisplayName: "My Agent",
            Description: "desc",
            Category: "ApiDesign",
            SystemPrompt: "",
            Objective: "obj",
            OwnershipType: "User",
            Visibility: "Private",
            PreferredModelId: null,
            AllowedModelIds: null,
            AllowedTools: null,
            Capabilities: null,
            TargetPersona: null,
            InputSchema: null,
            OutputSchema: null,
            Icon: null);

        var validator = new CreateAgent.Validator();
        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "SystemPrompt");
    }
}
