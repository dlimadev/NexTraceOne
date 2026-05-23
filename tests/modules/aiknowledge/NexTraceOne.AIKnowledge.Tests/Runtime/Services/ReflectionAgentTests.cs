using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ReflectionAgent;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

public sealed class ReflectionAgentTests
{
    private readonly IAiKernelService _kernelService = Substitute.For<IAiKernelService>();
    private readonly IAiProviderFactory _providerFactory = Substitute.For<IAiProviderFactory>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<ReflectionAgent.Handler> _logger = NullLogger<ReflectionAgent.Handler>.Instance;

    public ReflectionAgentTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private ReflectionAgent.Handler CreateHandler() => new(_kernelService, _providerFactory, _clock, _logger);

    [Fact]
    public async Task Handle_SingleIteration_CompletesOnHighScore()
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns("ollama");
        _providerFactory.GetChatProvider("ollama").Returns(provider);

        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);

        // Plan
        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var systemPrompt = callInfo.ArgAt<string>(1);
                if (systemPrompt.Contains("planning expert"))
                    return "1. Analyze requirements\n2. Generate solution";
                if (systemPrompt.Contains("execution agent"))
                    return "The solution is to use a caching layer with Redis.";
                if (systemPrompt.Contains("critical evaluator"))
                    return "{\"score\":85,\"reflection\":\"Good solution, covers the main requirement.\",\"decision\":\"complete\"}";
                return "unknown";
            });

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ReflectionAgent.Command("How to improve API performance?", MaxIterations: 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FinalOutput.Should().Contain("caching layer");
        result.Value.FinalScore.Should().Be(85);
        result.Value.IterationCount.Should().Be(1);
        result.Value.WasRevised.Should().BeFalse();
        result.Value.History.Should().HaveCount(1);
        result.Value.History[0].Score.Should().Be(85);
        result.Value.History[0].Decision.Should().Be("Complete");
    }

    [Fact]
    public async Task Handle_TwoIterations_RevisesAndThenCompletes()
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns("ollama");
        _providerFactory.GetChatProvider("ollama").Returns(provider);

        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);

        var callCount = 0;
        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var systemPrompt = callInfo.ArgAt<string>(1);
                callCount++;

                if (systemPrompt.Contains("planning expert"))
                    return callCount <= 2
                        ? "1. Basic approach"
                        : "1. Improved approach with caching\n2. Add compression";

                if (systemPrompt.Contains("execution agent"))
                    return callCount <= 3
                        ? "Use basic in-memory cache."
                        : "Use Redis with compression for optimal performance.";

                if (systemPrompt.Contains("critical evaluator"))
                {
                    return callCount <= 4
                        ? "{\"score\":60,\"reflection\":\"Missing compression and distributed caching.\",\"decision\":\"revise\"}"
                        : "{\"score\":90,\"reflection\":\"Excellent, covers all requirements.\",\"decision\":\"complete\"}";
                }

                return "unknown";
            });

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ReflectionAgent.Command("How to improve API performance?", MaxIterations: 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FinalOutput.Should().Contain("Redis");
        result.Value.FinalScore.Should().Be(90);
        result.Value.IterationCount.Should().Be(2);
        result.Value.WasRevised.Should().BeTrue();
        result.Value.History.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoProvider_ReturnsFallback()
    {
        _providerFactory.GetChatProvider(Arg.Any<string>()).Returns((IChatCompletionProvider?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ReflectionAgent.Command("Test task"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FinalOutput.Should().Contain("Unable to process");
        result.Value.IterationCount.Should().Be(0);
        result.Value.FinalScore.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MaxIterations_ReachesLimitAndReturnsBest()
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns("ollama");
        _providerFactory.GetChatProvider("ollama").Returns(provider);

        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);

        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var systemPrompt = callInfo.ArgAt<string>(1);
                if (systemPrompt.Contains("planning expert"))
                    return "1. Do something";
                if (systemPrompt.Contains("execution agent"))
                    return "Result.";
                if (systemPrompt.Contains("critical evaluator"))
                    return "{\"score\":55,\"reflection\":\"Not good enough.\",\"decision\":\"revise\"}";
                return "unknown";
            });

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ReflectionAgent.Command("Hard task", MaxIterations: 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IterationCount.Should().Be(2);
        result.Value.FinalScore.Should().Be(55);
    }
}
