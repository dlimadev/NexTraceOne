using FluentAssertions;
using NSubstitute;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using SimulateContractChangeImpactFeature = NexTraceOne.Catalog.Application.Contracts.Features.SimulateContractChangeImpact.SimulateContractChangeImpact;
using GetLatestTopologySnapshotFeature = NexTraceOne.Catalog.Application.Graph.Features.GetLatestTopologySnapshot.GetLatestTopologySnapshot;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave D.1 — Digital Twin.
/// Cobre SimulateContractChangeImpact e GetLatestTopologySnapshot.
/// </summary>
public sealed class DigitalTwinD1Tests
{
    private static readonly Guid ApiAssetId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static ConsumerExpectation MakeConsumer(string name = "billing-service", string domain = "Finance", bool isActive = true)
    {
        var c = ConsumerExpectation.Create(ApiAssetId, name, domain, "{}", null, FixedNow);
        if (!isActive) c.Deactivate();
        return c;
    }

    // ── SimulateContractChangeImpact ──────────────────────────────────────

    [Fact]
    public async Task SimulateContractChangeImpact_Breaking_Returns_Critical_For_All_Active_Consumers()
    {
        var repo = Substitute.For<IConsumerExpectationRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ConsumerExpectation>)[MakeConsumer("billing-service"), MakeConsumer("payment-service")]);

        var handler = new SimulateContractChangeImpactFeature.Handler(repo);
        var result = await handler.Handle(
            new SimulateContractChangeImpactFeature.Query(ApiAssetId, WhatIfChangeType.Breaking),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConsumerImpacts.Should().AllSatisfy(i => i.ImpactLevel.Should().Be(WhatIfImpactLevel.Critical));
    }

    [Fact]
    public async Task SimulateContractChangeImpact_Deprecation_Returns_High_For_Active_Consumers()
    {
        var repo = Substitute.For<IConsumerExpectationRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ConsumerExpectation>)[MakeConsumer()]);

        var handler = new SimulateContractChangeImpactFeature.Handler(repo);
        var result = await handler.Handle(
            new SimulateContractChangeImpactFeature.Query(ApiAssetId, WhatIfChangeType.Deprecation),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConsumerImpacts.Should().ContainSingle(i => i.ImpactLevel == WhatIfImpactLevel.High);
    }

    [Fact]
    public async Task SimulateContractChangeImpact_NonBreaking_Returns_Medium()
    {
        var repo = Substitute.For<IConsumerExpectationRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ConsumerExpectation>)[MakeConsumer()]);

        var handler = new SimulateContractChangeImpactFeature.Handler(repo);
        var result = await handler.Handle(
            new SimulateContractChangeImpactFeature.Query(ApiAssetId, WhatIfChangeType.NonBreaking),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConsumerImpacts.Should().ContainSingle(i => i.ImpactLevel == WhatIfImpactLevel.Medium);
    }

    [Fact]
    public async Task SimulateContractChangeImpact_Additive_Returns_Low()
    {
        var repo = Substitute.For<IConsumerExpectationRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ConsumerExpectation>)[MakeConsumer()]);

        var handler = new SimulateContractChangeImpactFeature.Handler(repo);
        var result = await handler.Handle(
            new SimulateContractChangeImpactFeature.Query(ApiAssetId, WhatIfChangeType.Additive),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConsumerImpacts.Should().ContainSingle(i => i.ImpactLevel == WhatIfImpactLevel.Low);
    }

    [Fact]
    public async Task SimulateContractChangeImpact_No_Consumers_Returns_No_Impact()
    {
        var repo = Substitute.For<IConsumerExpectationRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ConsumerExpectation>)[]);

        var handler = new SimulateContractChangeImpactFeature.Handler(repo);
        var result = await handler.Handle(
            new SimulateContractChangeImpactFeature.Query(ApiAssetId, WhatIfChangeType.Breaking),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalConsumers.Should().Be(0);
        result.Value.ImpactedConsumers.Should().Be(0);
        result.Value.ConsumerImpacts.Should().BeEmpty();
    }

    [Fact]
    public async Task SimulateContractChangeImpact_Inactive_Consumers_Are_Excluded()
    {
        var active = MakeConsumer("active-svc", isActive: true);
        var inactive = MakeConsumer("inactive-svc", isActive: false);

        var repo = Substitute.For<IConsumerExpectationRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ConsumerExpectation>)[active, inactive]);

        var handler = new SimulateContractChangeImpactFeature.Handler(repo);
        var result = await handler.Handle(
            new SimulateContractChangeImpactFeature.Query(ApiAssetId, WhatIfChangeType.Breaking),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalConsumers.Should().Be(1);
        result.Value.ConsumerImpacts.Should().ContainSingle(i => i.ConsumerServiceName == "active-svc");
    }

    [Fact]
    public async Task SimulateContractChangeImpact_Breaking_Sets_OverallRisk_Critical()
    {
        var repo = Substitute.For<IConsumerExpectationRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ConsumerExpectation>)[MakeConsumer()]);

        var handler = new SimulateContractChangeImpactFeature.Handler(repo);
        var result = await handler.Handle(
            new SimulateContractChangeImpactFeature.Query(ApiAssetId, WhatIfChangeType.Breaking),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallRisk.Should().Be("critical");
    }

    [Fact]
    public async Task SimulateContractChangeImpact_Additive_Sets_OverallRisk_Low()
    {
        var repo = Substitute.For<IConsumerExpectationRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ConsumerExpectation>)[MakeConsumer()]);

        var handler = new SimulateContractChangeImpactFeature.Handler(repo);
        var result = await handler.Handle(
            new SimulateContractChangeImpactFeature.Query(ApiAssetId, WhatIfChangeType.Additive),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallRisk.Should().Be("low");
    }

    [Fact]
    public async Task SimulateContractChangeImpact_No_Consumers_Sets_OverallRisk_None()
    {
        var repo = Substitute.For<IConsumerExpectationRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ConsumerExpectation>)[]);

        var handler = new SimulateContractChangeImpactFeature.Handler(repo);
        var result = await handler.Handle(
            new SimulateContractChangeImpactFeature.Query(ApiAssetId, WhatIfChangeType.Breaking),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallRisk.Should().Be("none");
    }

    // ── GetLatestTopologySnapshot ─────────────────────────────────────────

    [Fact]
    public async Task GetLatestTopologySnapshot_Returns_NotFound_When_No_Snapshot()
    {
        var repo = Substitute.For<IGraphSnapshotRepository>();
        repo.GetLatestAsync(Arg.Any<CancellationToken>()).Returns((GraphSnapshot?)null);

        var handler = new GetLatestTopologySnapshotFeature.Handler(repo);
        var result = await handler.Handle(new GetLatestTopologySnapshotFeature.Query(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("GraphSnapshot");
    }

    [Fact]
    public async Task GetLatestTopologySnapshot_Returns_Snapshot_With_Node_Count()
    {
        var snapshot = GraphSnapshot.Create("Test", FixedNow, "[{},{},{}]", "[{}]", 3, 1, "system");
        var repo = Substitute.For<IGraphSnapshotRepository>();
        repo.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(snapshot);

        var handler = new GetLatestTopologySnapshotFeature.Handler(repo);
        var result = await handler.Handle(new GetLatestTopologySnapshotFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NodeCount.Should().Be(3);
        result.Value.EdgeCount.Should().Be(1);
        result.Value.CapturedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetLatestTopologySnapshot_Parses_Empty_Arrays_As_Zero()
    {
        var snapshot = GraphSnapshot.Create("Empty", FixedNow, "[]", "[]", 0, 0, "system");
        var repo = Substitute.For<IGraphSnapshotRepository>();
        repo.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(snapshot);

        var handler = new GetLatestTopologySnapshotFeature.Handler(repo);
        var result = await handler.Handle(new GetLatestTopologySnapshotFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NodeCount.Should().Be(0);
        result.Value.EdgeCount.Should().Be(0);
    }

    // ── Enum value tests ──────────────────────────────────────────────────

    [Fact]
    public void WhatIfChangeType_Enum_Has_Expected_Values()
    {
        ((int)WhatIfChangeType.Additive).Should().Be(0);
        ((int)WhatIfChangeType.NonBreaking).Should().Be(1);
        ((int)WhatIfChangeType.Breaking).Should().Be(2);
        ((int)WhatIfChangeType.Deprecation).Should().Be(3);
    }

    [Fact]
    public void WhatIfImpactLevel_Enum_Has_Expected_Values()
    {
        ((int)WhatIfImpactLevel.None).Should().Be(0);
        ((int)WhatIfImpactLevel.Low).Should().Be(1);
        ((int)WhatIfImpactLevel.Medium).Should().Be(2);
        ((int)WhatIfImpactLevel.High).Should().Be(3);
        ((int)WhatIfImpactLevel.Critical).Should().Be(4);
    }
}
