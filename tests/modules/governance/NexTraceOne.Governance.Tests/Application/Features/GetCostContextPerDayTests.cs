using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;

using GetCostContextPerDayFeature = NexTraceOne.Governance.Application.Features.GetCostContextPerDay.GetCostContextPerDay;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes unitários para GetCostContextPerDay.
/// Valida mapeamento de custo disponível, resposta nula quando sem dados e validação.
/// </summary>
public sealed class GetCostContextPerDayTests
{
    private readonly ICostIntelligenceModule _costModule = Substitute.For<ICostIntelligenceModule>();

    private GetCostContextPerDayFeature.Handler CreateHandler() =>
        new(_costModule);

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCostContextExists_ReturnsMappedResponse()
    {
        var context = new CostContextPerDay(
            ActualCostPerDay: 120.50m,
            BaselineCostPerDay: 100.00m,
            Currency: "USD",
            ServiceName: "order-api",
            Environment: "production");

        _costModule.GetCostContextPerDayAsync("order-api", "production", Arg.Any<CancellationToken>())
            .Returns(context);

        var result = await CreateHandler().Handle(
            new GetCostContextPerDayFeature.Query("order-api", "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ServiceName.Should().Be("order-api");
        result.Value.Environment.Should().Be("production");
        result.Value.ActualCostPerDay.Should().Be(120.50m);
        result.Value.BaselineCostPerDay.Should().Be(100.00m);
        result.Value.Currency.Should().Be("USD");
    }

    // ── Null result (no cost data) ────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoCostData_ReturnsSuccessWithNull()
    {
        _costModule.GetCostContextPerDayAsync("unknown-svc", "staging", Arg.Any<CancellationToken>())
            .Returns((CostContextPerDay?)null);

        var result = await CreateHandler().Handle(
            new GetCostContextPerDayFeature.Query("unknown-svc", "staging"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // ── Validator ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "production")]
    [InlineData("order-api", "")]
    public void Validator_MissingRequiredField_ReturnsError(string serviceName, string environment)
    {
        var validator = new GetCostContextPerDayFeature.Validator();
        var result = validator.Validate(new GetCostContextPerDayFeature.Query(serviceName, environment));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ValidInput_Passes()
    {
        var validator = new GetCostContextPerDayFeature.Validator();
        var result = validator.Validate(new GetCostContextPerDayFeature.Query("checkout-api", "staging"));

        result.IsValid.Should().BeTrue();
    }
}
