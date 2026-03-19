using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.ClassifyChangeWithAI;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes unitários do handler ClassifyChangeWithAI.
/// Valida: parsing de classificação, extração de steps de mitigação, detecção de fallback, falha do provider.
/// </summary>
public sealed class ClassifyChangeWithAITests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<ClassifyChangeWithAI.Handler> _logger = Substitute.For<ILogger<ClassifyChangeWithAI.Handler>>();

    private ClassifyChangeWithAI.Handler CreateHandler() =>
        new(_routingPort, _dateTimeProvider, _logger);

    [Fact]
    public async Task Handle_ShouldClassifyAsBreaking_WhenResponseContainsBreaking()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("This is a Breaking change that removes the /users endpoint.\n- Update all consumers\n- Add deprecation notice");

        var command = new ClassifyChangeWithAI.Command(
            ChangeTitle: "Remove /users endpoint",
            ChangeDescription: "Removing legacy users endpoint",
            AffectedService: "UserService",
            CurrentVersion: "1.0.0",
            TargetVersion: "2.0.0",
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuggestedChangeType.Should().Be("BreakingChange");
        result.Value.SuggestedMitigationSteps.Should().HaveCount(2);
        result.Value.SuggestedMitigationSteps[0].Should().Be("Update all consumers");
        result.Value.IsFallback.Should().BeFalse();
        result.Value.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_ShouldClassifyAsSecurity_WhenResponseContainsSecurity()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("This is a Security fix for CVE-2025-0001.\n- Apply patch immediately\n* Rotate API keys");

        var command = new ClassifyChangeWithAI.Command(
            ChangeTitle: "Fix CVE-2025-0001",
            ChangeDescription: null,
            AffectedService: null,
            CurrentVersion: null,
            TargetVersion: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuggestedChangeType.Should().Be("Security");
        result.Value.SuggestedMitigationSteps.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldClassifyAsPatch_WhenResponseContainsFix()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("This is a bug fix for null reference exception.\n- Deploy during off-peak hours");

        var command = new ClassifyChangeWithAI.Command(
            ChangeTitle: "Fix null reference in OrderService",
            ChangeDescription: null,
            AffectedService: null,
            CurrentVersion: null,
            TargetVersion: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuggestedChangeType.Should().Be("Patch");
    }

    [Fact]
    public async Task Handle_ShouldDetectFallback_WhenResponseStartsWithFallbackPrefix()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("[FALLBACK_PROVIDER_UNAVAILABLE] Provider is down. Query: Classify this change");

        var command = new ClassifyChangeWithAI.Command(
            ChangeTitle: "Add new endpoint",
            ChangeDescription: null,
            AffectedService: null,
            CurrentVersion: null,
            TargetVersion: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsFallback.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenProviderThrows()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("Timeout")));

        var command = new ClassifyChangeWithAI.Command(
            ChangeTitle: "Add new endpoint",
            ChangeDescription: null,
            AffectedService: null,
            CurrentVersion: null,
            TargetVersion: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("AIKnowledge.Provider.Unavailable");
    }

    [Fact]
    public async Task Handle_ShouldParseMitigationSteps_WithNumberedList()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Feature change.\n1. Update documentation\n2. Notify consumers\n3. Deploy gradually");

        var command = new ClassifyChangeWithAI.Command(
            ChangeTitle: "Add optional field to response",
            ChangeDescription: null,
            AffectedService: null,
            CurrentVersion: null,
            TargetVersion: null,
            PreferredProvider: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuggestedMitigationSteps.Should().HaveCount(3);
        result.Value.SuggestedMitigationSteps[0].Should().Be("Update documentation");
    }
}
