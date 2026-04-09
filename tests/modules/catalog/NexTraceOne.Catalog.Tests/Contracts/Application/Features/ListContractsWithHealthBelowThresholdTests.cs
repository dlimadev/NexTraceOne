using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

using ListBelowThresholdFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContractsWithHealthBelowThreshold.ListContractsWithHealthBelowThreshold;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler ListContractsWithHealthBelowThreshold — lista contratos com score abaixo de um threshold.
/// Valida retorno de lista, lista vazia e validação de entrada.
/// </summary>
public sealed class ListContractsWithHealthBelowThresholdTests
{
    private static readonly DateTimeOffset FixedDate = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Retorna contratos abaixo do threshold ────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnItems_When_ScoresBelowThreshold()
    {
        var score1 = ContractHealthScore.Create(Guid.NewGuid(), 20, 20, 20, 20, 20, 20, 50, FixedDate);
        var score2 = ContractHealthScore.Create(Guid.NewGuid(), 30, 30, 30, 30, 30, 30, 50, FixedDate);

        var repository = Substitute.For<IContractHealthScoreRepository>();
        repository.ListBelowThresholdAsync(50, Arg.Any<CancellationToken>())
            .Returns(new List<ContractHealthScore> { score1, score2 });

        var sut = new ListBelowThresholdFeature.Handler(repository);
        var result = await sut.Handle(
            new ListBelowThresholdFeature.Query(50),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Threshold.Should().Be(50);
    }

    // ── Retorna lista vazia quando nenhum abaixo ─────────────────────

    [Fact]
    public async Task Handle_Should_ReturnEmptyList_When_NoScoresBelowThreshold()
    {
        var repository = Substitute.For<IContractHealthScoreRepository>();
        repository.ListBelowThresholdAsync(50, Arg.Any<CancellationToken>())
            .Returns(new List<ContractHealthScore>());

        var sut = new ListBelowThresholdFeature.Handler(repository);
        var result = await sut.Handle(
            new ListBelowThresholdFeature.Query(50),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── Respeita threshold configurado ───────────────────────────────

    [Fact]
    public async Task Handle_Should_PassThresholdToRepository()
    {
        var repository = Substitute.For<IContractHealthScoreRepository>();
        repository.ListBelowThresholdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContractHealthScore>());

        var sut = new ListBelowThresholdFeature.Handler(repository);
        await sut.Handle(
            new ListBelowThresholdFeature.Query(75),
            CancellationToken.None);

        await repository.Received(1).ListBelowThresholdAsync(75, Arg.Any<CancellationToken>());
    }

    // ── Items contêm dados corretos ──────────────────────────────────

    [Fact]
    public async Task Handle_Should_MapAllFieldsToResponse()
    {
        var apiAssetId = Guid.NewGuid();
        var score = ContractHealthScore.Create(apiAssetId, 10, 20, 30, 40, 15, 25, 60, FixedDate);

        var repository = Substitute.For<IContractHealthScoreRepository>();
        repository.ListBelowThresholdAsync(60, Arg.Any<CancellationToken>())
            .Returns(new List<ContractHealthScore> { score });

        var sut = new ListBelowThresholdFeature.Handler(repository);
        var result = await sut.Handle(
            new ListBelowThresholdFeature.Query(60),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items[0];
        item.ApiAssetId.Should().Be(apiAssetId);
        item.BreakingChangeFrequencyScore.Should().Be(10);
        item.ConsumerImpactScore.Should().Be(20);
        item.ReviewRecencyScore.Should().Be(30);
        item.ExampleCoverageScore.Should().Be(40);
        item.PolicyComplianceScore.Should().Be(15);
        item.DocumentationScore.Should().Be(25);
        item.CalculatedAt.Should().Be(FixedDate);
    }

    // ── Validador ────────────────────────────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Validator_Should_Fail_When_ThresholdOutOfRange(int threshold)
    {
        var validator = new ListBelowThresholdFeature.Validator();
        var result = await validator.ValidateAsync(
            new ListBelowThresholdFeature.Query(threshold));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Threshold");
    }

    [Fact]
    public async Task Validator_Should_Pass_When_ValidThreshold()
    {
        var validator = new ListBelowThresholdFeature.Validator();
        var result = await validator.ValidateAsync(
            new ListBelowThresholdFeature.Query(50));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_Should_Pass_When_BoundaryThresholdZero()
    {
        var validator = new ListBelowThresholdFeature.Validator();
        var result = await validator.ValidateAsync(
            new ListBelowThresholdFeature.Query(0));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_Should_Pass_When_BoundaryThresholdHundred()
    {
        var validator = new ListBelowThresholdFeature.Validator();
        var result = await validator.ValidateAsync(
            new ListBelowThresholdFeature.Query(100));

        result.IsValid.Should().BeTrue();
    }
}
