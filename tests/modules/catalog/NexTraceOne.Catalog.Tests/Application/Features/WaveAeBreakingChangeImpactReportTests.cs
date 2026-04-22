using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSchemaBreakingChangeImpactReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AE.2 — GetSchemaBreakingChangeImpactReport.
/// Cobre BreakingChangeImpactTier, DirectConsumers, IndirectConsumers,
/// ImpactScore, MitigationOptions, ByEnvironment e Validator.
/// </summary>
public sealed class WaveAeBreakingChangeImpactReportTests
{
    private const string TenantId = "tenant-ae2";

    private static GetSchemaBreakingChangeImpactReport.Handler CreateHandler(
        IReadOnlyList<BreakingChangeEntry> entries)
    {
        var reader = Substitute.For<IBreakingChangeImpactReader>();
        reader.ListBreakingChangesByTenantAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(entries));
        return new GetSchemaBreakingChangeImpactReport.Handler(reader);
    }

    private static BreakingChangeEntry MakeEntry(
        string changeId,
        string apiId,
        string producer,
        IReadOnlyList<ConsumerServiceInfo> consumers) =>
        new(changeId, apiId, producer, "1.0.0", "2.0.0",
            new DateTimeOffset(2026, 4, 10, 9, 0, 0, TimeSpan.Zero),
            $"Breaking change in {apiId}", consumers);

    private static ConsumerServiceInfo MakeConsumer(
        string name, string tier = "Standard", string env = "production",
        IReadOnlyList<string>? dependents = null) =>
        new(name, tier, env, dependents ?? new List<string>());

    private static GetSchemaBreakingChangeImpactReport.Query DefaultQuery()
        => new(TenantId: TenantId);

    // ── Contained tier ────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Contained_when_only_experimental_consumers()
    {
        var entries = new List<BreakingChangeEntry>
        {
            MakeEntry("bc-1", "api-1", "svc-a", new List<ConsumerServiceInfo>
            {
                MakeConsumer("svc-exp", "Experimental")
            })
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllChanges.Single().ImpactTier
            .Should().Be(GetSchemaBreakingChangeImpactReport.BreakingChangeImpactTier.Contained);
    }

    // ── Moderate tier ─────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Moderate_when_standard_consumer_affected()
    {
        var entries = new List<BreakingChangeEntry>
        {
            MakeEntry("bc-2", "api-2", "svc-a", new List<ConsumerServiceInfo>
            {
                MakeConsumer("svc-std", "Standard")
            })
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllChanges.Single().ImpactTier
            .Should().Be(GetSchemaBreakingChangeImpactReport.BreakingChangeImpactTier.Moderate);
    }

    // ── Significant tier ──────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Significant_when_critical_consumer_affected()
    {
        var entries = new List<BreakingChangeEntry>
        {
            MakeEntry("bc-3", "api-3", "svc-a", new List<ConsumerServiceInfo>
            {
                MakeConsumer("svc-crit", "Critical")
            })
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllChanges.Single().ImpactTier
            .Should().Be(GetSchemaBreakingChangeImpactReport.BreakingChangeImpactTier.Significant);
        result.Value.HighImpactBreakingChanges.Should().Be(1);
    }

    // ── Widespread tier (≥5 total affected) ───────────────────────────────

    [Fact]
    public async Task Returns_Widespread_when_total_affected_ge_5()
    {
        var consumers = Enumerable.Range(1, 5)
            .Select(i => MakeConsumer($"consumer-{i}", "Standard"))
            .ToList<ConsumerServiceInfo>();

        var entries = new List<BreakingChangeEntry>
        {
            MakeEntry("bc-4", "api-4", "svc-a", consumers)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllChanges.Single().ImpactTier
            .Should().Be(GetSchemaBreakingChangeImpactReport.BreakingChangeImpactTier.Widespread);
    }

    // ── IndirectConsumers via dependents ─────────────────────────────────

    [Fact]
    public async Task Calculates_indirect_consumers_from_dependent_names()
    {
        var entries = new List<BreakingChangeEntry>
        {
            MakeEntry("bc-5", "api-5", "svc-a", new List<ConsumerServiceInfo>
            {
                MakeConsumer("svc-std", "Standard", "production",
                    new List<string> { "svc-dep-1", "svc-dep-2" })
            })
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        var entry = result.Value.AllChanges.Single();
        entry.IndirectConsumerCount.Should().Be(2);
        entry.TotalAffectedServices.Should().Be(3); // 1 direct + 2 indirect
    }

    // ── ImpactScore calculation ───────────────────────────────────────────

    [Fact]
    public async Task ImpactScore_uses_tier_weights_for_direct_consumers()
    {
        var entries = new List<BreakingChangeEntry>
        {
            MakeEntry("bc-6", "api-6", "svc-a", new List<ConsumerServiceInfo>
            {
                MakeConsumer("svc-crit", "Critical"),   // weight 3
                MakeConsumer("svc-std", "Standard")     // weight 2
            })
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        // ImpactScore = 3 + 2 = 5 (both direct, total<5 → Significant because Critical present)
        result.Value.AllChanges.Single().ImpactScore.Should().Be(5);
    }

    // ── ByEnvironment breakdown ───────────────────────────────────────────

    [Fact]
    public async Task ByEnvironment_groups_consumers_by_environment()
    {
        var entries = new List<BreakingChangeEntry>
        {
            MakeEntry("bc-7", "api-7", "svc-a", new List<ConsumerServiceInfo>
            {
                MakeConsumer("svc-prod", "Standard", "production"),
                MakeConsumer("svc-staging", "Experimental", "staging")
            })
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        var envs = result.Value.AllChanges.Single().ByEnvironment;
        envs.Should().Contain(e => e.Environment == "production");
        envs.Should().Contain(e => e.Environment == "staging");
        envs.First(e => e.Environment == "production").HasCriticalConsumer.Should().BeFalse();
    }

    // ── MitigationOptions ─────────────────────────────────────────────────

    [Fact]
    public async Task MitigationOptions_includes_notify_for_significant_impact()
    {
        var entries = new List<BreakingChangeEntry>
        {
            MakeEntry("bc-8", "api-8", "svc-a", new List<ConsumerServiceInfo>
            {
                MakeConsumer("svc-crit", "Critical", "production")
            })
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        var mitigations = result.Value.AllChanges.Single().MitigationOptions;
        mitigations.Should().Contain(m => m.Option == "notify-consumers");
        mitigations.Should().Contain(m => m.Option == "staged-rollout");
    }

    // ── Empty result ──────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_empty_report_when_no_breaking_changes()
    {
        var handler = CreateHandler(new List<BreakingChangeEntry>());
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalBreakingChanges.Should().Be(0);
        result.Value.HighImpactBreakingChanges.Should().Be(0);
    }

    // ── TierDistribution ──────────────────────────────────────────────────

    [Fact]
    public async Task TierDistribution_sums_to_total_breaking_changes()
    {
        var entries = new List<BreakingChangeEntry>
        {
            MakeEntry("bc-9",  "api-9",  "svc-a", new[] { MakeConsumer("c1", "Experimental") }),
            MakeEntry("bc-10", "api-10", "svc-b", new[] { MakeConsumer("c2", "Critical") })
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        var dist = result.Value.TierDistribution;
        (dist.ContainedCount + dist.ModerateCount + dist.SignificantCount + dist.WidespreadCount)
            .Should().Be(result.Value.TotalBreakingChanges);
    }

    // ── MaxConsumers limit ────────────────────────────────────────────────

    [Fact]
    public async Task MaxConsumers_limits_consumers_analysed_per_change()
    {
        var consumers = Enumerable.Range(1, 20)
            .Select(i => MakeConsumer($"svc-{i}", "Standard"))
            .ToList<ConsumerServiceInfo>();

        var entries = new List<BreakingChangeEntry>
        {
            MakeEntry("bc-11", "api-11", "svc-a", consumers)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(
            new GetSchemaBreakingChangeImpactReport.Query(TenantId: TenantId, MaxConsumers: 5),
            CancellationToken.None);

        result.Value.AllChanges.Single().DirectConsumerCount.Should().Be(5);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_rejects_empty_tenantId()
    {
        var validator = new GetSchemaBreakingChangeImpactReport.Validator();
        validator.Validate(new GetSchemaBreakingChangeImpactReport.Query(TenantId: ""))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_rejects_max_hop_depth_out_of_range()
    {
        var validator = new GetSchemaBreakingChangeImpactReport.Validator();
        validator.Validate(
            new GetSchemaBreakingChangeImpactReport.Query(TenantId: TenantId, MaxHopDepth: 10))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_accepts_valid_query()
    {
        var validator = new GetSchemaBreakingChangeImpactReport.Validator();
        validator.Validate(DefaultQuery()).IsValid.Should().BeTrue();
    }
}
