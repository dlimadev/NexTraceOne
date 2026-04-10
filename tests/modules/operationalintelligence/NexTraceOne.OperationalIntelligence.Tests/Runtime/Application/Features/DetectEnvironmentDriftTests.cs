using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DetectEnvironmentDrift;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>
/// Testes unitários para a feature DetectEnvironmentDrift.
/// Verificam: sucesso, stale de relatórios anteriores, severidade derivada, validação.
/// </summary>
public sealed class DetectEnvironmentDriftTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid FixedTenantId = Guid.NewGuid();

    private readonly IEnvironmentDriftReportRepository _reportRepo = Substitute.For<IEnvironmentDriftReportRepository>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public DetectEnvironmentDriftTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _currentTenant.Id.Returns(FixedTenantId);
    }

    [Fact]
    public async Task Handle_NoPreviousReport_ShouldCreateNewReport()
    {
        _reportRepo.GetLatestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EnvironmentDriftReport?)null);

        var handler = new DetectEnvironmentDrift.Handler(
            _reportRepo, _currentTenant, _clock, _unitOfWork);
        var command = new DetectEnvironmentDrift.Command(
            "production", "staging",
            ServiceVersionDrifts: "[]",
            TotalDriftItems: 2,
            CriticalDriftItems: 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SourceEnvironment.Should().Be("production");
        result.Value.TargetEnvironment.Should().Be("staging");
        result.Value.TotalDriftItems.Should().Be(2);
        result.Value.Status.Should().Be("Generated");
        result.Value.GeneratedAt.Should().Be(FixedNow);

        _reportRepo.Received(1).Add(Arg.Any<EnvironmentDriftReport>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPreviousGeneratedReport_ShouldMarkPreviousAsStale()
    {
        var previousReport = EnvironmentDriftReport.Generate(
            "production", "staging", "General",
            null, null, null, null, null, null,
            1, 0, DriftSeverity.Low, FixedTenantId, FixedNow.AddDays(-1));

        _reportRepo.GetLatestAsync("production", "staging", Arg.Any<CancellationToken>())
            .Returns(previousReport);

        var handler = new DetectEnvironmentDrift.Handler(
            _reportRepo, _currentTenant, _clock, _unitOfWork);
        var command = new DetectEnvironmentDrift.Command(
            "production", "staging",
            TotalDriftItems: 3, CriticalDriftItems: 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        previousReport.Status.Should().Be(DriftReportStatus.Stale);
        _reportRepo.Received(1).Update(previousReport);
    }

    [Fact]
    public async Task Handle_WithCriticalDrifts_ShouldReturnCriticalSeverity()
    {
        _reportRepo.GetLatestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EnvironmentDriftReport?)null);

        var handler = new DetectEnvironmentDrift.Handler(
            _reportRepo, _currentTenant, _clock, _unitOfWork);
        var command = new DetectEnvironmentDrift.Command(
            "production", "staging",
            ServiceVersionDrifts: "[]",
            TotalDriftItems: 5, CriticalDriftItems: 2);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallSeverity.Should().Be("Critical");
    }

    [Fact]
    public async Task Handle_WithMoreThan5Drifts_NoCritical_ShouldReturnHighSeverity()
    {
        _reportRepo.GetLatestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EnvironmentDriftReport?)null);

        var handler = new DetectEnvironmentDrift.Handler(
            _reportRepo, _currentTenant, _clock, _unitOfWork);
        var command = new DetectEnvironmentDrift.Command(
            "production", "staging",
            TotalDriftItems: 8, CriticalDriftItems: 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallSeverity.Should().Be("High");
    }

    [Fact]
    public async Task Handle_WithZeroDrifts_ShouldReturnLowSeverity()
    {
        _reportRepo.GetLatestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EnvironmentDriftReport?)null);

        var handler = new DetectEnvironmentDrift.Handler(
            _reportRepo, _currentTenant, _clock, _unitOfWork);
        var command = new DetectEnvironmentDrift.Command(
            "production", "staging",
            TotalDriftItems: 0, CriticalDriftItems: 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallSeverity.Should().Be("Low");
    }

    [Fact]
    public async Task Handle_WithMultipleDimensions_ShouldBuildDimensionsList()
    {
        _reportRepo.GetLatestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EnvironmentDriftReport?)null);

        var handler = new DetectEnvironmentDrift.Handler(
            _reportRepo, _currentTenant, _clock, _unitOfWork);
        var command = new DetectEnvironmentDrift.Command(
            "production", "staging",
            ServiceVersionDrifts: "[]",
            ContractVersionDrifts: "[]",
            PolicyDrifts: "[]",
            TotalDriftItems: 3, CriticalDriftItems: 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AnalyzedDimensions.Should().Contain("ServiceVersions");
        result.Value.AnalyzedDimensions.Should().Contain("ContractVersions");
        result.Value.AnalyzedDimensions.Should().Contain("Policies");
    }

    [Fact]
    public void Validator_ShouldRejectSameEnvironments()
    {
        var validator = new DetectEnvironmentDrift.Validator();
        var command = new DetectEnvironmentDrift.Command("production", "production");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldRejectEmptySourceEnvironment()
    {
        var validator = new DetectEnvironmentDrift.Validator();
        var command = new DetectEnvironmentDrift.Command("", "staging");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldRejectNegativeCriticalGreaterThanTotal()
    {
        var validator = new DetectEnvironmentDrift.Validator();
        var command = new DetectEnvironmentDrift.Command(
            "production", "staging",
            TotalDriftItems: 2, CriticalDriftItems: 5);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldAcceptValidCommand()
    {
        var validator = new DetectEnvironmentDrift.Validator();
        var command = new DetectEnvironmentDrift.Command(
            "production", "staging",
            TotalDriftItems: 3, CriticalDriftItems: 1);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
