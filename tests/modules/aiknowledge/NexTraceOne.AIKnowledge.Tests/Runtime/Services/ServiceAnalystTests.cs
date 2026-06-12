using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ServiceAnalyst;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

public sealed class ServiceAnalystTests
{
    private readonly IAiKernelService _kernelService = Substitute.For<IAiKernelService>();
    private readonly IAiExecutionGateway _aiGateway = Substitute.For<IAiExecutionGateway>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<ServiceAnalyst.Handler> _logger = NullLogger<ServiceAnalyst.Handler>.Instance;

    public ServiceAnalystTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _aiGateway.PreviewExecutionAsync(Arg.Any<AiExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiExecutionPlan(
                ProviderType: AiProviderType.Internal,
                ProviderId: "ollama",
                ModelId: "ollama",
                ModelDisplayName: "Ollama",
                IsAvailable: true,
                UnavailabilityReason: null,
                EstimatedCost: null,
                AppliedPolicies: []));
    }

    private ServiceAnalyst.Handler CreateHandler() => new(_kernelService, _aiGateway, _clock, _logger);

    [Fact]
    public async Task Handle_ValidService_ReturnsStructuredResponse()
    {


        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);
        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("""
                {
                  "overallStatus": "Degraded",
                  "healthScore": 65,
                  "bottlenecks": [
                    { "area": "Database", "description": "Slow queries", "severity": "High", "suggestedFix": "Add indexes" }
                  ],
                  "criticalDependencies": [
                    { "dependencyName": "auth-service", "dependencyType": "Internal", "impactLevel": "High", "alternative": "Local JWT validation" }
                  ],
                  "recommendations": [
                    { "priority": 1, "action": "Add database indexes", "expectedBenefit": "Reduce query latency", "effort": "Low" }
                  ],
                  "summary": "Service has degraded performance due to database bottlenecks."
                }
                """);

        var handler = CreateHandler();
        var result = await handler.Handle(new ServiceAnalyst.Command("High latency on checkout API", "checkout-service"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be("Degraded");
        result.Value.HealthScore.Should().Be(65);
        result.Value.Bottlenecks.Should().HaveCount(1);
        result.Value.Bottlenecks[0].Area.Should().Be("Database");
        result.Value.CriticalDependencies.Should().HaveCount(1);
        result.Value.CriticalDependencies[0].DependencyName.Should().Be("auth-service");
        result.Value.Recommendations.Should().HaveCount(1);
        result.Value.Recommendations[0].Priority.Should().Be(1);
        result.Value.Summary.Should().Contain("database bottlenecks");

        kernel.Data.Should().ContainKey("GroundingQuery");
        kernel.Data["GroundingQuery"].Should().Be("checkout-service High latency on checkout API");
    }

    [Fact]
    public async Task Handle_NoProvider_ReturnsNotAvailableError()
    {
        _aiGateway.PreviewExecutionAsync(Arg.Any<AiExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiExecutionPlan(
                ProviderType: AiProviderType.Null,
                ProviderId: "null",
                ModelId: "null",
                ModelDisplayName: "Null",
                IsAvailable: false,
                UnavailabilityReason: "No provider available",
                EstimatedCost: null,
                AppliedPolicies: []));

        var handler = CreateHandler();
        var result = await handler.Handle(new ServiceAnalyst.Command("Service is slow"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.NotAvailable");
    }

    [Fact]
    public async Task Handle_InvalidJson_ReturnsFallbackResponse()
    {
        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);
        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("This is not valid JSON");

        var handler = CreateHandler();
        var result = await handler.Handle(new ServiceAnalyst.Command("Latency spike"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be("Unknown");
        result.Value.Bottlenecks.Should().HaveCount(1);
        result.Value.Bottlenecks[0].Description.Should().Contain("manual review");
    }

    [Fact]
    public async Task Handle_MetricsSnapshot_IncludedInPrompt()
    {
        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);
        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("""
                {"overallStatus":"Healthy","healthScore":95,"bottlenecks":[],"criticalDependencies":[],"recommendations":[],"summary":"All good"}
                """);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ServiceAnalyst.Command("Checking status", "api-gateway", "CPU: 45%, Memory: 60%"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HealthScore.Should().Be(95);
    }
}
