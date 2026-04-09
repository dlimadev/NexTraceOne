using FluentAssertions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GenerateAnomalyNarrative;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

using NSubstitute;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>
/// Testes unitários para a feature GenerateAnomalyNarrative.
/// Verificam: sucesso, drift not found, narrativa duplicada, validação.
/// </summary>
public sealed class GenerateAnomalyNarrativeTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IDriftFindingRepository _driftRepo = Substitute.For<IDriftFindingRepository>();
    private readonly IAnomalyNarrativeRepository _narrativeRepo = Substitute.For<IAnomalyNarrativeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();

    public GenerateAnomalyNarrativeTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _currentTenant.IsActive.Returns(true);
        _currentTenant.Id.Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_DriftExists_NoExistingNarrative_ShouldSucceed()
    {
        var driftFinding = DriftFinding.Detect(
            "order-service", "production", "AvgLatencyMs",
            100m, 180m, FixedNow);
        var driftFindingGuid = driftFinding.Id.Value;

        _driftRepo.GetByIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns(driftFinding);
        _narrativeRepo.GetByDriftFindingIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns((AnomalyNarrative?)null);

        var handler = new GenerateAnomalyNarrative.Handler(
            _driftRepo, _narrativeRepo, _unitOfWork, _clock, _currentTenant);
        var command = new GenerateAnomalyNarrative.Command(driftFindingGuid, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NarrativeText.Should().Contain("Anomaly Narrative");
        result.Value.ModelUsed.Should().Be("template-v1");
        result.Value.GeneratedAt.Should().Be(FixedNow);

        await _narrativeRepo.Received(1).AddAsync(Arg.Any<AnomalyNarrative>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DriftExists_WithModelPreference_ShouldUsePreferredModel()
    {
        var driftFinding = DriftFinding.Detect(
            "order-service", "production", "ErrorRate",
            0.01m, 0.05m, FixedNow);

        _driftRepo.GetByIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns(driftFinding);
        _narrativeRepo.GetByDriftFindingIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns((AnomalyNarrative?)null);

        var handler = new GenerateAnomalyNarrative.Handler(
            _driftRepo, _narrativeRepo, _unitOfWork, _clock, _currentTenant);
        var command = new GenerateAnomalyNarrative.Command(driftFinding.Id.Value, "gpt-4o");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ModelUsed.Should().Be("gpt-4o");
    }

    [Fact]
    public async Task Handle_DriftNotFound_ShouldReturnError()
    {
        _driftRepo.GetByIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns((DriftFinding?)null);

        var handler = new GenerateAnomalyNarrative.Handler(
            _driftRepo, _narrativeRepo, _unitOfWork, _clock, _currentTenant);
        var command = new GenerateAnomalyNarrative.Command(Guid.NewGuid(), null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_NarrativeAlreadyExists_ShouldReturnError()
    {
        var driftFinding = DriftFinding.Detect(
            "order-service", "production", "AvgLatencyMs",
            100m, 180m, FixedNow);

        _driftRepo.GetByIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns(driftFinding);

        var existingNarrative = AnomalyNarrative.Create(
            AnomalyNarrativeId.New(),
            driftFinding.Id,
            "Existing narrative",
            null, null, null, null, null, null,
            "template-v1", 0, AnomalyNarrativeStatus.Draft, null, FixedNow);

        _narrativeRepo.GetByDriftFindingIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns(existingNarrative);

        var handler = new GenerateAnomalyNarrative.Handler(
            _driftRepo, _narrativeRepo, _unitOfWork, _clock, _currentTenant);
        var command = new GenerateAnomalyNarrative.Command(driftFinding.Id.Value, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyExists");
    }

    [Fact]
    public void Validator_ShouldRejectEmptyDriftFindingId()
    {
        var validator = new GenerateAnomalyNarrative.Validator();
        var command = new GenerateAnomalyNarrative.Command(Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldAcceptValidCommand()
    {
        var validator = new GenerateAnomalyNarrative.Validator();
        var command = new GenerateAnomalyNarrative.Command(Guid.NewGuid(), "gpt-4o");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
