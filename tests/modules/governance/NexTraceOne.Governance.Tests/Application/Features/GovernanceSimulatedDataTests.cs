using FluentAssertions;
using NexTraceOne.Governance.Application.Features.GetExecutiveDrillDown;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de honestidade funcional para handlers do módulo Governance
/// que retornam dados simulados. Garante marcação IsSimulated/DataSource.
/// </summary>
public sealed class GovernanceSimulatedDataTests
{
    [Theory]
    [InlineData("domain", "commerce")]
    [InlineData("team", "order-squad")]
    [InlineData("service", "svc-order-api")]
    public async Task GetExecutiveDrillDown_ShouldDeclareIsSimulated(string entityType, string entityId)
    {
        var handler = new GetExecutiveDrillDown.Handler();
        var query = new GetExecutiveDrillDown.Query(entityType, entityId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSimulated.Should().BeTrue("handler returns demo data, must declare IsSimulated=true");
        result.Value.DataSource.Should().Be("demo");
    }

    [Fact]
    public async Task GetExecutiveDrillDown_ShouldPopulateAllSections()
    {
        var handler = new GetExecutiveDrillDown.Handler();
        var result = await handler.Handle(new GetExecutiveDrillDown.Query("domain", "commerce"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.KeyIndicators.Should().NotBeEmpty();
        result.Value.CriticalServices.Should().NotBeEmpty();
        result.Value.TopGaps.Should().NotBeEmpty();
        result.Value.RecommendedFocus.Should().NotBeEmpty();
        result.Value.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
