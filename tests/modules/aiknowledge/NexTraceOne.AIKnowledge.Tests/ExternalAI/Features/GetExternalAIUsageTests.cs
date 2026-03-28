using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.GetExternalAIUsage;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Features;

public sealed class GetExternalAIUsageTests
{
    private readonly IAiUsageEntryRepository _usageRepository = Substitute.For<IAiUsageEntryRepository>();

    [Fact]
    public async Task Handle_ShouldAggregateTokenUsageCorrectly()
    {
        var conversationA = Guid.NewGuid();
        var conversationB = Guid.NewGuid();

        var entries = new List<AIUsageEntry>
        {
            AIUsageEntry.Record("u1", "User 1", Guid.NewGuid(), "gpt-4o", "openai", false, DateTimeOffset.UtcNow, 100, 50, null, null, UsageResult.Allowed, "ops", AIClientType.Web, "c1", conversationA),
            AIUsageEntry.Record("u1", "User 1", Guid.NewGuid(), "gpt-4o", "openai", false, DateTimeOffset.UtcNow, 200, 100, null, null, UsageResult.Allowed, "ops", AIClientType.Web, "c2", conversationA),
            AIUsageEntry.Record("u1", "User 1", Guid.NewGuid(), "llama3.1:8b", "ollama", true, DateTimeOffset.UtcNow, 80, 20, null, null, UsageResult.Allowed, "ops", AIClientType.Web, "c3", conversationB)
        };

        _usageRepository.ListAsync("u1", null, null, null, null, null, 1_000, Arg.Any<CancellationToken>())
            .Returns(entries);

        var handler = new GetExternalAIUsage.Handler(_usageRepository);
        var query = new GetExternalAIUsage.Query(
            ConversationId: null,
            UserId: "u1",
            From: null,
            To: null,
            Provider: null,
            Model: null,
            TenantId: null,
            EnvironmentId: null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalTokens.Should().Be(550);
        result.Value.InputTokens.Should().Be(380);
        result.Value.OutputTokens.Should().Be(170);
        result.Value.ConversationCount.Should().Be(2);
        result.Value.AverageTokensPerConversation.Should().Be(275);
    }
}
