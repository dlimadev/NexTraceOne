using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GetAgentMarketplace;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes para marketplace de agents com ratings agregados via IAiFeedbackRepository.
/// Valida: retorno de AverageRating correto, handling de agents sem feedback (0.0).
/// </summary>
public sealed class GetAgentMarketplaceWithRatingsTests
{
    private readonly IAiAgentRepository _agentRepository = Substitute.For<IAiAgentRepository>();
    private readonly IAiFeedbackRepository _feedbackRepository = Substitute.For<IAiFeedbackRepository>();

    private GetAgentMarketplace.Handler CreateSut() =>
        new(_agentRepository, _feedbackRepository);

    // ── 1. Marketplace returns average rating per agent ───────────────────

    [Fact]
    public async Task Marketplace_Returns_Average_Rating_Per_Agent()
    {
        // Arrange
        var agent = CreateAgent("contract-agent", "Contract Design Agent");
        _agentRepository.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns(new List<AiAgent> { agent } as IReadOnlyList<AiAgent>);

        // Average of Positive(1) and Positive(1) = 1.0
        _feedbackRepository.GetAverageRatingAsync(agent.Id.Value, Arg.Any<CancellationToken>())
            .Returns(1.0);

        var query = new GetAgentMarketplace.Query(
            Category: null,
            Search: null,
            IsOfficial: null,
            Page: 1,
            PageSize: 20);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].AverageRating.Should().Be(1.0);
    }

    // ── 2. Marketplace handles agent with no feedback (defaults to 0.0) ───

    [Fact]
    public async Task Marketplace_Returns_Zero_Rating_When_No_Feedback_Exists()
    {
        // Arrange
        var agent = CreateAgent("incident-agent", "Incident Analysis Agent");
        _agentRepository.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns(new List<AiAgent> { agent } as IReadOnlyList<AiAgent>);

        // No feedback → repository returns 0.0
        _feedbackRepository.GetAverageRatingAsync(agent.Id.Value, Arg.Any<CancellationToken>())
            .Returns(0.0);

        var query = new GetAgentMarketplace.Query(
            Category: null,
            Search: null,
            IsOfficial: null,
            Page: 1,
            PageSize: 20);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].AverageRating.Should().Be(0.0);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AiAgent CreateAgent(string name, string displayName)
        => AiAgent.Register(
            name: name,
            displayName: displayName,
            description: "Test agent",
            category: AgentCategory.General,
            isOfficial: true,
            systemPrompt: "You are a helpful agent.");
}
