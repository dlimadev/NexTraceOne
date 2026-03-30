using System.Linq;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultAgents;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class SeedDefaultAgentsTests
{
    private readonly IAiAgentRepository _agentRepository = Substitute.For<IAiAgentRepository>();

    private SeedDefaultAgents.Handler CreateHandler() =>
        new(_agentRepository);

    [Fact]
    public async Task Handle_EmptyRegistry_SeedsAllCatalogAgents()
    {
        // Arrange
        _agentRepository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiAgent>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new SeedDefaultAgents.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AgentsSeeded.Should().Be(DefaultAgentCatalog.GetAll().Count);
        result.Value.TotalInCatalog.Should().Be(DefaultAgentCatalog.GetAll().Count);

        await _agentRepository.Received(DefaultAgentCatalog.GetAll().Count)
            .AddAsync(Arg.Any<AiAgent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllAgentsExist_SeedsNothing()
    {
        // Arrange
        var existingAgents = DefaultAgentCatalog.GetAll()
            .Select(def => AiAgent.Register(
                def.Name, def.DisplayName, def.Description,
                def.Category, true, def.SystemPrompt,
                capabilities: def.Capabilities,
                targetPersona: def.TargetPersona,
                icon: def.Icon,
                sortOrder: def.SortOrder))
            .ToList();

        _agentRepository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(existingAgents);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new SeedDefaultAgents.Command(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AgentsSeeded.Should().Be(0);
        result.Value.TotalInCatalog.Should().Be(DefaultAgentCatalog.GetAll().Count);

        await _agentRepository.DidNotReceive()
            .AddAsync(Arg.Any<AiAgent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PartialAgentsExist_SeedsOnlyMissing()
    {
        // Arrange — create only the first agent from catalog
        var firstDef = DefaultAgentCatalog.GetAll()[0];
        var existingAgents = new[]
        {
            AiAgent.Register(
                firstDef.Name, firstDef.DisplayName, firstDef.Description,
                firstDef.Category, true, firstDef.SystemPrompt)
        };

        _agentRepository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(existingAgents);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new SeedDefaultAgents.Command(), CancellationToken.None);

        // Assert
        var expectedSeeded = DefaultAgentCatalog.GetAll().Count - 1;
        result.IsSuccess.Should().BeTrue();
        result.Value.AgentsSeeded.Should().Be(expectedSeeded);

        await _agentRepository.Received(expectedSeeded)
            .AddAsync(Arg.Any<AiAgent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CaseInsensitive_AgentNameMatch()
    {
        // Arrange — create agent with different casing
        var firstDef = DefaultAgentCatalog.GetAll()[0];
        var existingAgents = new[]
        {
            AiAgent.Register(
                firstDef.Name.ToUpperInvariant(), firstDef.DisplayName, firstDef.Description,
                firstDef.Category, true, firstDef.SystemPrompt)
        };

        _agentRepository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(existingAgents);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new SeedDefaultAgents.Command(), CancellationToken.None);

        // Assert — should not duplicate despite different casing
        var expectedSeeded = DefaultAgentCatalog.GetAll().Count - 1;
        result.IsSuccess.Should().BeTrue();
        result.Value.AgentsSeeded.Should().Be(expectedSeeded);
    }

    [Fact]
    public async Task Handle_IsIdempotent_SecondCallSeedsNothing()
    {
        // Arrange
        var capturedAgents = new List<AiAgent>();
        _agentRepository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromResult<IReadOnlyList<AiAgent>>(Array.Empty<AiAgent>()),
                _ => Task.FromResult<IReadOnlyList<AiAgent>>(capturedAgents));

        _agentRepository.When(r => r.AddAsync(Arg.Any<AiAgent>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedAgents.Add(callInfo.Arg<AiAgent>()));

        var handler = CreateHandler();

        // Act — first call
        var result1 = await handler.Handle(new SeedDefaultAgents.Command(), CancellationToken.None);

        // Act — second call
        var result2 = await handler.Handle(new SeedDefaultAgents.Command(), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result1.Value.AgentsSeeded.Should().BeGreaterThan(0);

        result2.IsSuccess.Should().BeTrue();
        result2.Value.AgentsSeeded.Should().Be(0, "second call should be idempotent");
    }

    [Fact]
    public async Task Handle_CreatesOfficialAgents()
    {
        // Arrange
        _agentRepository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiAgent>());

        AiAgent? capturedAgent = null;
        _agentRepository.When(r => r.AddAsync(Arg.Any<AiAgent>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedAgent ??= callInfo.Arg<AiAgent>());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new SeedDefaultAgents.Command(), CancellationToken.None);

        // Assert
        capturedAgent.Should().NotBeNull();
        capturedAgent!.IsOfficial.Should().BeTrue("seeded agents should be official");
        capturedAgent.IsActive.Should().BeTrue("seeded agents should be active");
        capturedAgent.OwnershipType.Should().Be(AgentOwnershipType.System, "seeded agents should be System-owned");
        capturedAgent.PublicationStatus.Should().Be(AgentPublicationStatus.Published, "seeded agents should be Published");
    }
}
