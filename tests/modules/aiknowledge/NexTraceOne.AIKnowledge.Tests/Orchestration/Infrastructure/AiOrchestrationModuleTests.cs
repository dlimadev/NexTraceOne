using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Contracts.Orchestration.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Services;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Infrastructure;

public sealed class AiOrchestrationModuleTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 28, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetConversationAsync_WhenConversationExists_ShouldReturnSummaryMetadata()
    {
        await using var db = CreateDbContext();
        var conversation = AiConversation.Start("orders", "incident diagnosis", "alice", FixedNow);
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetConversationAsync(conversation.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(conversation.Id.Value);
        result.ServiceName.Should().Be("orders");
        result.OwnerUserId.Should().Be("alice");
        result.Title.Should().Be("incident diagnosis");
    }

    [Fact]
    public async Task GetConversationAsync_WhenConversationDoesNotExist_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetConversationAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConversationsByServiceAsync_ShouldReturnMostRecentConversationsForServiceOnly()
    {
        await using var db = CreateDbContext();
        var oldConversation = AiConversation.Start("orders", "old topic", "alice", FixedNow.AddMinutes(-20));
        var latestConversation = AiConversation.Start("orders", "latest topic", "bob", FixedNow.AddMinutes(-5));
        var otherServiceConversation = AiConversation.Start("billing", "billing topic", "carol", FixedNow);
        db.Conversations.AddRange(oldConversation, latestConversation, otherServiceConversation);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetConversationsByServiceAsync("orders", 10, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(latestConversation.Id.Value);
        result[1].Id.Should().Be(oldConversation.Id.Value);
        result.Should().OnlyContain(r => r.ServiceName == "orders");
    }

    [Fact]
    public async Task GetConversationsByServiceAsync_ShouldRespectLimit()
    {
        await using var db = CreateDbContext();
        db.Conversations.Add(AiConversation.Start("orders", "topic-1", "alice", FixedNow.AddMinutes(-30)));
        db.Conversations.Add(AiConversation.Start("orders", "topic-2", "alice", FixedNow.AddMinutes(-20)));
        db.Conversations.Add(AiConversation.Start("orders", "topic-3", "alice", FixedNow.AddMinutes(-10)));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetConversationsByServiceAsync("orders", 2, CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAgentExecutionResultAsync_WhenArtifactExists_ShouldReturnArtifactSummary()
    {
        await using var db = CreateDbContext();
        var generateResult = GeneratedTestArtifact.Generate(
            releaseId: Guid.NewGuid(),
            serviceName: "orders",
            testFramework: "xunit",
            generatedCode: "// test",
            confidence: 0.9m,
            generatedAt: FixedNow);

        generateResult.IsSuccess.Should().BeTrue();
        var artifact = generateResult.Value;
        db.TestArtifacts.Add(artifact);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetAgentExecutionResultAsync(artifact.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(artifact.Id.Value);
        result.ServiceName.Should().Be("orders");
        result.Status.Should().Be("Draft");
        result.AgentType.Should().Be("test-generation:xunit");
    }

    [Fact]
    public async Task GetAgentExecutionResultAsync_WhenContextExists_ShouldReturnContextSummary()
    {
        await using var db = CreateDbContext();
        var context = AiContext.Assemble("orders", "change-analysis", "{\"change\":\"ok\"}", FixedNow);
        db.Contexts.Add(context);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetAgentExecutionResultAsync(context.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(context.Id.Value);
        result.ServiceName.Should().Be("orders");
        result.AgentType.Should().Be("change-analysis");
        result.TokensUsed.Should().Be(context.TokenEstimate);
    }

    [Fact]
    public async Task GetAgentExecutionResultAsync_WhenExecutionDoesNotExist_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetAgentExecutionResultAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    private static AiOrchestrationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AiOrchestrationDbContext>()
            .UseInMemoryDatabase($"ai-orchestration-module-tests-{Guid.NewGuid():N}")
            .Options;

        return new AiOrchestrationDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

    private static IAiOrchestrationModule CreateSut(AiOrchestrationDbContext db)
        => new AiOrchestrationModule(db);

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        public string Slug => "tests";
        public string Name => "Tests";
        public bool IsActive => true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id => "aik-tests-user";
        public string Name => "AIKnowledge Tests";
        public string Email => "aik.tests@nextraceone.local";
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => FixedNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}
