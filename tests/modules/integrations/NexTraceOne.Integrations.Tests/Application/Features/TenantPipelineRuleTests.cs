using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.CreatePipelineRule;
using NexTraceOne.Integrations.Application.Features.DeletePipelineRule;
using NexTraceOne.Integrations.Application.Features.ListPipelineRules;
using NexTraceOne.Integrations.Application.Features.UpdatePipelineRule;
using NexTraceOne.Integrations.Application.Services;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes de unidade para PIP-03/04/06 — TenantPipelineRule, StorageBucketRouter, LogToMetricProcessor.
/// </summary>
public sealed class TenantPipelineRuleTests
{
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ITenantPipelineRuleRepository _ruleRepo = Substitute.For<ITenantPipelineRuleRepository>();
    private readonly IStorageBucketRepository _bucketRepo = Substitute.For<IStorageBucketRepository>();
    private readonly ILogToMetricRuleRepository _metricRepo = Substitute.For<ILogToMetricRuleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public TenantPipelineRuleTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    // ── CreatePipelineRule ──

    [Fact]
    public async Task CreatePipelineRule_ValidCommand_ShouldReturnRuleId()
    {
        var handler = new CreatePipelineRule.Handler(_ruleRepo, _unitOfWork, _clock);
        var command = new CreatePipelineRule.Command(
            TenantId: "tenant-1",
            Name: "Mask PII emails",
            RuleType: PipelineRuleType.Masking,
            SignalType: PipelineSignalType.Log,
            ConditionJson: """{"field": "$.level", "operator": "eq", "value": "error"}""",
            ActionJson: """{"field": "$.body.email", "replacement": "[REDACTED]"}""",
            Priority: 10,
            IsEnabled: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RuleId.Should().NotBeEmpty();
        result.Value.Name.Should().Be("Mask PII emails");
        result.Value.RuleType.Should().Be(PipelineRuleType.Masking);
        result.Value.SignalType.Should().Be(PipelineSignalType.Log);
        result.Value.Priority.Should().Be(10);
        result.Value.IsEnabled.Should().BeTrue();
        await _ruleRepo.Received(1).AddAsync(
            Arg.Is<TenantPipelineRule>(r =>
                r.TenantId == "tenant-1" &&
                r.Name == "Mask PII emails" &&
                r.RuleType == PipelineRuleType.Masking &&
                r.SignalType == PipelineSignalType.Log &&
                r.Priority == 10 &&
                r.IsEnabled),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePipelineRule_InvalidPriority_ShouldFailValidation()
    {
        var validator = new CreatePipelineRule.Validator();
        var command = new CreatePipelineRule.Command(
            TenantId: "t",
            Name: "Rule",
            RuleType: PipelineRuleType.Filtering,
            SignalType: PipelineSignalType.Span,
            ConditionJson: "{}",
            ActionJson: """{"action": "discard"}""",
            Priority: 0);

        var result = await validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Priority");
    }

    [Fact]
    public async Task CreatePipelineRule_EmptyTenantId_ShouldFailValidation()
    {
        var validator = new CreatePipelineRule.Validator();
        var command = new CreatePipelineRule.Command(
            TenantId: "",
            Name: "Rule",
            RuleType: PipelineRuleType.Masking,
            SignalType: PipelineSignalType.Log,
            ConditionJson: "{}",
            ActionJson: "{}",
            Priority: 1);

        var result = await validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }

    // ── UpdatePipelineRule ──

    [Fact]
    public async Task UpdatePipelineRule_NotFound_ShouldReturnFailure()
    {
        _ruleRepo.GetByIdAsync(Arg.Any<TenantPipelineRuleId>(), Arg.Any<CancellationToken>())
            .Returns((TenantPipelineRule?)null);

        var handler = new UpdatePipelineRule.Handler(_ruleRepo, _unitOfWork, _clock);
        var command = new UpdatePipelineRule.Command(
            TenantId: "t", RuleId: Guid.NewGuid(),
            Name: "Updated", RuleType: PipelineRuleType.Masking,
            SignalType: PipelineSignalType.Log,
            ConditionJson: "{}", ActionJson: "{}", Priority: 1, IsEnabled: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PipelineRule.NotFound");
    }

    // ── DeletePipelineRule ──

    [Fact]
    public async Task DeletePipelineRule_NotFound_ShouldReturnFailure()
    {
        _ruleRepo.GetByIdAsync(Arg.Any<TenantPipelineRuleId>(), Arg.Any<CancellationToken>())
            .Returns((TenantPipelineRule?)null);

        var handler = new DeletePipelineRule.Handler(_ruleRepo, _unitOfWork);
        var result = await handler.Handle(
            new DeletePipelineRule.Command("t", Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── ListPipelineRules ──

    [Fact]
    public async Task ListPipelineRules_ShouldReturnPaginatedItems()
    {
        var rules = new List<TenantPipelineRule>
        {
            TenantPipelineRule.Create("tenant-1", "Rule A", PipelineRuleType.Masking,
                PipelineSignalType.Log, "{}", "{}", 10, true, null, DateTimeOffset.UtcNow)
        };

        _ruleRepo.ListAsync(null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((rules, 1));

        var handler = new ListPipelineRules.Handler(_ruleRepo);
        var result = await handler.Handle(
            new ListPipelineRules.Query("tenant-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].Name.Should().Be("Rule A");
    }

    // ── TenantPipelineEngine ──

    [Fact]
    public async Task TenantPipelineEngine_NoRules_ShouldReturnOriginalJson()
    {
        _ruleRepo.ListEnabledBySignalTypeAsync(PipelineSignalType.Log, Arg.Any<CancellationToken>())
            .Returns(new List<TenantPipelineRule>());

        var engine = new TenantPipelineEngine(_ruleRepo, _cache);
        var result = await engine.ProcessAsync(PipelineSignalType.Log, """{"level": "error"}""");

        result.ShouldDiscard.Should().BeFalse();
        result.ProcessedJson.Should().Be("""{"level": "error"}""");
        result.AppliedRules.Should().BeEmpty();
    }

    [Fact]
    public async Task TenantPipelineEngine_FilteringRule_ShouldDiscardMatchingSignal()
    {
        var rule = TenantPipelineRule.Create(
            "t", "Discard debug", PipelineRuleType.Filtering, PipelineSignalType.Log,
            conditionJson: """{"field": "$.level", "operator": "eq", "value": "debug"}""",
            actionJson: """{"action": "discard"}""",
            priority: 1, isEnabled: true, description: null, utcNow: DateTimeOffset.UtcNow);

        _ruleRepo.ListEnabledBySignalTypeAsync(PipelineSignalType.Log, Arg.Any<CancellationToken>())
            .Returns(new List<TenantPipelineRule> { rule });

        var engine = new TenantPipelineEngine(_ruleRepo, _cache);
        var result = await engine.ProcessAsync(PipelineSignalType.Log, """{"level": "debug", "message": "test"}""");

        result.ShouldDiscard.Should().BeTrue();
    }

    [Fact]
    public async Task TenantPipelineEngine_FilteringRule_ShouldNotDiscardNonMatchingSignal()
    {
        var rule = TenantPipelineRule.Create(
            "t", "Discard debug", PipelineRuleType.Filtering, PipelineSignalType.Log,
            conditionJson: """{"field": "$.level", "operator": "eq", "value": "debug"}""",
            actionJson: """{"action": "discard"}""",
            priority: 1, isEnabled: true, description: null, utcNow: DateTimeOffset.UtcNow);

        _ruleRepo.ListEnabledBySignalTypeAsync(PipelineSignalType.Log, Arg.Any<CancellationToken>())
            .Returns(new List<TenantPipelineRule> { rule });

        var engine = new TenantPipelineEngine(_ruleRepo, _cache);
        var result = await engine.ProcessAsync(PipelineSignalType.Log, """{"level": "error", "message": "fail"}""");

        result.ShouldDiscard.Should().BeFalse();
    }

    [Fact]
    public async Task TenantPipelineEngine_MaskingRule_ShouldRedactField()
    {
        var rule = TenantPipelineRule.Create(
            "t", "Mask email", PipelineRuleType.Masking, PipelineSignalType.Log,
            conditionJson: "{}",
            actionJson: """{"field": "$.email", "replacement": "[REDACTED]"}""",
            priority: 1, isEnabled: true, description: null, utcNow: DateTimeOffset.UtcNow);

        _ruleRepo.ListEnabledBySignalTypeAsync(PipelineSignalType.Log, Arg.Any<CancellationToken>())
            .Returns(new List<TenantPipelineRule> { rule });

        var engine = new TenantPipelineEngine(_ruleRepo, _cache);
        var result = await engine.ProcessAsync(
            PipelineSignalType.Log,
            """{"email": "user@example.com", "level": "info"}""");

        result.ShouldDiscard.Should().BeFalse();
        result.ProcessedJson.Should().Contain("[REDACTED]");
        result.ProcessedJson.Should().NotContain("user@example.com");
        result.AppliedRules.Should().Contain("Mask email");
    }

    // ── StorageBucketRouter ──

    [Fact]
    public async Task StorageBucketRouter_NoBuckets_ShouldReturnDefaultRoute()
    {
        _bucketRepo.ListEnabledOrderedAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StorageBucket>());

        var router = new StorageBucketRouter(_bucketRepo, _cache);
        var result = await router.RouteAsync("""{"level": "info"}""");

        result.BucketName.Should().Be("default");
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task StorageBucketRouter_MatchingFilter_ShouldReturnCorrectBucket()
    {
        var debugBucket = StorageBucket.Create(
            "t", "debug", StorageBucketBackendType.ClickHouse,
            retentionDays: 3,
            filterJson: """{"field": "$.level", "operator": "eq", "value": "debug"}""",
            priority: 1, isEnabled: true, isFallback: false, description: null,
            utcNow: DateTimeOffset.UtcNow);

        _bucketRepo.ListEnabledOrderedAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StorageBucket> { debugBucket });

        var router = new StorageBucketRouter(_bucketRepo, _cache);
        var result = await router.RouteAsync("""{"level": "debug", "msg": "test"}""");

        result.BucketName.Should().Be("debug");
        result.BackendType.Should().Be(StorageBucketBackendType.ClickHouse);
        result.RetentionDays.Should().Be(3);
        result.IsDefault.Should().BeFalse();
    }

    // ── TenantPipelineRule domain ──

    [Fact]
    public void TenantPipelineRule_Enable_ShouldSetIsEnabledTrue()
    {
        var rule = TenantPipelineRule.Create(
            "t", "R", PipelineRuleType.Filtering, PipelineSignalType.Span,
            "{}", "{}", 1, false, null, DateTimeOffset.UtcNow);

        rule.Enable(DateTimeOffset.UtcNow);

        rule.IsEnabled.Should().BeTrue();
        rule.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void TenantPipelineRule_Disable_ShouldSetIsEnabledFalse()
    {
        var rule = TenantPipelineRule.Create(
            "t", "R", PipelineRuleType.Masking, PipelineSignalType.Log,
            "{}", "{}", 1, true, null, DateTimeOffset.UtcNow);

        rule.Disable(DateTimeOffset.UtcNow);

        rule.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void StorageBucket_Create_ShouldHaveCorrectProperties()
    {
        var bucket = StorageBucket.Create(
            "t", "audit", StorageBucketBackendType.Elasticsearch,
            retentionDays: 2555, filterJson: null, priority: 100,
            isEnabled: true, isFallback: true, description: "Audit bucket",
            utcNow: DateTimeOffset.UtcNow);

        bucket.BucketName.Should().Be("audit");
        bucket.BackendType.Should().Be(StorageBucketBackendType.Elasticsearch);
        bucket.RetentionDays.Should().Be(2555);
        bucket.IsFallback.Should().BeTrue();
        bucket.IsEnabled.Should().BeTrue();
    }
}
