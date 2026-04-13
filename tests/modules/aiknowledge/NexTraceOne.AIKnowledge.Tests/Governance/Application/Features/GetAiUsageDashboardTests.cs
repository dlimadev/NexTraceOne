using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiUsageDashboard;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para GetAiUsageDashboard handler.
/// Valida grupos "model"/"user"/"provider", período, top N, totais e defaults.
/// </summary>
public sealed class GetAiUsageDashboardTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset UtcNow = new DateTimeOffset(2026, 4, 13, 21, 0, 0, TimeSpan.Zero);

    private readonly IAiUsageEntryRepository _repo = Substitute.For<IAiUsageEntryRepository>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private GetAiUsageDashboard.Handler CreateHandler()
    {
        _tenant.Id.Returns(TenantId);
        _clock.UtcNow.Returns(UtcNow);
        return new GetAiUsageDashboard.Handler(_repo, _tenant, _clock);
    }

    // ── Defaults ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NullPeriodAndGroup_UsesDefaults()
    {
        _repo.GetAggregatedUsageAsync(
            TenantId, Arg.Any<DateTimeOffset>(), UtcNow, "model", 20, Arg.Any<CancellationToken>())
            .Returns(new List<AiUsageAggregate>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAiUsageDashboard.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Period.Should().Be("7d");
        result.Value.GroupBy.Should().Be("model");
        result.Value.Items.Should().BeEmpty();
    }

    // ── GroupBy model ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_GroupByModel_ReturnsAggregates()
    {
        var aggregates = new List<AiUsageAggregate>
        {
            new("gpt-4o", "gpt-4o", 12000, 45, null),
            new("claude-3-5-sonnet-20241022", "claude-3-5-sonnet-20241022", 8000, 30, null),
        };

        _repo.GetAggregatedUsageAsync(
            TenantId, Arg.Any<DateTimeOffset>(), UtcNow, "model", 20, Arg.Any<CancellationToken>())
            .Returns(aggregates);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetAiUsageDashboard.Query("7d", "model", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.GrandTotalTokens.Should().Be(20000);
        result.Value.GrandTotalRequests.Should().Be(75);
        result.Value.GrandTotalEstimatedCostUsd.Should().BeNull();
    }

    // ── GroupBy user ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_GroupByUser_PassesCorrectDimensionToRepo()
    {
        _repo.GetAggregatedUsageAsync(
            TenantId, Arg.Any<DateTimeOffset>(), UtcNow, "user", 10, Arg.Any<CancellationToken>())
            .Returns(new List<AiUsageAggregate>
            {
                new("user-123", "user-123", 5000, 20, null),
            });

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetAiUsageDashboard.Query("30d", "user", 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GroupBy.Should().Be("user");
        result.Value.Items[0].DimensionKey.Should().Be("user-123");

        await _repo.Received(1).GetAggregatedUsageAsync(
            TenantId,
            Arg.Is<DateTimeOffset>(d => d.Date == UtcNow.AddDays(-30).Date),
            UtcNow,
            "user",
            10,
            Arg.Any<CancellationToken>());
    }

    // ── Period 1d ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Period1d_UsesCorrectFrom()
    {
        _repo.GetAggregatedUsageAsync(
            TenantId, Arg.Any<DateTimeOffset>(), UtcNow, "model", 20, Arg.Any<CancellationToken>())
            .Returns(new List<AiUsageAggregate>());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetAiUsageDashboard.Query("1d", "model", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Period.Should().Be("1d");
        result.Value.From.Date.Should().Be(UtcNow.AddDays(-1).Date);
    }

    // ── EstimatedCost ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithCostData_AggregatesGrandCost()
    {
        var aggregates = new List<AiUsageAggregate>
        {
            new("gpt-4o", "gpt-4o", 10000, 30, 0.10m),
            new("claude-3-5-sonnet-20241022", "claude-3-5-sonnet-20241022", 5000, 10, 0.05m),
        };

        _repo.GetAggregatedUsageAsync(
            TenantId, Arg.Any<DateTimeOffset>(), UtcNow, "provider", 20, Arg.Any<CancellationToken>())
            .Returns(aggregates);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetAiUsageDashboard.Query("7d", "provider", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GrandTotalEstimatedCostUsd.Should().Be(0.15m);
    }

    // ── Validator ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("invalid_period", "model", 10)]
    [InlineData("7d", "invalid_group", 10)]
    [InlineData("7d", "model", 0)]
    [InlineData("7d", "model", 101)]
    public async Task Validator_RejectsInvalidInputs(string? period, string? groupBy, int? top)
    {
        var validator = new GetAiUsageDashboard.Validator();
        var result = await validator.ValidateAsync(new GetAiUsageDashboard.Query(period, groupBy, top));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("1d", "model", 5)]
    [InlineData("7d", "user", 20)]
    [InlineData("30d", "provider", 100)]
    [InlineData("90d", "model", 1)]
    [InlineData(null, null, null)]
    public async Task Validator_AcceptsValidInputs(string? period, string? groupBy, int? top)
    {
        var validator = new GetAiUsageDashboard.Validator();
        var result = await validator.ValidateAsync(new GetAiUsageDashboard.Query(period, groupBy, top));
        result.IsValid.Should().BeTrue();
    }
}
