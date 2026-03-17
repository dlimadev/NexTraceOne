using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Features.ExecuteAiChat;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Features;

/// <summary>Testes unitários do handler ExecuteAiChat — execução real de chat via provider.</summary>
public sealed class ExecuteAiChatTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IAiProviderFactory _providerFactory = Substitute.For<IAiProviderFactory>();
    private readonly IAiModelCatalogService _modelCatalog = Substitute.For<IAiModelCatalogService>();
    private readonly IAiAssistantConversationRepository _conversationRepo = Substitute.For<IAiAssistantConversationRepository>();
    private readonly IAiMessageRepository _messageRepo = Substitute.For<IAiMessageRepository>();
    private readonly IAiUsageEntryRepository _usageRepo = Substitute.For<IAiUsageEntryRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public ExecuteAiChatTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns("user-1");
        _currentUser.Name.Returns("Test User");
        _dateTimeProvider.UtcNow.Returns(FixedNow);
    }

    private ExecuteAiChat.Handler CreateHandler() => new(
        _providerFactory,
        _modelCatalog,
        _conversationRepo,
        _messageRepo,
        _usageRepo,
        _currentUser,
        _dateTimeProvider);

    private static ExecuteAiChat.Command CreateCommand(string message = "Hello AI") =>
        new(null, message, null, null, null, null);

    // ── Handle: error cases ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnError_WhenNoModelAvailable()
    {
        _modelCatalog.ResolveDefaultModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ResolvedModel?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ModelNotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenProviderNotFound()
    {
        var resolved = new ResolvedModel(Guid.NewGuid(), "llama3", "Llama 3", "ollama", "Ollama", true, "chat");
        _modelCatalog.ResolveDefaultModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolved);
        _providerFactory.GetChatProvider("ollama").Returns((IChatCompletionProvider?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderNotFound");
    }

    // ── Handle: success ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldCreateConversationAndMessages_OnSuccess()
    {
        var resolved = new ResolvedModel(Guid.NewGuid(), "llama3", "Llama 3", "ollama", "Ollama", true, "chat");
        _modelCatalog.ResolveDefaultModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolved);

        var chatProvider = Substitute.For<IChatCompletionProvider>();
        chatProvider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(true, "Hello!", "llama3", "ollama", 10, 20, TimeSpan.FromMilliseconds(100)));
        _providerFactory.GetChatProvider("ollama").Returns(chatProvider);

        var handler = CreateHandler();
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("Hello!");
        result.Value.Success.Should().BeTrue();
        result.Value.IsInternalModel.Should().BeTrue();

        await _conversationRepo.Received(1).AddAsync(Arg.Any<AiAssistantConversation>(), Arg.Any<CancellationToken>());
        await _messageRepo.Received(2).AddAsync(Arg.Any<AiMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRecordUsage_OnSuccess()
    {
        var resolved = new ResolvedModel(Guid.NewGuid(), "llama3", "Llama 3", "ollama", "Ollama", true, "chat");
        _modelCatalog.ResolveDefaultModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolved);

        var chatProvider = Substitute.For<IChatCompletionProvider>();
        chatProvider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(true, "Response", "llama3", "ollama", 5, 15, TimeSpan.FromMilliseconds(50)));
        _providerFactory.GetChatProvider("ollama").Returns(chatProvider);

        var handler = CreateHandler();
        await handler.Handle(CreateCommand(), CancellationToken.None);

        await _usageRepo.Received(1).AddAsync(
            Arg.Is<Domain.Governance.Entities.AIUsageEntry>(u =>
                u.ModelName == "llama3" && u.PromptTokens == 5 && u.CompletionTokens == 15),
            Arg.Any<CancellationToken>());
    }
}
