using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.AssessPromotionReadiness;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

public sealed class AssessPromotionReadinessTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<AssessPromotionReadiness.Handler> _logger = Substitute.For<ILogger<AssessPromotionReadiness.Handler>>();

    private AssessPromotionReadiness.Handler CreateHandler() =>
        new(_routingPort, _dateTimeProvider, _logger);

    private static AssessPromotionReadiness.Command DefaultCommand() =>
        new(
            TenantId: "tenant-acme-001",
            SourceEnvironmentId: "env-qa-001",
            SourceEnvironmentName: "QA",
            SourceIsProductionLike: false,
            TargetEnvironmentId: "env-prod-001",
            TargetEnvironmentName: "Production",
            TargetIsProductionLike: true,
            ServiceName: "payment-service",
            Version: "2.1.0",
            ReleaseId: "rel-123",
            ObservationWindowDays: 7,
            PreferredProvider: null);

    [Fact]
    public async Task Handle_ShouldReturnReadinessAssessment_WhenProviderResponds()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                "READINESS_SCORE: 78\n" +
                "READINESS_LEVEL: NEEDS_REVIEW\n" +
                "BLOCKER: contract | Payment v2.1 has breaking change in response schema\n" +
                "WARNING: performance | P99 latency slightly above SLA threshold\n" +
                "SHOULD_BLOCK: YES\n" +
                "SUMMARY: Service has a contract breaking change that must be resolved.");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReadinessScore.Should().Be(78);
        result.Value.ReadinessLevel.Should().Be("NEEDS_REVIEW");
        result.Value.Blockers.Should().HaveCount(1);
        result.Value.Warnings.Should().HaveCount(1);
        result.Value.ShouldBlock.Should().BeTrue();
        result.Value.Summary.Should().NotBeEmpty();
        result.Value.ServiceName.Should().Be("payment-service");
        result.Value.Version.Should().Be("2.1.0");
    }

    [Fact]
    public async Task Handle_ShouldReturnReady_WhenServiceIsPromotable()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                "READINESS_SCORE: 95\n" +
                "READINESS_LEVEL: READY\n" +
                "SHOULD_BLOCK: NO\n" +
                "SUMMARY: Service is ready for production promotion.");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReadinessLevel.Should().Be("READY");
        result.Value.ShouldBlock.Should().BeFalse();
        result.Value.Blockers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldClampScore_WhenScoreOutOfRange()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("READINESS_SCORE: 150\nREADINESS_LEVEL: READY\nSHOULD_BLOCK: NO\nSUMMARY: Good.");

        var result = await CreateHandler().Handle(DefaultCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReadinessScore.Should().Be(100);
    }

    [Fact]
    public void Validate_ShouldFail_WhenSourceAndTargetAreSame()
    {
        var validator = new AssessPromotionReadiness.Validator();
        var command = new AssessPromotionReadiness.Command(
            TenantId: "tenant-acme-001",
            SourceEnvironmentId: "env-qa-001",
            SourceEnvironmentName: "QA",
            SourceIsProductionLike: false,
            TargetEnvironmentId: "env-qa-001", // same as source — the issue
            TargetEnvironmentName: "QA",
            TargetIsProductionLike: true,
            ServiceName: "payment-service",
            Version: "2.1.0",
            ReleaseId: null,
            ObservationWindowDays: 7,
            PreferredProvider: null);
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSourceIsProductionLike()
    {
        var validator = new AssessPromotionReadiness.Validator();
        var command = new AssessPromotionReadiness.Command(
            TenantId: "tenant-acme-001",
            SourceEnvironmentId: "env-prod-001",
            SourceEnvironmentName: "Production",
            SourceIsProductionLike: true, // wrong — source must be non-prod
            TargetEnvironmentId: "env-qa-001",
            TargetEnvironmentName: "QA",
            TargetIsProductionLike: true,
            ServiceName: "payment-service",
            Version: "2.1.0",
            ReleaseId: null,
            ObservationWindowDays: 7,
            PreferredProvider: null);
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.SourceIsProductionLike));
    }

    [Fact]
    public void Validate_ShouldFail_WhenTargetIsNotProductionLike()
    {
        var validator = new AssessPromotionReadiness.Validator();
        var command = new AssessPromotionReadiness.Command(
            TenantId: "tenant-acme-001",
            SourceEnvironmentId: "env-qa-001",
            SourceEnvironmentName: "QA",
            SourceIsProductionLike: false,
            TargetEnvironmentId: "env-staging-001",
            TargetEnvironmentName: "Staging",
            TargetIsProductionLike: false, // wrong — target must be prod-like
            ServiceName: "payment-service",
            Version: "2.1.0",
            ReleaseId: null,
            ObservationWindowDays: 7,
            PreferredProvider: null);
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.TargetIsProductionLike));
    }
}
