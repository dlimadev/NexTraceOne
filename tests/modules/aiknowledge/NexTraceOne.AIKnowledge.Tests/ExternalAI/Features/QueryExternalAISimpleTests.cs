using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAISimple;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Features;

/// <summary>
/// Testes unitários do handler QueryExternalAISimple.
/// Valida: roteamento de query, detecção de fallback, falha do provider.
/// </summary>
public sealed class QueryExternalAISimpleTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<QueryExternalAISimple.Handler> _logger = Substitute.For<ILogger<QueryExternalAISimple.Handler>>();

    private QueryExternalAISimple.Handler CreateHandler() =>
        new(_routingPort, _dateTimeProvider, _logger);

    [Fact]
    public async Task Handle_ShouldReturnAiResponse_WhenProviderResponds()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync("general", "What is NexTraceOne?", null, Arg.Any<CancellationToken>())
            .Returns("NexTraceOne is a unified service governance platform.");

        var command = new QueryExternalAISimple.Command(
            Query: "What is NexTraceOne?",
            ContextScope: null,
            SystemContext: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Be("NexTraceOne is a unified service governance platform.");
        result.Value.IsFallback.Should().BeFalse();
        result.Value.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_ShouldDetectFallback_WhenResponseStartsWithFallbackPrefix()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        const string fallbackResponse = "[FALLBACK_PROVIDER_UNAVAILABLE] Provider is down. Query: What is NexTraceOne?";
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(fallbackResponse);

        var command = new QueryExternalAISimple.Command(
            Query: "What is NexTraceOne?",
            ContextScope: null,
            SystemContext: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsFallback.Should().BeTrue();
        result.Value.Content.Should().StartWith("[FALLBACK_PROVIDER_UNAVAILABLE]");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenProviderThrows()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("Connection refused")));

        var command = new QueryExternalAISimple.Command(
            Query: "What is NexTraceOne?",
            ContextScope: null,
            SystemContext: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("AIKnowledge.Provider.Unavailable");
    }

    [Fact]
    public async Task Handle_ShouldUseSystemContext_AsGroundingContext_WhenProvided()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync("service context", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Response with context");

        var command = new QueryExternalAISimple.Command(
            Query: "Explain this service",
            ContextScope: null,
            SystemContext: "service context",
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _routingPort.Received(1).RouteQueryAsync("service context", "Explain this service", null, Arg.Any<CancellationToken>());
    }
}
