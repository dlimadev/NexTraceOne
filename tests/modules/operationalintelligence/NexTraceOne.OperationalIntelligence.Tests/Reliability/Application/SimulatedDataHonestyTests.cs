using FluentAssertions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetDomainReliabilitySummary;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityCoverage;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilityTrend;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Application;

/// <summary>
/// Testes que validam que todos os handlers com dados simulados
/// declaram explicitamente IsSimulated=true e DataSource="demo".
/// Garante honestidade funcional — nenhum dado demo pode parecer dado real.
/// </summary>
public sealed class SimulatedDataHonestyTests
{
    [Fact]
    public async Task GetTeamReliabilityTrend_ShouldDeclareIsSimulated()
    {
        var handler = new GetTeamReliabilityTrend.Handler();
        var result = await handler.Handle(new GetTeamReliabilityTrend.Query("any-team"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSimulated.Should().BeTrue("handler returns demo data, must declare IsSimulated=true");
        result.Value.DataSource.Should().Be("demo");
    }

    [Fact]
    public async Task GetServiceReliabilityTrend_ShouldDeclareIsSimulated()
    {
        var handler = new GetServiceReliabilityTrend.Handler();
        var result = await handler.Handle(new GetServiceReliabilityTrend.Query("svc-order-api"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSimulated.Should().BeTrue("handler returns demo data, must declare IsSimulated=true");
        result.Value.DataSource.Should().Be("demo");
    }

    [Fact]
    public async Task GetServiceReliabilityCoverage_ShouldDeclareIsSimulated()
    {
        var handler = new GetServiceReliabilityCoverage.Handler();
        var result = await handler.Handle(new GetServiceReliabilityCoverage.Query("svc-order-api"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSimulated.Should().BeTrue("handler returns demo data, must declare IsSimulated=true");
        result.Value.DataSource.Should().Be("demo");
    }

    [Fact]
    public async Task GetDomainReliabilitySummary_ShouldDeclareIsSimulated()
    {
        var handler = new GetDomainReliabilitySummary.Handler();
        var result = await handler.Handle(new GetDomainReliabilitySummary.Query("orders"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSimulated.Should().BeTrue("handler returns demo data, must declare IsSimulated=true");
        result.Value.DataSource.Should().Be("demo");
    }
}
