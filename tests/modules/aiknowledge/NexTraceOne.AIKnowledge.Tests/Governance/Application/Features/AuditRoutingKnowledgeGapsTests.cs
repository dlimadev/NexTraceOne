using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListAuditEntries;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListKnowledgeSources;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListRoutingStrategies;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para ListAuditEntries, ListKnowledgeSources, ListRoutingStrategies.
/// Cobre trilha de auditoria de IA, fontes de conhecimento e estratégias de roteamento.
/// </summary>
public sealed class AuditRoutingKnowledgeGapsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IAiUsageEntryRepository _usageEntryRepo = Substitute.For<IAiUsageEntryRepository>();
    private readonly IAiKnowledgeSourceRepository _knowledgeSourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
    private readonly IAiRoutingStrategyRepository _routingStrategyRepo = Substitute.For<IAiRoutingStrategyRepository>();

    // ── ListAuditEntries ───────────────────────────────────────────────────

    [Fact]
    public async Task ListAuditEntries_NoFilters_ReturnsAllEntries()
    {
        var modelId = Guid.NewGuid();
        var entries = new List<AIUsageEntry>
        {
            CreateUsageEntry(modelId, "user-1", AIClientType.Web, UsageResult.Allowed),
            CreateUsageEntry(modelId, "user-2", AIClientType.VsCode, UsageResult.Allowed),
        };
        _usageEntryRepo.ListAsync(
            null, null, null, null, null, null, 50, Arg.Any<CancellationToken>())
            .Returns(entries.AsReadOnly());

        var handler = new ListAuditEntries.Handler(_usageEntryRepo);
        var result = await handler.Handle(
            new ListAuditEntries.Query(null, null, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAuditEntries_FilterByUserId_PassesUserIdToRepository()
    {
        var modelId = Guid.NewGuid();
        var entries = new List<AIUsageEntry>
        {
            CreateUsageEntry(modelId, "user-1", AIClientType.Web, UsageResult.Allowed),
        };
        _usageEntryRepo.ListAsync(
            "user-1", null, null, null, null, null, 50, Arg.Any<CancellationToken>())
            .Returns(entries.AsReadOnly());

        var handler = new ListAuditEntries.Handler(_usageEntryRepo);
        var result = await handler.Handle(
            new ListAuditEntries.Query("user-1", null, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task ListAuditEntries_FilterByModelId_PassesModelIdToRepository()
    {
        var modelId = Guid.NewGuid();
        var entries = new List<AIUsageEntry>
        {
            CreateUsageEntry(modelId, "user-1", AIClientType.Web, UsageResult.Allowed),
        };
        _usageEntryRepo.ListAsync(
            null, modelId, null, null, null, null, 50, Arg.Any<CancellationToken>())
            .Returns(entries.AsReadOnly());

        var handler = new ListAuditEntries.Handler(_usageEntryRepo);
        var result = await handler.Handle(
            new ListAuditEntries.Query(null, modelId, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListAuditEntries_FilterByDateRange_PassesDatesToRepository()
    {
        var start = FixedNow.AddDays(-7);
        var end = FixedNow;
        _usageEntryRepo.ListAsync(
            null, null, start, end, null, null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<AIUsageEntry>().AsReadOnly());

        var handler = new ListAuditEntries.Handler(_usageEntryRepo);
        var result = await handler.Handle(
            new ListAuditEntries.Query(null, null, start, end, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _usageEntryRepo.Received(1).ListAsync(
            null, null, start, end, null, null, 50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAuditEntries_CustomPageSize_PassesPageSizeToRepository()
    {
        _usageEntryRepo.ListAsync(
            null, null, null, null, null, null, 100, Arg.Any<CancellationToken>())
            .Returns(new List<AIUsageEntry>().AsReadOnly());

        var handler = new ListAuditEntries.Handler(_usageEntryRepo);
        var result = await handler.Handle(
            new ListAuditEntries.Query(null, null, null, null, null, null, 100), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _usageEntryRepo.Received(1).ListAsync(
            null, null, null, null, null, null, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAuditEntries_EmptyResult_ReturnsEmptyList()
    {
        _usageEntryRepo.ListAsync(
            null, null, null, null, null, null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<AIUsageEntry>().AsReadOnly());

        var handler = new ListAuditEntries.Handler(_usageEntryRepo);
        var result = await handler.Handle(
            new ListAuditEntries.Query(null, null, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    // ── ListKnowledgeSources ───────────────────────────────────────────────

    [Fact]
    public async Task ListKnowledgeSources_NoFilters_ReturnsAllSources()
    {
        var sources = new List<AIKnowledgeSource>
        {
            CreateKnowledgeSource("catalog-source", KnowledgeSourceType.Service),
            CreateKnowledgeSource("runbook-source", KnowledgeSourceType.Runbook),
        };
        _knowledgeSourceRepo.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(sources.AsReadOnly());

        var handler = new ListKnowledgeSources.Handler(_knowledgeSourceRepo);
        var result = await handler.Handle(new ListKnowledgeSources.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListKnowledgeSources_FilterBySourceType_PassesTypeToRepository()
    {
        var sources = new List<AIKnowledgeSource>
        {
            CreateKnowledgeSource("catalog-source", KnowledgeSourceType.Service),
        };
        _knowledgeSourceRepo.ListAsync(KnowledgeSourceType.Service, null, Arg.Any<CancellationToken>())
            .Returns(sources.AsReadOnly());

        var handler = new ListKnowledgeSources.Handler(_knowledgeSourceRepo);
        var result = await handler.Handle(
            new ListKnowledgeSources.Query(KnowledgeSourceType.Service, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].SourceType.Should().Be(KnowledgeSourceType.Service.ToString());
    }

    [Fact]
    public async Task ListKnowledgeSources_FilterActiveOnly_PassesFlagToRepository()
    {
        _knowledgeSourceRepo.ListAsync(null, true, Arg.Any<CancellationToken>())
            .Returns(new List<AIKnowledgeSource>().AsReadOnly());

        var handler = new ListKnowledgeSources.Handler(_knowledgeSourceRepo);
        var result = await handler.Handle(new ListKnowledgeSources.Query(null, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _knowledgeSourceRepo.Received(1).ListAsync(null, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListKnowledgeSources_ItemsMappedCorrectly()
    {
        var source = CreateKnowledgeSource("ops-runbooks", KnowledgeSourceType.Runbook);
        _knowledgeSourceRepo.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<AIKnowledgeSource> { source }.AsReadOnly());

        var handler = new ListKnowledgeSources.Handler(_knowledgeSourceRepo);
        var result = await handler.Handle(new ListKnowledgeSources.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items[0];
        item.Name.Should().Be("ops-runbooks");
        item.IsActive.Should().BeTrue();
        item.SourceType.Should().Be("Runbook");
    }

    // ── ListRoutingStrategies ──────────────────────────────────────────────

    [Fact]
    public async Task ListRoutingStrategies_NoFilters_ReturnsAllStrategies()
    {
        var strategies = new List<AIRoutingStrategy>
        {
            CreateRoutingStrategy("engineer-internal", "Engineer", "chat", AIRoutingPath.InternalOnly),
            CreateRoutingStrategy("exec-external", "*", "analysis", AIRoutingPath.ExternalEscalation),
        };
        _routingStrategyRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(strategies.AsReadOnly());

        var handler = new ListRoutingStrategies.Handler(_routingStrategyRepo);
        var result = await handler.Handle(new ListRoutingStrategies.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListRoutingStrategies_FilterActiveOnly_PassesFlagToRepository()
    {
        _routingStrategyRepo.ListAsync(true, Arg.Any<CancellationToken>())
            .Returns(new List<AIRoutingStrategy>().AsReadOnly());

        var handler = new ListRoutingStrategies.Handler(_routingStrategyRepo);
        var result = await handler.Handle(new ListRoutingStrategies.Query(true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _routingStrategyRepo.Received(1).ListAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRoutingStrategies_ItemsMappedCorrectly()
    {
        var strategy = CreateRoutingStrategy("eng-strategy", "Engineer", "chat", AIRoutingPath.InternalOnly);
        _routingStrategyRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<AIRoutingStrategy> { strategy }.AsReadOnly());

        var handler = new ListRoutingStrategies.Handler(_routingStrategyRepo);
        var result = await handler.Handle(new ListRoutingStrategies.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items[0];
        item.Name.Should().Be("eng-strategy");
        item.TargetPersona.Should().Be("Engineer");
        item.IsActive.Should().BeTrue();
        item.PreferredPath.Should().Be("InternalOnly");
    }

    [Fact]
    public async Task ListRoutingStrategies_EmptyResult_ReturnsEmptyList()
    {
        _routingStrategyRepo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<AIRoutingStrategy>().AsReadOnly());

        var handler = new ListRoutingStrategies.Handler(_routingStrategyRepo);
        var result = await handler.Handle(new ListRoutingStrategies.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AIUsageEntry CreateUsageEntry(
        Guid modelId, string userId, AIClientType clientType, UsageResult usageResult) =>
        AIUsageEntry.Record(
            userId, $"User {userId}", modelId, "gpt-4o", "OpenAI",
            isInternal: false, timestamp: DateTimeOffset.UtcNow,
            promptTokens: 100, completionTokens: 50,
            policyId: null, policyName: null,
            result: usageResult, contextScope: "service-catalog",
            clientType: clientType, correlationId: Guid.NewGuid().ToString("N"));

    private static AIKnowledgeSource CreateKnowledgeSource(string name, KnowledgeSourceType sourceType) =>
        AIKnowledgeSource.Register(
            name, $"Description for {name}", sourceType,
            endpointOrPath: $"/api/{name}", priority: 10,
            registeredAt: DateTimeOffset.UtcNow);

    private static AIRoutingStrategy CreateRoutingStrategy(
        string name, string persona, string useCase, AIRoutingPath path) =>
        AIRoutingStrategy.Create(
            name, $"Strategy for {name}", persona, useCase, "*",
            path, maxSensitivityLevel: 3, allowExternalEscalation: false,
            priority: 100, registeredAt: DateTimeOffset.UtcNow);
}
