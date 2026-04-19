using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgent;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListAgents;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateAgent;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para ListAgents, GetAgent e UpdateAgent.
/// Cobre listagem com filtros, obtenção individual, not-found e atualização de agents.
/// </summary>
public sealed class AiAgentCrudGapsTests
{
    private readonly IAiAgentRepository _repository = Substitute.For<IAiAgentRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    // ── ListAgents ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAgents_NoFilter_ReturnsAllActiveAgents()
    {
        var agents = new List<AiAgent>
        {
            CreateAgent("agent-1", "Agent One", AgentCategory.ChangeIntelligence, isOfficial: true),
            CreateAgent("agent-2", "Agent Two", AgentCategory.ApiDesign, isOfficial: false),
        };
        _repository.ListAsync(isActive: true, isOfficial: null, Arg.Any<CancellationToken>())
            .Returns(agents.AsReadOnly());

        var handler = new ListAgents.Handler(_repository, _currentUser);
        var result = await handler.Handle(new ListAgents.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAgents_FilterOfficialOnly_InvokesRepositoryWithFlag()
    {
        var agents = new List<AiAgent>
        {
            CreateAgent("official-agent", "Official Agent", AgentCategory.ChangeIntelligence, isOfficial: true),
        };
        _repository.ListAsync(isActive: true, isOfficial: true, Arg.Any<CancellationToken>())
            .Returns(agents.AsReadOnly());

        var handler = new ListAgents.Handler(_repository, _currentUser);
        var result = await handler.Handle(new ListAgents.Query(IsOfficial: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].IsOfficial.Should().BeTrue();
    }

    [Fact]
    public async Task ListAgents_EmptyRepository_ReturnsEmptyList()
    {
        _repository.ListAsync(isActive: true, isOfficial: null, Arg.Any<CancellationToken>())
            .Returns(new List<AiAgent>().AsReadOnly());

        var handler = new ListAgents.Handler(_repository, _currentUser);
        var result = await handler.Handle(new ListAgents.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAgents_ReturnsItemsOrderedBySortOrder()
    {
        var agent1 = CreateAgent("z-agent", "Z Agent", AgentCategory.ApiDesign, isOfficial: false, sortOrder: 200);
        var agent2 = CreateAgent("a-agent", "A Agent", AgentCategory.ChangeIntelligence, isOfficial: false, sortOrder: 10);
        var agents = new List<AiAgent> { agent1, agent2 };

        _repository.ListAsync(isActive: true, isOfficial: null, Arg.Any<CancellationToken>())
            .Returns(agents.AsReadOnly());

        var handler = new ListAgents.Handler(_repository, _currentUser);
        var result = await handler.Handle(new ListAgents.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Name.Should().Be("a-agent");
        result.Value.Items[1].Name.Should().Be("z-agent");
    }

    // ── GetAgent ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAgent_ExistingAgent_ReturnsDetails()
    {
        var agentId = Guid.NewGuid();
        var agent = CreateAgent("my-agent", "My Agent", AgentCategory.ChangeIntelligence, isOfficial: true);

        _repository.GetByIdAsync(Arg.Is<AiAgentId>(x => x == AiAgentId.From(agentId)), Arg.Any<CancellationToken>())
            .Returns(agent);

        var handler = new GetAgent.Handler(_repository);
        var result = await handler.Handle(new GetAgent.Query(agentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("my-agent");
        result.Value.DisplayName.Should().Be("My Agent");
        result.Value.IsOfficial.Should().BeTrue();
    }

    [Fact]
    public async Task GetAgent_NotFound_ReturnsFailure()
    {
        var agentId = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Any<AiAgentId>(), Arg.Any<CancellationToken>())
            .Returns((AiAgent?)null);

        var handler = new GetAgent.Handler(_repository);
        var result = await handler.Handle(new GetAgent.Query(agentId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Agent.NotFound");
    }

    // ── UpdateAgent ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAgent_ValidCommand_UpdatesAndReturnsSuccess()
    {
        var agentId = Guid.NewGuid();
        var agent = CreateCustomAgent("custom-agent", "Custom Agent", AgentCategory.ApiDesign);

        _repository.GetByIdAsync(Arg.Is<AiAgentId>(x => x == AiAgentId.From(agentId)), Arg.Any<CancellationToken>())
            .Returns(agent);

        var command = new UpdateAgent.Command(
            AgentId: agentId,
            DisplayName: "Updated Display Name",
            Description: "Updated description",
            SystemPrompt: "Updated prompt",
            Objective: "New objective",
            Capabilities: "chat,code",
            TargetPersona: "Engineer",
            Icon: "🔧",
            PreferredModelId: null,
            AllowedModelIds: null,
            AllowedTools: null,
            InputSchema: null,
            OutputSchema: null,
            Visibility: null,
            AllowModelOverride: true,
            SortOrder: 50);

        var handler = new UpdateAgent.Handler(_repository);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).UpdateAsync(agent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAgent_AgentNotFound_ReturnsFailure()
    {
        _repository.GetByIdAsync(Arg.Any<AiAgentId>(), Arg.Any<CancellationToken>())
            .Returns((AiAgent?)null);

        var command = new UpdateAgent.Command(
            AgentId: Guid.NewGuid(),
            DisplayName: "Updated",
            Description: "Desc",
            SystemPrompt: null,
            Objective: null,
            Capabilities: null,
            TargetPersona: null,
            Icon: null,
            PreferredModelId: null,
            AllowedModelIds: null,
            AllowedTools: null,
            InputSchema: null,
            OutputSchema: null,
            Visibility: null,
            AllowModelOverride: null,
            SortOrder: null);

        var handler = new UpdateAgent.Handler(_repository);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AiAgent CreateAgent(
        string name, string displayName, AgentCategory category,
        bool isOfficial, int sortOrder = 100) =>
        AiAgent.Register(
            name, displayName, $"Description for {name}",
            category, isOfficial, "System prompt", sortOrder: sortOrder);

    private static AiAgent CreateCustomAgent(string name, string displayName, AgentCategory category) =>
        AiAgent.CreateCustom(
            name, displayName, $"Description for {name}",
            category, "You are a custom agent.", "Assist with tasks",
            AgentOwnershipType.Tenant, AgentVisibility.Tenant, ownerId: "user-1");
}
