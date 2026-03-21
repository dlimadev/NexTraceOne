using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.CompareEnvironments;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

public sealed class CompareEnvironmentsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<CompareEnvironments.Handler> _logger = Substitute.For<ILogger<CompareEnvironments.Handler>>();

    private CompareEnvironments.Handler CreateHandler() =>
        new(_routingPort, _dateTimeProvider, _logger);

    private static CompareEnvironments.Command DefaultCommand(string? tenantId = null) =>
        new(
            TenantId: tenantId ?? "tenant-acme-001",
            SubjectEnvironmentId: "env-qa-001",
            SubjectEnvironmentName: "QA",
            SubjectEnvironmentProfile: "qa",
            ReferenceEnvironmentId: "env-prod-001",
            ReferenceEnvironmentName: "Production",
            ReferenceEnvironmentProfile: "production",
            ServiceFilter: null,
            ComparisonDimensions: null,
            PreferredProvider: null);

    [Fact]
    public async Task Handle_ShouldReturnComparison_WithDivergencesAndRecommendation()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("DIVERGENCE: HIGH | contracts | OrderService v2.1 in QA has breaking changes vs v2.0 in Production\n" +
                     "DIVERGENCE: MEDIUM | telemetry | P99 latency 40% higher in QA\n" +
                     "PROMOTION_RECOMMENDATION: BLOCK_PROMOTION\n" +
                     "SUMMARY: QA shows contract drift and performance regression.");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Divergences.Should().HaveCount(2);
        result.Value.PromotionRecommendation.Should().Be("BLOCK_PROMOTION");
        result.Value.Summary.Should().NotBeEmpty();
        result.Value.IsFallback.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldEnforceSameTenantInGrounding()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        string? capturedGrounding = null;
        _routingPort.RouteQueryAsync(
            Arg.Do<string>(g => capturedGrounding = g),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("PROMOTION_RECOMMENDATION: SAFE_TO_PROMOTE\nSUMMARY: Environments look aligned.");

        await CreateHandler().Handle(DefaultCommand(tenantId: "tenant-acme-001"), CancellationToken.None);

        capturedGrounding.Should().Contain("tenant-acme-001");
        capturedGrounding.Should().Contain("same tenant");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSubjectAndReferenceAreTheSame()
    {
        var validator = new CompareEnvironments.Validator();
        var command = new CompareEnvironments.Command(
            TenantId: "tenant-acme-001",
            SubjectEnvironmentId: "env-qa-001",
            SubjectEnvironmentName: "QA",
            SubjectEnvironmentProfile: "qa",
            ReferenceEnvironmentId: "env-qa-001",  // same as subject
            ReferenceEnvironmentName: "QA",
            ReferenceEnvironmentProfile: "qa",
            ServiceFilter: null,
            ComparisonDimensions: null,
            PreferredProvider: null);
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnSafeToPromote_WhenNoDivergences()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("PROMOTION_RECOMMENDATION: SAFE_TO_PROMOTE\nSUMMARY: Environments are well-aligned.");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PromotionRecommendation.Should().Be("SAFE_TO_PROMOTE");
        result.Value.Divergences.Should().BeEmpty();
    }
}
