using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using RecalculateFeature = NexTraceOne.Catalog.Application.Contracts.Features.RecalculateContractHealthScore.RecalculateContractHealthScore;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler RecalculateContractHealthScore — recalcula o score de saúde de um contrato.
/// Valida criação de novo score, atualização de score existente, e erro quando sem versões.
/// </summary>
public sealed class RecalculateContractHealthScoreTests
{
    private static readonly Guid ApiAssetId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedDate = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    private const string FullFeaturedSpec =
        """
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0", "description": "A test API", "summary": "Test summary" },
          "paths": {
            "/users": {
              "get": {
                "description": "List users",
                "summary": "List all users",
                "responses": {
                  "200": {
                    "example": { "id": 1, "name": "Test" },
                    "examples": { "default": { "value": { "id": 1 } } },
                    "schema": { "$ref": "#/components/schemas/User" }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "User": { "type": "object" }
            }
          }
        }
        """;

    private static ContractVersion CreateVersion(
        string semVer,
        string specContent,
        ContractLifecycleState state = ContractLifecycleState.Approved)
    {
        var version = ContractVersion.Import(
            ApiAssetId, semVer, specContent, "json", "upload", ContractProtocol.OpenApi).Value;

        if (state >= ContractLifecycleState.InReview)
            version.TransitionTo(ContractLifecycleState.InReview, FixedDate);
        if (state >= ContractLifecycleState.Approved)
            version.TransitionTo(ContractLifecycleState.Approved, FixedDate);

        return version;
    }

    private static (IContractHealthScoreRepository healthRepo, IContractVersionRepository versionRepo,
        IContractsUnitOfWork unitOfWork, IDateTimeProvider clock) CreateMocks()
    {
        var healthRepo = Substitute.For<IContractHealthScoreRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedDate);
        return (healthRepo, versionRepo, unitOfWork, clock);
    }

    // ── Cria novo score quando não existe ────────────────────────────

    [Fact]
    public async Task Handle_Should_CreateNewScore_When_NoneExists()
    {
        var (healthRepo, versionRepo, unitOfWork, clock) = CreateMocks();
        var version = CreateVersion("1.0.0", FullFeaturedSpec);

        versionRepo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { version });
        healthRepo.GetByApiAssetIdAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((ContractHealthScore?)null);

        var sut = new RecalculateFeature.Handler(healthRepo, versionRepo, unitOfWork, clock);
        var result = await sut.Handle(
            new RecalculateFeature.Command(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiAssetId.Should().Be(ApiAssetId);
        result.Value.OverallScore.Should().BeInRange(0, 100);
        result.Value.CalculatedAt.Should().Be(FixedDate);

        await healthRepo.Received(1).AddAsync(Arg.Any<ContractHealthScore>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Atualiza score existente ─────────────────────────────────────

    [Fact]
    public async Task Handle_Should_UpdateExistingScore_When_AlreadyExists()
    {
        var (healthRepo, versionRepo, unitOfWork, clock) = CreateMocks();
        var version = CreateVersion("1.0.0", FullFeaturedSpec);
        var existing = ContractHealthScore.Create(ApiAssetId, 50, 50, 50, 50, 50, 50, 50, FixedDate.AddHours(-1));

        versionRepo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { version });
        healthRepo.GetByApiAssetIdAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var sut = new RecalculateFeature.Handler(healthRepo, versionRepo, unitOfWork, clock);
        var result = await sut.Handle(
            new RecalculateFeature.Command(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CalculatedAt.Should().Be(FixedDate);

        await healthRepo.Received(1).UpdateAsync(Arg.Any<ContractHealthScore>(), Arg.Any<CancellationToken>());
        await healthRepo.DidNotReceive().AddAsync(Arg.Any<ContractHealthScore>(), Arg.Any<CancellationToken>());
    }

    // ── Erro quando sem versões ──────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnError_When_NoVersionsExist()
    {
        var (healthRepo, versionRepo, unitOfWork, clock) = CreateMocks();

        versionRepo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion>());

        var sut = new RecalculateFeature.Handler(healthRepo, versionRepo, unitOfWork, clock);
        var result = await sut.Handle(
            new RecalculateFeature.Command(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("HealthScore");
    }

    // ── Validador ────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_Should_Fail_When_ApiAssetIdIsEmpty()
    {
        var validator = new RecalculateFeature.Validator();
        var result = await validator.ValidateAsync(
            new RecalculateFeature.Command(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiAssetId");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Validator_Should_Fail_When_ThresholdOutOfRange(int threshold)
    {
        var validator = new RecalculateFeature.Validator();
        var result = await validator.ValidateAsync(
            new RecalculateFeature.Command(Guid.NewGuid(), threshold));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DegradationThreshold");
    }

    // ── Score reflete breaking changes ────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReflectBreakingChanges_When_MajorBumpsExist()
    {
        var (healthRepo, versionRepo, unitOfWork, clock) = CreateMocks();

        var v1 = CreateVersion("1.0.0", FullFeaturedSpec);
        var v2 = CreateVersion("2.0.0", FullFeaturedSpec);
        var v3 = CreateVersion("3.0.0", FullFeaturedSpec);

        versionRepo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { v1, v2, v3 });
        healthRepo.GetByApiAssetIdAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((ContractHealthScore?)null);

        var sut = new RecalculateFeature.Handler(healthRepo, versionRepo, unitOfWork, clock);
        var result = await sut.Handle(
            new RecalculateFeature.Command(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // All versions are major bumps (100% breaking ratio) → BreakingChangeFrequencyScore = 0
        result.Value.BreakingChangeFrequencyScore.Should().Be(0);
    }

    // ── Degradation com threshold personalizado ──────────────────────

    [Fact]
    public async Task Handle_Should_SetIsDegraded_When_HighThreshold()
    {
        var (healthRepo, versionRepo, unitOfWork, clock) = CreateMocks();
        var version = CreateVersion("1.0.0", "{\"openapi\":\"3.1.0\"}");

        versionRepo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { version });
        healthRepo.GetByApiAssetIdAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((ContractHealthScore?)null);

        var sut = new RecalculateFeature.Handler(healthRepo, versionRepo, unitOfWork, clock);
        var result = await sut.Handle(
            new RecalculateFeature.Command(ApiAssetId, DegradationThreshold: 95),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDegraded.Should().BeTrue();
    }
}
