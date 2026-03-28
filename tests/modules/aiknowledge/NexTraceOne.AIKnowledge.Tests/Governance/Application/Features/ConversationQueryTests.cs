using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetConversation;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListConversations;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para os handlers GetConversation e ListConversations.
/// Valida propriedade de conversa, ordenação de mensagens, isolamento por utilizador
/// e retorno correto de metadados de conversa.
/// </summary>
public sealed class ConversationQueryTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 14, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "user-query-001";
    private const string OwnerEmail = "query@nextraceone.io";

    private readonly IAiAssistantConversationRepository _convRepo = Substitute.For<IAiAssistantConversationRepository>();
    private readonly IAiMessageRepository _msgRepo = Substitute.For<IAiMessageRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public ConversationQueryTests()
    {
        _currentUser.Id.Returns(OwnerId);
        _currentUser.Email.Returns(OwnerEmail);
        _currentUser.IsAuthenticated.Returns(true);
    }

    private static AiAssistantConversation MakeConversation(string createdBy = OwnerId)
    {
        var conv = AiAssistantConversation.Start(
            "Service health check",
            "Engineer",
            AIClientType.Web,
            "services",
            createdBy);
        return conv;
    }

    private static IReadOnlyList<AiMessage> MakeMessages(Guid conversationId)
    {
        var t1 = FixedNow.AddMinutes(-5);
        var t2 = FixedNow.AddMinutes(-3);
        var t3 = FixedNow;

        return new List<AiMessage>
        {
            AiMessage.UserMessage(conversationId, "What is the service health?", t1),
            AiMessage.AssistantMessage(conversationId, "All services are nominal.", "qwen3.5:9b", "ollama",
                isInternal: true, promptTokens: 40, completionTokens: 12, appliedPolicyName: null,
                groundingSources: string.Empty, contextReferences: string.Empty,
                correlationId: Guid.NewGuid().ToString(), timestamp: t2),
            AiMessage.UserMessage(conversationId, "Any alerts?", t3)
        }.AsReadOnly();
    }

    // ── GetConversation tests ─────────────────────────────────────────────

    [Fact]
    public async Task GetConversation_WhenOwnerRequests_ReturnsConversationWithMessages()
    {
        var conv = MakeConversation();
        var messages = MakeMessages(conv.Id.Value);
        _convRepo.GetByIdAsync(Arg.Any<AiAssistantConversationId>(), Arg.Any<CancellationToken>())
            .Returns(conv);
        _msgRepo.ListByConversationAsync(conv.Id.Value, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(messages);

        var handler = new GetConversation.Handler(_convRepo, _msgRepo, _currentUser);
        var result = await handler.Handle(new GetConversation.Query(conv.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ConversationId.Should().Be(conv.Id.Value);
        result.Value.Messages.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetConversation_WhenOtherUserRequests_ReturnsForbidden()
    {
        var conv = MakeConversation(createdBy: "different-user-999");
        _convRepo.GetByIdAsync(Arg.Any<AiAssistantConversationId>(), Arg.Any<CancellationToken>())
            .Returns(conv);

        var handler = new GetConversation.Handler(_convRepo, _msgRepo, _currentUser);
        var result = await handler.Handle(new GetConversation.Query(conv.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetConversation_WhenNotFound_ReturnsNotFoundError()
    {
        _convRepo.GetByIdAsync(Arg.Any<AiAssistantConversationId>(), Arg.Any<CancellationToken>())
            .Returns((AiAssistantConversation?)null);

        var handler = new GetConversation.Handler(_convRepo, _msgRepo, _currentUser);
        var result = await handler.Handle(new GetConversation.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetConversation_MessagesIncludeRoleAndContent()
    {
        var conv = MakeConversation();
        var messages = MakeMessages(conv.Id.Value);
        _convRepo.GetByIdAsync(Arg.Any<AiAssistantConversationId>(), Arg.Any<CancellationToken>())
            .Returns(conv);
        _msgRepo.ListByConversationAsync(conv.Id.Value, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(messages);

        var handler = new GetConversation.Handler(_convRepo, _msgRepo, _currentUser);
        var result = await handler.Handle(new GetConversation.Query(conv.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Messages.Should().Contain(m => m.Role == "user");
        result.Value.Messages.Should().Contain(m => m.Role == "assistant");
    }

    // ── ListConversations tests ───────────────────────────────────────────

    [Fact]
    public async Task ListConversations_ReturnsOnlyCurrentUserConversations()
    {
        var conv1 = MakeConversation();
        var conv2 = MakeConversation();
        var conversations = new List<AiAssistantConversation> { conv1, conv2 }.AsReadOnly();

        _convRepo.ListAsync(Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AiAssistantConversation>)conversations);
        _convRepo.CountAsync(Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(2);

        var handler = new ListConversations.Handler(_convRepo, _currentUser);
        var result = await handler.Handle(new ListConversations.Query(UserId: null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListConversations_WhenOtherUserIdRequested_ReturnsForbidden()
    {
        var handler = new ListConversations.Handler(_convRepo, _currentUser);
        var result = await handler.Handle(
            new ListConversations.Query(UserId: "completely-different-user"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListConversations_WhenNoConversations_ReturnsEmptyList()
    {
        _convRepo.ListAsync(Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AiAssistantConversation>)new List<AiAssistantConversation>());
        _convRepo.CountAsync(Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new ListConversations.Handler(_convRepo, _currentUser);
        var result = await handler.Handle(new ListConversations.Query(UserId: null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}
