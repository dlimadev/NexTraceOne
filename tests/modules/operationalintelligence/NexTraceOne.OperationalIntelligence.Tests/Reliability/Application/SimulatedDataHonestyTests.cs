using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetDomainReliabilitySummary;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityCoverage;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Application;

/// <summary>
/// Testes que validam que todos os handlers de Reliability utilizam dados reais
/// (via superfícies mockadas) e não retornam dados simulados hardcoded.
/// Phase 3: todos os handlers integram com IReliabilityRuntimeSurface e IReliabilityIncidentSurface.
/// </summary>
public sealed class SimulatedDataHonestyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ICurrentTenant MockTenant()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(TenantId);
        return tenant;
    }

    [Fact]
    public async Task GetTeamReliabilityTrend_ShouldUseRealIncidentData()
    {
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        incidentSurface.GetTeamIncidentsAsync("any-team", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());

        var handler = new GetTeamReliabilityTrend.Handler(incidentSurface, MockTenant());
        var result = await handler.Handle(new GetTeamReliabilityTrend.Query("any-team"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await incidentSurface.Received(1).GetTeamIncidentsAsync("any-team", TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetServiceReliabilityTrend_ShouldUseRealSnapshotData()
    {
        var snapshotRepo = Substitute.For<IReliabilitySnapshotRepository>();
        snapshotRepo.GetHistoryAsync("svc-order-api", TenantId, 30, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilitySnapshot>());

        var handler = new GetServiceReliabilityTrend.Handler(snapshotRepo, MockTenant());
        var result = await handler.Handle(new GetServiceReliabilityTrend.Query("svc-order-api"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await snapshotRepo.Received(1).GetHistoryAsync("svc-order-api", TenantId, 30, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetServiceReliabilityCoverage_ShouldUseRealSurfaces()
    {
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        var snapshotRepo = Substitute.For<IReliabilitySnapshotRepository>();

        runtimeSurface.GetLatestSignalAsync("svc-order-api", string.Empty, Arg.Any<CancellationToken>())
            .Returns((RuntimeServiceSignal?)null);
        incidentSurface.HasRunbookAsync("svc-order-api", Arg.Any<CancellationToken>()).Returns(false);
        incidentSurface.GetActiveIncidentsAsync("svc-order-api", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());

        var handler = new GetServiceReliabilityCoverage.Handler(runtimeSurface, incidentSurface, snapshotRepo, MockTenant());
        var result = await handler.Handle(new GetServiceReliabilityCoverage.Query("svc-order-api"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await runtimeSurface.Received(1).GetLatestSignalAsync("svc-order-api", string.Empty, Arg.Any<CancellationToken>());
        await incidentSurface.Received(1).HasRunbookAsync("svc-order-api", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDomainReliabilitySummary_ShouldUseRealIncidentData()
    {
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        incidentSurface.GetDomainIncidentsAsync("orders", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());

        var handler = new GetDomainReliabilitySummary.Handler(incidentSurface, MockTenant());
        var result = await handler.Handle(new GetDomainReliabilitySummary.Query("orders"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await incidentSurface.Received(1).GetDomainIncidentsAsync("orders", TenantId, Arg.Any<CancellationToken>());
    }
}
