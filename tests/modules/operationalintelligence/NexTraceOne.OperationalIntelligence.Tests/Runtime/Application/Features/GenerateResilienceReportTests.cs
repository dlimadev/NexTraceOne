using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GenerateResilienceReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>
/// Testes unitários para a feature GenerateResilienceReport.
/// Verificam: sucesso, validação, persistência.
/// </summary>
public sealed class GenerateResilienceReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 10, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid ExperimentId = Guid.NewGuid();

    private readonly IResilienceReportRepository _repository = Substitute.For<IResilienceReportRepository>();
    private readonly IRuntimeIntelligenceUnitOfWork _unitOfWork = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public GenerateResilienceReportTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _tenant.Id.Returns(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldGenerateAndPersistReport()
    {
        var handler = new GenerateResilienceReport.Handler(_repository, _unitOfWork, _tenant, _clock);
        var command = new GenerateResilienceReport.Command(
            ExperimentId,
            "payment-service",
            "Production",
            "latency-injection",
            85,
            "{\"services\":[\"order-svc\"]}",
            "{\"services\":[\"order-svc\",\"billing-svc\"]}",
            15.5m,
            "{\"p99_latency\":250}",
            120.5m,
            2.3m,
            45,
            "[\"Circuit breaker OK\"]",
            "[\"No retry on timeout\"]",
            "[\"Add retry policy\"]");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("payment-service");
        result.Value.Environment.Should().Be("Production");
        result.Value.ExperimentType.Should().Be("latency-injection");
        result.Value.ResilienceScore.Should().Be(85);
        result.Value.ChaosExperimentId.Should().Be(ExperimentId);
        result.Value.Status.Should().Be("Generated");
        result.Value.GeneratedAt.Should().Be(FixedNow);
        result.Value.ReportId.Should().NotBe(Guid.Empty);

        await _repository.Received(1).AddAsync(
            Arg.Is<ResilienceReport>(r =>
                r.ServiceName == "payment-service" &&
                r.Environment == "Production" &&
                r.ExperimentType == "latency-injection" &&
                r.ResilienceScore == 85 &&
                r.ChaosExperimentId == ExperimentId &&
                r.TenantId == "00000000-0000-0000-0000-000000000001"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MinimalCommand_ShouldSucceed()
    {
        var handler = new GenerateResilienceReport.Handler(_repository, _unitOfWork, _tenant, _clock);
        var command = new GenerateResilienceReport.Command(
            ExperimentId,
            "order-service",
            "Staging",
            "pod-kill",
            50,
            null, null, null, null, null, null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResilienceScore.Should().Be(50);
        await _repository.Received(1).AddAsync(Arg.Any<ResilienceReport>(), Arg.Any<CancellationToken>());
    }

    // ── Validator tests ──────────────────────────────────────────────────

    [Fact]
    public void Validator_ValidCommand_ShouldPass()
    {
        var validator = new GenerateResilienceReport.Validator();
        var command = new GenerateResilienceReport.Command(
            ExperimentId, "svc", "Dev", "latency-injection", 75,
            null, null, null, null, null, null, null, null, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_EmptyExperimentId_ShouldFail()
    {
        var validator = new GenerateResilienceReport.Validator();
        var command = new GenerateResilienceReport.Command(
            Guid.Empty, "svc", "Dev", "latency-injection", 75,
            null, null, null, null, null, null, null, null, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_EmptyServiceName_ShouldFail()
    {
        var validator = new GenerateResilienceReport.Validator();
        var command = new GenerateResilienceReport.Command(
            ExperimentId, "", "Dev", "latency-injection", 75,
            null, null, null, null, null, null, null, null, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ScoreTooLow_ShouldFail()
    {
        var validator = new GenerateResilienceReport.Validator();
        var command = new GenerateResilienceReport.Command(
            ExperimentId, "svc", "Dev", "pod-kill", -1,
            null, null, null, null, null, null, null, null, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ScoreTooHigh_ShouldFail()
    {
        var validator = new GenerateResilienceReport.Validator();
        var command = new GenerateResilienceReport.Command(
            ExperimentId, "svc", "Dev", "pod-kill", 101,
            null, null, null, null, null, null, null, null, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_BoundaryScore0_ShouldPass()
    {
        var validator = new GenerateResilienceReport.Validator();
        var command = new GenerateResilienceReport.Command(
            ExperimentId, "svc", "Dev", "pod-kill", 0,
            null, null, null, null, null, null, null, null, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_BoundaryScore100_ShouldPass()
    {
        var validator = new GenerateResilienceReport.Validator();
        var command = new GenerateResilienceReport.Command(
            ExperimentId, "svc", "Dev", "pod-kill", 100,
            null, null, null, null, null, null, null, null, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
