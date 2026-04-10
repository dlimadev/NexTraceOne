using FluentAssertions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.RefreshAnomalyNarrative;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

using NSubstitute;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>
/// Testes unitários para a feature RefreshAnomalyNarrative.
/// Verificam: sucesso, drift not found, narrativa não encontrada, validação.
/// </summary>
public sealed class RefreshAnomalyNarrativeTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IDriftFindingRepository _driftRepo = Substitute.For<IDriftFindingRepository>();
    private readonly IAnomalyNarrativeRepository _narrativeRepo = Substitute.For<IAnomalyNarrativeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public RefreshAnomalyNarrativeTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    [Fact]
    public async Task Handle_Success_ShouldRefreshNarrativeAndIncrementCount()
    {
        var driftFinding = DriftFinding.Detect(
            "order-service", "production", "AvgLatencyMs",
            100m, 180m, FixedNow.AddHours(-2));

        var existingNarrative = AnomalyNarrative.Create(
            AnomalyNarrativeId.New(),
            driftFinding.Id,
            "Old narrative",
            null, null, null, null, null, null,
            "template-v1", 0, AnomalyNarrativeStatus.Draft, null, FixedNow.AddHours(-1));

        _driftRepo.GetByIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns(driftFinding);
        _narrativeRepo.GetByDriftFindingIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns(existingNarrative);

        var handler = new RefreshAnomalyNarrative.Handler(
            _driftRepo, _narrativeRepo, _unitOfWork, _clock);
        var command = new RefreshAnomalyNarrative.Command(driftFinding.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NarrativeText.Should().Contain("Anomaly Narrative");
        result.Value.RefreshCount.Should().Be(1);
        result.Value.RefreshedAt.Should().Be(FixedNow);

        _narrativeRepo.Received(1).Update(Arg.Any<AnomalyNarrative>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DriftNotFound_ShouldReturnError()
    {
        _driftRepo.GetByIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns((DriftFinding?)null);

        var handler = new RefreshAnomalyNarrative.Handler(
            _driftRepo, _narrativeRepo, _unitOfWork, _clock);
        var command = new RefreshAnomalyNarrative.Command(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_NarrativeNotFound_ShouldReturnError()
    {
        var driftFinding = DriftFinding.Detect(
            "order-service", "production", "AvgLatencyMs",
            100m, 180m, FixedNow);

        _driftRepo.GetByIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns(driftFinding);
        _narrativeRepo.GetByDriftFindingIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns((AnomalyNarrative?)null);

        var handler = new RefreshAnomalyNarrative.Handler(
            _driftRepo, _narrativeRepo, _unitOfWork, _clock);
        var command = new RefreshAnomalyNarrative.Command(driftFinding.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void Validator_ShouldRejectEmptyDriftFindingId()
    {
        var validator = new RefreshAnomalyNarrative.Validator();
        var command = new RefreshAnomalyNarrative.Command(Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldAcceptValidCommand()
    {
        var validator = new RefreshAnomalyNarrative.Validator();
        var command = new RefreshAnomalyNarrative.Command(Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
