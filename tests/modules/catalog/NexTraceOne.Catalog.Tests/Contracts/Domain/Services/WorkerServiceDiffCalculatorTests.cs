using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes do WorkerServiceDiffCalculator.
/// Valida a detecção de breaking changes, additive changes e non-breaking changes
/// em Background Service Contracts, cobrindo todos os tipos de mudança suportados.
/// </summary>
public sealed class WorkerServiceDiffCalculatorTests
{
    private static string FullSpec(
        string serviceName = "OrderExpirationJob",
        string category = "Job",
        string triggerType = "Cron",
        string? schedule = "0 * * * *",
        string? timeout = "PT30M",
        bool allowsConcurrency = false,
        string inputsJson = """{"orderId": "Guid"}""",
        string outputsJson = """{"expiredCount": "int"}""",
        string sideEffectsJson = """["Writes to order_history"]""") =>
        $$"""
        {
            "serviceName": "{{serviceName}}",
            "category": "{{category}}",
            "triggerType": "{{triggerType}}",
            {{(schedule is not null ? $"\"scheduleExpression\": \"{schedule}\"," : "")}}
            {{(timeout is not null ? $"\"timeoutExpression\": \"{timeout}\"," : "")}}
            "allowsConcurrency": {{allowsConcurrency.ToString().ToLowerInvariant()}},
            "inputs": {{inputsJson}},
            "outputs": {{outputsJson}},
            "sideEffects": {{sideEffectsJson}}
        }
        """;

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_SpecsAreIdentical()
    {
        var spec = FullSpec();

        var result = WorkerServiceDiffCalculator.ComputeDiff(spec, spec);

        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
        result.BreakingChanges.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_Should_ReturnBreaking_When_ServiceNameChanged()
    {
        var baseSpec = FullSpec(serviceName: "OrderExpirationJob");
        var targetSpec = FullSpec(serviceName: "OrderCleanupJob");

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "ServiceNameChanged");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnBreaking_When_TriggerTypeChanged()
    {
        var baseSpec = FullSpec(triggerType: "Cron");
        var targetSpec = FullSpec(triggerType: "OnDemand");

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "TriggerTypeChanged");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnBreaking_When_ScheduleExpressionChanged()
    {
        var baseSpec = FullSpec(schedule: "0 * * * *");
        var targetSpec = FullSpec(schedule: "0 0 * * *");

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "ScheduleExpressionChanged");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnBreaking_When_InputRemoved()
    {
        var baseSpec = FullSpec(inputsJson: """{"orderId": "Guid", "priority": "int"}""");
        var targetSpec = FullSpec(inputsJson: """{"orderId": "Guid"}""");

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "InputRemoved" && c.Method == "priority");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnAdditive_When_InputAdded()
    {
        var baseSpec = FullSpec(inputsJson: """{"orderId": "Guid"}""");
        var targetSpec = FullSpec(inputsJson: """{"orderId": "Guid", "priority": "int"}""");

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
        result.AdditiveChanges.Should().Contain(c => c.ChangeType == "InputAdded" && c.Method == "priority");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnBreaking_When_OutputRemoved()
    {
        var baseSpec = FullSpec(outputsJson: """{"expiredCount": "int", "errorCount": "int"}""");
        var targetSpec = FullSpec(outputsJson: """{"expiredCount": "int"}""");

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "OutputRemoved" && c.Method == "errorCount");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnBreaking_When_ConcurrencyDisabled()
    {
        var baseSpec = FullSpec(allowsConcurrency: true);
        var targetSpec = FullSpec(allowsConcurrency: false);

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "ConcurrencyDisabled");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnAdditive_When_ConcurrencyEnabled()
    {
        var baseSpec = FullSpec(allowsConcurrency: false);
        var targetSpec = FullSpec(allowsConcurrency: true);

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.AdditiveChanges.Should().Contain(c => c.ChangeType == "ConcurrencyEnabled");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_CategoryChanged()
    {
        var baseSpec = FullSpec(category: "Job");
        var targetSpec = FullSpec(category: "Worker");

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        // Category change alone should be non-breaking
        result.NonBreakingChanges.Should().Contain(c => c.ChangeType == "CategoryChanged");
        result.BreakingChanges.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_Should_ReturnAdditive_When_SideEffectAdded()
    {
        var baseSpec = FullSpec(sideEffectsJson: """["Writes to order_history"]""");
        var targetSpec = FullSpec(sideEffectsJson: """["Writes to order_history", "Publishes event"]""");

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.AdditiveChanges.Should().Contain(c => c.ChangeType == "SideEffectAdded");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnBreaking_When_TimeoutRemoved()
    {
        var baseSpec = FullSpec(timeout: "PT30M");
        var targetSpec = FullSpec(timeout: null);

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.BreakingChanges.Should().Contain(c => c.ChangeType == "TimeoutRemoved");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnAdditive_When_TimeoutAdded()
    {
        var baseSpec = FullSpec(timeout: null);
        var targetSpec = FullSpec(timeout: "PT1H");

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.AdditiveChanges.Should().Contain(c => c.ChangeType == "TimeoutAdded");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_TimeoutChanged()
    {
        var baseSpec = FullSpec(timeout: "PT30M");
        var targetSpec = FullSpec(timeout: "PT1H");

        var result = WorkerServiceDiffCalculator.ComputeDiff(baseSpec, targetSpec);

        result.NonBreakingChanges.Should().Contain(c => c.ChangeType == "TimeoutChanged");
        result.BreakingChanges.Should().NotContain(c => c.ChangeType == "TimeoutChanged");
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_EmptySpecsCompared()
    {
        var result = WorkerServiceDiffCalculator.ComputeDiff("{}", "{}");

        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
        result.BreakingChanges.Should().BeEmpty();
    }
}
