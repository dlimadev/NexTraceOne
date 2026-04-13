using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para ConversationPersistenceService.
/// Valida que mensagens do utilizador e do assistente, conversas e estado são
/// corretamente geridos pelo serviço de persistência extraído do handler.
/// </summary>
public sealed class ConversationPersistenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IAiAssistantConversationRepository _convRepo = Substitute.For<IAiAssistantConversationRepository>();
    private readonly IAiMessageRepository _msgRepo = Substitute.For<IAiMessageRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ConversationPersistenceTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    private ConversationPersistenceService CreateService() =>
        new(_convRepo, _msgRepo, _clock);

    private static AiAssistantConversation MakeActiveConversation(string ownerId = "user-persist-001")
        => AiAssistantConversation.Start(
            "Test conversation", "Engineer", AIClientType.Web, "services", ownerId);

    // ── GetOrCreate: new conversation ────────────────────────────────────

    [Fact]
    public async Task GetOrCreateAsync_WhenNoConversationId_CreatesNewConversation()
    {
        var (conv, error) = await CreateService().GetOrCreateAsync(
            conversationId: null,
            userId: "user-001",
            userEmail: "user@test.io",
            message: "Hello assistant",
            persona: "Engineer",
            clientType: AIClientType.Web,
            contextScope: "services",
            serviceId: null, contractId: null, incidentId: null, changeId: null, teamId: null);

        error.Should().BeNull();
        conv.Should().NotBeNull();
        conv!.IsActive.Should().BeTrue();
        await _convRepo.Received(1).AddAsync(Arg.Any<AiAssistantConversation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenNoConversationId_TruncatesTitleAt100Chars()
    {
        var longMessage = new string('A', 150);
        var (conv, _) = await CreateService().GetOrCreateAsync(
            null, "user-001", null, longMessage, "Engineer", AIClientType.Web,
            null, null, null, null, null, null);

        conv!.Title.Length.Should().BeLessThanOrEqualTo(100);
        conv.Title.Should().EndWith("...");
    }

    // ── GetOrCreate: existing conversation ───────────────────────────────

    [Fact]
    public async Task GetOrCreateAsync_WhenConversationIdExists_ReturnsExisting()
    {
        var existing = MakeActiveConversation("user-001");
        _convRepo.GetByIdAsync(Arg.Any<AiAssistantConversationId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var (conv, error) = await CreateService().GetOrCreateAsync(
            existing.Id.Value, "user-001", null, "Follow-up", "Engineer",
            AIClientType.Web, null, null, null, null, null, null);

        error.Should().BeNull();
        conv.Should().Be(existing);
        await _convRepo.DidNotReceive().AddAsync(Arg.Any<AiAssistantConversation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenConversationNotFound_ReturnsError()
    {
        _convRepo.GetByIdAsync(Arg.Any<AiAssistantConversationId>(), Arg.Any<CancellationToken>())
            .Returns((AiAssistantConversation?)null);

        var (conv, error) = await CreateService().GetOrCreateAsync(
            Guid.NewGuid(), "user-001", null, "Follow-up", "Engineer",
            AIClientType.Web, null, null, null, null, null, null);

        conv.Should().BeNull();
        error.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenConversationBelongsToOtherUser_ReturnsAccessDenied()
    {
        var existing = MakeActiveConversation("other-user");
        _convRepo.GetByIdAsync(Arg.Any<AiAssistantConversationId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var (conv, error) = await CreateService().GetOrCreateAsync(
            existing.Id.Value, "user-001", "user@email.io", "Follow-up", "Engineer",
            AIClientType.Web, null, null, null, null, null, null);

        conv.Should().BeNull();
        error.Should().NotBeNull();
        error!.Code.Should().Be("AiGovernance.Conversation.AccessDenied");
    }

    // ── PersistMessagePair ────────────────────────────────────────────────

    [Fact]
    public async Task PersistMessagePairAsync_PersistsBothUserAndAssistantMessages()
    {
        var conv = MakeActiveConversation();

        await CreateService().PersistMessagePairAsync(
            conv, "User question", FixedNow,
            "Assistant answer", "model-1", "provider-1", true,
            50, 100, null, "Service Catalog", "scope:services",
            "corr-123", isDegraded: false);

        await _msgRepo.Received(2).AddAsync(Arg.Any<AiMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PersistMessagePairAsync_UpdatesConversation()
    {
        var conv = MakeActiveConversation();

        await CreateService().PersistMessagePairAsync(
            conv, "User question", FixedNow,
            "Assistant answer", "model-1", "provider-1", true,
            50, 100, null, "Service Catalog", "scope:services",
            "corr-123", isDegraded: false);

        await _convRepo.Received(1).UpdateAsync(conv, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PersistMessagePairAsync_ReturnsCorrectMetadata()
    {
        var conv = MakeActiveConversation();
        const string modelName = "llama3:8b";
        const string providerId = "ollama";
        const int promptTokens = 42;
        const int completionTokens = 88;

        var result = await CreateService().PersistMessagePairAsync(
            conv, "question", FixedNow, "answer", modelName, providerId, true,
            promptTokens, completionTokens, "policy-x",
            "Service Catalog,Contract Registry", "scope:services",
            "corr-abc", isDegraded: false);

        result.ConversationId.Should().Be(conv.Id.Value);
        result.UserMessageId.Should().NotBeEmpty();
        result.AssistantMessageId.Should().NotBeEmpty();
        result.ConversationTitle.Should().Be(conv.Title);
        result.IsDegraded.Should().BeFalse();
    }

    [Fact]
    public async Task PersistMessagePairAsync_WhenDegraded_SetsDegradedFlag()
    {
        var conv = MakeActiveConversation();

        var result = await CreateService().PersistMessagePairAsync(
            conv, "question", FixedNow,
            $"{AiMessage.DeterministicFallbackPrefix} Provider unavailable.",
            "deterministic-fallback", "system-fallback", true,
            10, 20, null, "Service Catalog", string.Empty,
            "corr-xyz", isDegraded: true);

        result.IsDegraded.Should().BeTrue();
    }

    // ── Owner matching ────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateAsync_WhenOwnerMatchesByEmail_AllowsAccess()
    {
        const string email = "owner@nextraceone.io";
        var existing = AiAssistantConversation.Start(
            "Test", "Engineer", AIClientType.Web, "general", email);
        _convRepo.GetByIdAsync(Arg.Any<AiAssistantConversationId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var (conv, error) = await CreateService().GetOrCreateAsync(
            existing.Id.Value, "different-id", email, "Question",
            "Engineer", AIClientType.Web, null, null, null, null, null, null);

        error.Should().BeNull();
        conv.Should().Be(existing);
    }
}
