using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

using GetHealthScoreFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractHealthScore.GetContractHealthScore;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler GetContractHealthScore — obtém o score de saúde persistido de um contrato.
/// Valida retorno de score existente, erro quando não encontrado e validação de entrada.
/// </summary>
public sealed class GetContractHealthScoreTests
{
    private static readonly Guid ApiAssetId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedDate = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Retorna score existente ──────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnScore_When_Exists()
    {
        var healthScore = ContractHealthScore.Create(
            ApiAssetId, 80, 70, 60, 50, 90, 85, 50, FixedDate);

        var repository = Substitute.For<IContractHealthScoreRepository>();
        repository.GetByApiAssetIdAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(healthScore);

        var sut = new GetHealthScoreFeature.Handler(repository);
        var result = await sut.Handle(
            new GetHealthScoreFeature.Query(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiAssetId.Should().Be(ApiAssetId);
        result.Value.OverallScore.Should().Be(healthScore.OverallScore);
        result.Value.BreakingChangeFrequencyScore.Should().Be(80);
        result.Value.ConsumerImpactScore.Should().Be(70);
        result.Value.ReviewRecencyScore.Should().Be(60);
        result.Value.ExampleCoverageScore.Should().Be(50);
        result.Value.PolicyComplianceScore.Should().Be(90);
        result.Value.DocumentationScore.Should().Be(85);
        result.Value.DegradationThreshold.Should().Be(50);
        result.Value.CalculatedAt.Should().Be(FixedDate);
    }

    // ── Erro quando não encontrado ───────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnError_When_ScoreNotFound()
    {
        var repository = Substitute.For<IContractHealthScoreRepository>();
        repository.GetByApiAssetIdAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((ContractHealthScore?)null);

        var sut = new GetHealthScoreFeature.Handler(repository);
        var result = await sut.Handle(
            new GetHealthScoreFeature.Query(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("HealthScore");
    }

    // ── IsDegraded refletido na resposta ──────────────────────────────

    [Fact]
    public async Task Handle_Should_ReflectDegradedState()
    {
        var healthScore = ContractHealthScore.Create(
            ApiAssetId, 10, 10, 10, 10, 10, 10, 50, FixedDate);

        var repository = Substitute.For<IContractHealthScoreRepository>();
        repository.GetByApiAssetIdAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(healthScore);

        var sut = new GetHealthScoreFeature.Handler(repository);
        var result = await sut.Handle(
            new GetHealthScoreFeature.Query(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDegraded.Should().BeTrue();
    }

    // ── Validador ────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_Should_Fail_When_ApiAssetIdIsEmpty()
    {
        var validator = new GetHealthScoreFeature.Validator();
        var result = await validator.ValidateAsync(
            new GetHealthScoreFeature.Query(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiAssetId");
    }

    [Fact]
    public async Task Validator_Should_Pass_When_ValidApiAssetId()
    {
        var validator = new GetHealthScoreFeature.Validator();
        var result = await validator.ValidateAsync(
            new GetHealthScoreFeature.Query(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
