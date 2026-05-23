using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.IncidentResponder;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

public sealed class IncidentResponderTests
{
    private readonly IAiKernelService _kernelService = Substitute.For<IAiKernelService>();
    private readonly IAiProviderFactory _providerFactory = Substitute.For<IAiProviderFactory>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<IncidentResponder.Handler> _logger = NullLogger<IncidentResponder.Handler>.Instance;

    public IncidentResponderTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private IncidentResponder.Handler CreateHandler() => new(_kernelService, _providerFactory, _clock, _logger);

    [Fact]
    public async Task Handle_ValidIncident_ReturnsStructuredResponse()
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns("ollama");
        _providerFactory.GetChatProvider("ollama").Returns(provider);

        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);
        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("""
                {
                  "rootCause": "Memory leak in cache layer",
                  "severity": "High",
                  "relatedChanges": [
                    { "changeId": "CHG-42", "description": "Added Redis cache", "timeAgo": "3 hours ago", "riskLevel": "Medium" }
                  ],
                  "mitigationSteps": [
                    { "priority": 1, "action": "Restart cache service", "expectedOutcome": "Restore availability", "responsibleTeam": "SRE" }
                  ],
                  "recommendedRunbooks": ["Cache-Outage-Response"],
                  "estimatedMttr": "15 minutes",
                  "escalationRecommended": false
                }
                """);

        var handler = CreateHandler();
        var result = await handler.Handle(new IncidentResponder.Command("Service X is returning 503 errors"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RootCause.Should().Be("Memory leak in cache layer");
        result.Value.Severity.Should().Be("High");
        result.Value.RelatedChanges.Should().HaveCount(1);
        result.Value.MitigationSteps.Should().HaveCount(1);
        result.Value.MitigationSteps[0].Priority.Should().Be(1);
        result.Value.EscalationRecommended.Should().BeFalse();

        kernel.Data.Should().ContainKey("GroundingQuery");
    }

    [Fact]
    public async Task Handle_NoProvider_ReturnsError()
    {
        _providerFactory.GetChatProvider(Arg.Any<string>()).Returns((IChatCompletionProvider?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new IncidentResponder.Command("Incident description"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderNotFound");
    }

    [Fact]
    public async Task Handle_InvalidJson_ReturnsFallbackResponse()
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns("ollama");
        _providerFactory.GetChatProvider("ollama").Returns(provider);

        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);
        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("This is not valid JSON");

        var handler = CreateHandler();
        var result = await handler.Handle(new IncidentResponder.Command("Incident"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RootCause.Should().Contain("Unable to determine");
        result.Value.EscalationRecommended.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ServiceNameIncluded_InGroundingQuery()
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns("ollama");
        _providerFactory.GetChatProvider("ollama").Returns(provider);

        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);
        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("""
                {"rootCause":"test","severity":"Low","relatedChanges":[],"mitigationSteps":[],"recommendedRunbooks":[],"estimatedMttr":"5m","escalationRecommended":false}
                """);

        var handler = CreateHandler();
        await handler.Handle(new IncidentResponder.Command("Latency spike", "payment-service"), CancellationToken.None);

        kernel.Data["GroundingQuery"].Should().Be("Latency spike payment-service");
    }
}
