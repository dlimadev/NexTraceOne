using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetModelCostAttribution;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários — WAVE A.4: Model Cost Attribution.
/// Cobre GetModelCostAttribution — relatório de atribuição de custo de tokens por modelo.
/// </summary>
public sealed class ModelCostAttributionTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetModelCostAttribution_ReturnsAggregatedByModel()
    {
        var repo = Substitute.For<IAiTokenUsageLedgerRepository>();
        var entries = new List<AiTokenUsageLedger>
        {
            CreateEntry("gpt-4o", "GPT-4o", 1000, 500, 0.05m),
            CreateEntry("gpt-4o", "GPT-4o", 800, 300, 0.04m),
            CreateEntry("claude-3", "Claude 3", 600, 400, 0.03m),
        };
        repo.ListByPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(entries);

        var handler = new GetModelCostAttribution.Handler(repo);
        var result = await handler.Handle(new GetModelCostAttribution.Query(PeriodDays: 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRequests.Should().Be(3);
        result.Value.ByModel.Should().HaveCount(2);
        result.Value.ByModel[0].ModelId.Should().Be("gpt-4o");
        result.Value.ByModel[0].TotalRequests.Should().Be(2);
    }

    [Fact]
    public async Task GetModelCostAttribution_FilterByModelId_ReturnsOnlyMatching()
    {
        var repo = Substitute.For<IAiTokenUsageLedgerRepository>();
        repo.ListByPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<AiTokenUsageLedger>
            {
                CreateEntry("gpt-4o", "GPT-4o", 1000, 500, 0.05m),
                CreateEntry("claude-3", "Claude 3", 600, 400, 0.03m),
            });

        var handler = new GetModelCostAttribution.Handler(repo);
        var result = await handler.Handle(new GetModelCostAttribution.Query(ModelId: "gpt-4o"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRequests.Should().Be(1);
        result.Value.ByModel.Should().HaveCount(1);
        result.Value.ByModel[0].ModelId.Should().Be("gpt-4o");
    }

    [Fact]
    public async Task GetModelCostAttribution_EmptyLedger_ReturnsZeroValues()
    {
        var repo = Substitute.For<IAiTokenUsageLedgerRepository>();
        repo.ListByPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<AiTokenUsageLedger>());

        var handler = new GetModelCostAttribution.Handler(repo);
        var result = await handler.Handle(new GetModelCostAttribution.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRequests.Should().Be(0);
        result.Value.TotalEstimatedCostUsd.Should().Be(0m);
        result.Value.ByModel.Should().BeEmpty();
    }

    [Fact]
    public void GetModelCostAttribution_Validator_InvalidPeriod_FailsValidation()
    {
        var validator = new GetModelCostAttribution.Validator();
        var result = validator.Validate(new GetModelCostAttribution.Query(PeriodDays: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetModelCostAttribution_Validator_TooLongPeriod_FailsValidation()
    {
        var validator = new GetModelCostAttribution.Validator();
        var result = validator.Validate(new GetModelCostAttribution.Query(PeriodDays: 400));
        result.IsValid.Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AiTokenUsageLedger CreateEntry(string modelId, string modelName, int promptTokens, int completionTokens, decimal cost)
    {
        return AiTokenUsageLedger.Record(
            userId: "user-1",
            tenantId: Guid.NewGuid(),
            providerId: "openai",
            modelId: modelId,
            modelName: modelName,
            promptTokens: promptTokens,
            completionTokens: completionTokens,
            totalTokens: promptTokens + completionTokens,
            policyId: null,
            policyName: null,
            isBlocked: false,
            blockReason: null,
            requestId: Guid.NewGuid().ToString(),
            executionId: Guid.NewGuid().ToString(),
            timestamp: FixedNow,
            status: "success",
            durationMs: 100,
            costPerInputToken: null,
            costPerOutputToken: null,
            estimatedCostUsd: cost,
            costCurrency: "USD");
    }
}
