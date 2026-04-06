using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.ReviewContractDraft;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes do handler ReviewContractDraft — revisão de rascunhos de contratos por IA.
/// Valida parsing de qualidade, issues, sugestões, recomendações e fallbacks.
/// </summary>
public sealed class ReviewContractDraftTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid DraftId = Guid.NewGuid();

    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<ReviewContractDraft.Handler> _logger = Substitute.For<ILogger<ReviewContractDraft.Handler>>();

    private ReviewContractDraft.Handler CreateHandler() => new(_routingPort, _dateTimeProvider, _logger);

    private ReviewContractDraft.Command DefaultCommand(string? preferred = null) => new(
        TenantId: "tenant-acme",
        DraftId: DraftId,
        ContractContent: """{"openapi":"3.1.0","info":{"title":"Test"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""",
        ContractType: "OpenApi",
        ServiceName: "user-service",
        PreferredProvider: preferred);

    [Fact]
    public async Task Handle_ShouldReturnApprove_WhenAiIndicatesHighQuality()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("QUALITY_SCORE: 92\nRECOMMENDATION: Approve\nSUGGESTION: Consider adding response examples.");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QualityScore.Should().Be(92);
        result.Value.Recommendation.Should().Be("Approve");
        result.Value.Suggestions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnReject_WhenAiIndicatesCriticalIssues()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("QUALITY_SCORE: 20\nISSUE: Critical|Security|No authentication defined\nRECOMMENDATION: Reject");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QualityScore.Should().Be(20);
        result.Value.Recommendation.Should().Be("Reject");
        result.Value.Issues.Should().HaveCount(1);
        result.Value.Issues[0].Severity.Should().Be("Critical");
        result.Value.Issues[0].Category.Should().Be("Security");
    }

    [Fact]
    public async Task Handle_ShouldFallbackToRequestChanges_WhenAiResponseIsEmpty()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Recommendation.Should().Be("RequestChanges");
        result.Value.QualityScore.Should().Be(50);
    }

    [Fact]
    public async Task Handle_ShouldFallbackToRequestChanges_WhenAiThrows()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("Provider unavailable"));

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Recommendation.Should().Be("RequestChanges");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenTenantIdIsEmpty()
    {
        var command = DefaultCommand() with { TenantId = string.Empty };
        var validator = new ReviewContractDraft.Validator();
        var vr = await validator.ValidateAsync(command);
        vr.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenContractContentIsEmpty()
    {
        var command = DefaultCommand() with { ContractContent = string.Empty };
        var validator = new ReviewContractDraft.Validator();
        var vr = await validator.ValidateAsync(command);
        vr.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenServiceNameIsEmpty()
    {
        var command = DefaultCommand() with { ServiceName = string.Empty };
        var validator = new ReviewContractDraft.Validator();
        var vr = await validator.ValidateAsync(command);
        vr.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldPassPreferredProvider_ToRoutingPort()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), "openai-gpt4", Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("QUALITY_SCORE: 80\nRECOMMENDATION: Approve");

        var result = await CreateHandler().Handle(DefaultCommand("openai-gpt4"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProviderUsed.Should().Be("openai-gpt4");
    }
}
