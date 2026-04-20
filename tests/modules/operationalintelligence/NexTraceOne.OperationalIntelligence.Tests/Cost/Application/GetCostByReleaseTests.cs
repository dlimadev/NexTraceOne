using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

using GetCostByReleaseFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostByRelease.GetCostByRelease;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost.Application;

/// <summary>
/// Testes unitários para GetCostByRelease — correlaciona custos de infra com períodos de release.
/// Valida mapeamento, agregação de custo total, lista vazia e validação.
/// </summary>
public sealed class GetCostByReleaseTests
{
    private static readonly DateTimeOffset PeriodStart = new(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 4, 30, 23, 59, 59, TimeSpan.Zero);

    private readonly ICostAttributionRepository _repo = Substitute.For<ICostAttributionRepository>();

    private GetCostByReleaseFeature.Handler CreateHandler() => new(_repo);

    private static CostAttribution MakeAttribution(string service, decimal totalCost)
    {
        var result = CostAttribution.Attribute(
            apiAssetId: Guid.NewGuid(),
            serviceName: service,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: totalCost,
            requestCount: 1000,
            environment: "production");
        return result.Value;
    }

    // ── Happy path — single attribution ──────────────────────────────────────

    [Fact]
    public async Task Handle_SingleAttribution_ReturnsMappedItem()
    {
        var releaseId = Guid.NewGuid();
        var attribution = MakeAttribution("checkout-api", 250.00m);

        _repo.ListByPeriodAsync(PeriodStart, PeriodEnd, Arg.Any<CancellationToken>())
            .Returns(new List<CostAttribution> { attribution });

        var result = await CreateHandler().Handle(
            new GetCostByReleaseFeature.Query(releaseId, PeriodStart, PeriodEnd),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(releaseId);
        result.Value.PeriodStart.Should().Be(PeriodStart);
        result.Value.PeriodEnd.Should().Be(PeriodEnd);
        result.Value.Attributions.Should().HaveCount(1);
        result.Value.Attributions[0].ServiceName.Should().Be("checkout-api");
        result.Value.Attributions[0].TotalCost.Should().Be(250.00m);
        result.Value.TotalCost.Should().Be(250.00m);
    }

    // ── Happy path — multiple attributions aggregated ────────────────────────

    [Fact]
    public async Task Handle_MultipleAttributions_SumsTotalCost()
    {
        var releaseId = Guid.NewGuid();
        var a1 = MakeAttribution("order-api", 100m);
        var a2 = MakeAttribution("payment-api", 150m);

        _repo.ListByPeriodAsync(PeriodStart, PeriodEnd, Arg.Any<CancellationToken>())
            .Returns(new List<CostAttribution> { a1, a2 });

        var result = await CreateHandler().Handle(
            new GetCostByReleaseFeature.Query(releaseId, PeriodStart, PeriodEnd),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCost.Should().Be(250m);
        result.Value.Attributions.Should().HaveCount(2);
    }

    // ── Empty result ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoAttributions_ReturnsTotalCostZero()
    {
        var releaseId = Guid.NewGuid();
        _repo.ListByPeriodAsync(PeriodStart, PeriodEnd, Arg.Any<CancellationToken>())
            .Returns(new List<CostAttribution>());

        var result = await CreateHandler().Handle(
            new GetCostByReleaseFeature.Query(releaseId, PeriodStart, PeriodEnd),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Attributions.Should().BeEmpty();
        result.Value.TotalCost.Should().Be(0m);
    }

    // ── Validator ────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyReleaseId_ReturnsError()
    {
        var validator = new GetCostByReleaseFeature.Validator();
        var result = validator.Validate(
            new GetCostByReleaseFeature.Query(Guid.Empty, PeriodStart, PeriodEnd));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ReleaseId");
    }

    [Fact]
    public void Validator_PeriodEndBeforePeriodStart_ReturnsError()
    {
        var validator = new GetCostByReleaseFeature.Validator();
        var result = validator.Validate(
            new GetCostByReleaseFeature.Query(Guid.NewGuid(), PeriodEnd, PeriodStart));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PeriodEnd");
    }

    [Fact]
    public void Validator_ValidInput_Passes()
    {
        var validator = new GetCostByReleaseFeature.Validator();
        var result = validator.Validate(
            new GetCostByReleaseFeature.Query(Guid.NewGuid(), PeriodStart, PeriodEnd));

        result.IsValid.Should().BeTrue();
    }
}
