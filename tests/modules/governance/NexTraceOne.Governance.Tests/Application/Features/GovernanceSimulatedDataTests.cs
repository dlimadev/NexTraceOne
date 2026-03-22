using FluentAssertions;
using NexTraceOne.Governance.Application.Features.GetExecutiveDrillDown;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using NSubstitute;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de honestidade funcional para handlers do módulo Governance
/// que consomem dados reais do CostIntelligence. Garante marcação IsSimulated/DataSource.
/// </summary>
public sealed class GovernanceSimulatedDataTests
{
    private static readonly CostRecordSummary[] SampleRecords =
    [
        new("svc-order-api", "Order API", "order-squad", "commerce", "Production", 18700m, "EUR", "2026-03", "azure"),
        new("svc-payment-api", "Payment API", "order-squad", "commerce", "Production", 12500m, "EUR", "2026-03", "azure")
    ];

    private static ICostIntelligenceModule CreateMock()
    {
        var mock = Substitute.For<ICostIntelligenceModule>();
        mock.GetCostRecordsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRecords);
        mock.GetCostsByDomainAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRecords);
        mock.GetCostsByTeamAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRecords);
        mock.GetServiceCostAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRecords[0]);
        return mock;
    }

    [Theory]
    [InlineData("domain", "commerce")]
    [InlineData("team", "order-squad")]
    [InlineData("service", "svc-order-api")]
    public async Task GetExecutiveDrillDown_ShouldDeclareIsNotSimulated(string entityType, string entityId)
    {
        var handler = new GetExecutiveDrillDown.Handler(CreateMock());
        var query = new GetExecutiveDrillDown.Query(entityType, entityId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSimulated.Should().BeFalse("handler now uses real cost data");
        result.Value.DataSource.Should().Be("cost-intelligence");
    }

    [Fact]
    public async Task GetExecutiveDrillDown_ShouldPopulateKeyIndicators()
    {
        var handler = new GetExecutiveDrillDown.Handler(CreateMock());
        var result = await handler.Handle(new GetExecutiveDrillDown.Query("domain", "commerce"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.KeyIndicators.Should().NotBeEmpty();
        result.Value.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
