using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.AnalyzeNonProdEnvironment;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes unitários do handler AnalyzeNonProdEnvironment.
/// Valida: análise de ambiente não produtivo, parsing de achados, isolamento de tenant.
/// </summary>
public sealed class AnalyzeNonProdEnvironmentTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 20, 10, 0, 0, TimeSpan.Zero);
    private const string SampleTenantId = "tenant-acme-001";
    private const string SampleEnvId = "env-qa-001";

    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<AnalyzeNonProdEnvironment.Handler> _logger = Substitute.For<ILogger<AnalyzeNonProdEnvironment.Handler>>();

    private AnalyzeNonProdEnvironment.Handler CreateHandler() =>
        new(_routingPort, _dateTimeProvider, _logger);

    private static AnalyzeNonProdEnvironment.Command DefaultCommand(
        string? tenantId = null,
        string? envId = null,
        int windowDays = 7) =>
        new(
            TenantId: tenantId ?? SampleTenantId,
            EnvironmentId: envId ?? SampleEnvId,
            EnvironmentName: "QA",
            EnvironmentProfile: "qa",
            ServiceFilter: null,
            ObservationWindowDays: windowDays,
            PreferredProvider: null);

    [Fact]
    public async Task Handle_ShouldReturnAnalysis_WhenProviderResponds()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("FINDING: HIGH | contract-drift | PaymentService contract has breaking changes not in production\nOVERALL_RISK: HIGH\nRECOMMENDATION: Review contract changes before promoting.");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantId.Should().Be(SampleTenantId);
        result.Value.EnvironmentId.Should().Be(SampleEnvId);
        result.Value.OverallRiskLevel.Should().Be("HIGH");
        result.Value.Recommendation.Should().NotBeEmpty();
        result.Value.Findings.Should().HaveCount(1);
        result.Value.Findings[0].Severity.Should().Be("HIGH");
        result.Value.Findings[0].Category.Should().Be("contract-drift");
        result.Value.CorrelationId.Should().NotBeNullOrWhiteSpace();
        result.Value.IsFallback.Should().BeFalse();
        result.Value.ObservationWindowDays.Should().Be(7);
    }

    [Fact]
    public async Task Handle_ShouldReturnNoFindings_WhenEnvironmentIsClean()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("OVERALL_RISK: LOW\nRECOMMENDATION: Environment looks healthy. No significant risks detected.");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallRiskLevel.Should().Be("LOW");
        result.Value.Findings.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldDetectFallback_WhenResponseContainsFallbackPrefix()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("[FALLBACK_PROVIDER_UNAVAILABLE] No AI provider available.");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsFallback.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenProviderThrows()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("Connection refused")));

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Contain("Unavailable");
    }

    [Fact]
    public async Task Handle_ShouldIncludeTenantInContext_ForIsolation()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        string? capturedGrounding = null;
        _routingPort.RouteQueryAsync(
            Arg.Do<string>(g => capturedGrounding = g),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("OVERALL_RISK: LOW\nRECOMMENDATION: All good.");

        await CreateHandler().Handle(DefaultCommand(tenantId: "tenant-xyz-999"), CancellationToken.None);

        capturedGrounding.Should().Contain("tenant-xyz-999");
    }

    [Fact]
    public async Task Handle_ShouldParseMultipleFindings()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                "FINDING: HIGH | contract-drift | PaymentService has breaking changes\n" +
                "FINDING: MEDIUM | telemetry | Error rate 15% above baseline\n" +
                "FINDING: LOW | topology | Missing health check endpoint\n" +
                "OVERALL_RISK: HIGH\n" +
                "RECOMMENDATION: Block promotion until contract drift is resolved.");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Findings.Should().HaveCount(3);
        result.Value.Findings[0].Severity.Should().Be("HIGH");
        result.Value.Findings[1].Severity.Should().Be("MEDIUM");
        result.Value.Findings[2].Severity.Should().Be("LOW");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_ShouldFail_WhenTenantIdIsEmpty(string tenantId)
    {
        var validator = new AnalyzeNonProdEnvironment.Validator();
        var command = DefaultCommand(tenantId: tenantId);
        var validationResult = await validator.ValidateAsync(command);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(AnalyzeNonProdEnvironment.Command.TenantId));
    }
}
