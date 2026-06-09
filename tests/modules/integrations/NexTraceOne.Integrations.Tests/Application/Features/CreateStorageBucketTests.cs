using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.CreateStorageBucket;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes de unidade para a feature CreateStorageBucket.
/// Verifica a criação de buckets e validação dos inputs.
/// </summary>
public sealed class CreateStorageBucketTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IStorageBucketRepository _repository = Substitute.For<IStorageBucketRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public CreateStorageBucketTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    private CreateStorageBucket.Handler CreateHandler()
        => new(_repository, _unitOfWork, _clock);

    // ── Test 1: Happy path ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateStorageBucket_ValidCommand_ShouldCreateBucketAndReturnResponse()
    {
        // Arrange
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();
        var command = new CreateStorageBucket.Command(
            TenantId: "tenant-abc",
            BucketName: "audit-logs",
            BackendType: StorageBucketBackendType.ClickHouse,
            RetentionDays: 90,
            FilterJson: null,
            Priority: 10,
            IsEnabled: true,
            IsFallback: false,
            Description: "Audit log storage bucket");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BucketName.Should().Be("audit-logs");
        result.Value.BackendType.Should().Be(StorageBucketBackendType.ClickHouse);
        result.Value.RetentionDays.Should().Be(90);
        result.Value.Priority.Should().Be(10);
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.IsFallback.Should().BeFalse();
        result.Value.BucketId.Should().NotBeEmpty();

        await _repository.Received(1).AddAsync(Arg.Any<StorageBucket>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Test 2: Fallback bucket ───────────────────────────────────────────────

    [Fact]
    public async Task CreateStorageBucket_FallbackBucket_ShouldSetIsFallbackTrue()
    {
        // Arrange
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();
        var command = new CreateStorageBucket.Command(
            TenantId: "tenant-xyz",
            BucketName: "default-fallback",
            BackendType: StorageBucketBackendType.ClickHouse,
            RetentionDays: 30,
            FilterJson: null,
            Priority: 999,
            IsEnabled: true,
            IsFallback: true,
            Description: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFallback.Should().BeTrue();
        result.Value.BucketName.Should().Be("default-fallback");

        await _repository.Received(1).AddAsync(
            Arg.Is<StorageBucket>(b => b.IsFallback && b.BucketName == "default-fallback"),
            Arg.Any<CancellationToken>());
    }

    // ── Test 3: Validation — bucket name too long ─────────────────────────────

    [Fact]
    public async Task CreateStorageBucket_BucketNameTooLong_ShouldFailValidation()
    {
        // Arrange
        var validator = new CreateStorageBucket.Validator();
        var longName = new string('a', 101); // max is 100
        var command = new CreateStorageBucket.Command(
            TenantId: "tenant-1",
            BucketName: longName,
            BackendType: StorageBucketBackendType.ClickHouse,
            RetentionDays: 30,
            FilterJson: null,
            Priority: 1);

        // Act
        var validationResult = await validator.ValidateAsync(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "BucketName");
    }
}
