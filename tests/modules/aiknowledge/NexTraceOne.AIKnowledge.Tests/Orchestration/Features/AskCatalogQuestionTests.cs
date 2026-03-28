using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.AskCatalogQuestion;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes unitários do handler AskCatalogQuestion.
/// Valida: resposta de IA, detecção de fallback, falha do provider.
/// </summary>
public sealed class AskCatalogQuestionTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<AskCatalogQuestion.Handler> _logger = Substitute.For<ILogger<AskCatalogQuestion.Handler>>();

    private AskCatalogQuestion.Handler CreateHandler() =>
        new(_routingPort, _dateTimeProvider, _logger);

    [Fact]
    public async Task Handle_ShouldReturnAiResponse_WhenProviderResponds()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("The PaymentService handles payment processing for the platform.");

        var command = new AskCatalogQuestion.Command(
            Question: "What does this service do?",
            EntityType: "service",
            EntityName: "PaymentService",
            EntityDescription: "Handles payments",
            Properties: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Answer.Should().Be("The PaymentService handles payment processing for the platform.");
        result.Value.EntityType.Should().Be("service");
        result.Value.EntityName.Should().Be("PaymentService");
        result.Value.IsFallback.Should().BeFalse();
        result.Value.CorrelationId.Should().NotBeNullOrWhiteSpace();
        result.Value.GroundingSources.Should().Contain("service");
        result.Value.GroundingSources.Should().Contain("PaymentService");
    }

    [Fact]
    public async Task Handle_ShouldDetectFallback_WhenResponseStartsWithFallbackPrefix()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        const string fallbackResponse = "[FALLBACK_PROVIDER_UNAVAILABLE] AI provider is unavailable. Query: What does this service do?";
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(fallbackResponse);

        var command = new AskCatalogQuestion.Command(
            Question: "What does this service do?",
            EntityType: "service",
            EntityName: "PaymentService",
            EntityDescription: null,
            Properties: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsFallback.Should().BeTrue();
        result.Value.Answer.Should().StartWith("[FALLBACK_PROVIDER_UNAVAILABLE]");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenProviderThrows()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("Connection refused")));

        var command = new AskCatalogQuestion.Command(
            Question: "What does this contract define?",
            EntityType: "contract",
            EntityName: "PaymentAPI",
            EntityDescription: null,
            Properties: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("AIKnowledge.Provider.Unavailable");
    }

    [Fact]
    public async Task Handle_ShouldIncludeProperties_InGroundingPrompt()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Response with properties context");

        var command = new AskCatalogQuestion.Command(
            Question: "What SLA applies?",
            EntityType: "service",
            EntityName: "OrderService",
            EntityDescription: null,
            Properties: new Dictionary<string, string>
            {
                ["sla"] = "99.9%",
                ["team"] = "payments-team"
            },
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Verify grounding context includes properties
        await _routingPort.Received(1).RouteQueryAsync(
            Arg.Is<string>(ctx => ctx.Contains("sla") && ctx.Contains("99.9%")),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}
