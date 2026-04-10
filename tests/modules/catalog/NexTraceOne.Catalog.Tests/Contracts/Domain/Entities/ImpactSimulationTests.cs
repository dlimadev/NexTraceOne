using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="ImpactSimulation"/>.
/// Valida criação via factory method Simulate, guarda de parâmetros, limites de negócio,
/// trimming de strings, cenários de enum e tipagem forte de Id.
/// </summary>
public sealed class ImpactSimulationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 07, 01, 12, 0, 0, TimeSpan.Zero);

    // ── Factory method: Simulate — valid scenarios ──

    [Fact]
    public void Simulate_ValidInputs_ShouldSetAllFields()
    {
        var result = CreateValid();

        result.Id.Value.Should().NotBeEmpty();
        result.ServiceName.Should().Be("OrderService");
        result.Scenario.Should().Be(ImpactSimulationScenario.EndpointRemoval);
        result.ScenarioDescription.Should().Be("Remove GET /orders/{id} endpoint.");
        result.AffectedServices.Should().Be("[\"BillingService\",\"ShippingService\"]");
        result.BrokenConsumers.Should().Be("[\"MobileApp\",\"ReportingService\"]");
        result.TransitiveCascadeDepth.Should().Be(3);
        result.RiskPercent.Should().Be(75);
        result.MitigationRecommendations.Should().Be("[\"Notify consumers 2 weeks before removal\"]");
        result.SimulatedAt.Should().Be(FixedNow);
        result.TenantId.Should().Be("tenant-abc");
    }

    [Fact]
    public void Simulate_NullOptionalFields_ShouldBeValid()
    {
        var result = ImpactSimulation.Simulate(
            "PaymentService",
            ImpactSimulationScenario.ServiceUnavailability,
            "Full service outage simulation.",
            null,
            null,
            0,
            50,
            null,
            FixedNow);

        result.AffectedServices.Should().BeNull();
        result.BrokenConsumers.Should().BeNull();
        result.MitigationRecommendations.Should().BeNull();
        result.TenantId.Should().BeNull();
    }

    [Fact]
    public void Simulate_AllScenarios_ShouldBeAccepted()
    {
        foreach (var scenario in Enum.GetValues<ImpactSimulationScenario>())
        {
            var result = ImpactSimulation.Simulate(
                "TestService",
                scenario,
                "Test scenario description.",
                null,
                null,
                0,
                50,
                null,
                FixedNow);

            result.Scenario.Should().Be(scenario);
        }
    }

    [Fact]
    public void Simulate_BoundaryRisk0_ShouldBeValid()
    {
        var result = ImpactSimulation.Simulate(
            "LowRiskService",
            ImpactSimulationScenario.SchemaChange,
            "Minor field addition.",
            null,
            null,
            0,
            0,
            null,
            FixedNow);

        result.RiskPercent.Should().Be(0);
    }

    [Fact]
    public void Simulate_BoundaryRisk100_ShouldBeValid()
    {
        var result = ImpactSimulation.Simulate(
            "CriticalService",
            ImpactSimulationScenario.ServiceUnavailability,
            "Total outage of critical payment gateway.",
            null,
            null,
            5,
            100,
            null,
            FixedNow);

        result.RiskPercent.Should().Be(100);
    }

    [Fact]
    public void Simulate_CascadeDepth0_ShouldBeValid()
    {
        var result = ImpactSimulation.Simulate(
            "LeafService",
            ImpactSimulationScenario.EndpointRemoval,
            "Remove endpoint with no downstream dependents.",
            null,
            null,
            0,
            10,
            null,
            FixedNow);

        result.TransitiveCascadeDepth.Should().Be(0);
    }

    [Fact]
    public void Simulate_TrimsStrings()
    {
        var result = ImpactSimulation.Simulate(
            "  OrderService  ",
            ImpactSimulationScenario.ContractMigration,
            "  Migrate to v2 contract  ",
            null,
            null,
            1,
            40,
            null,
            FixedNow,
            "  tenant-1  ");

        result.ServiceName.Should().Be("OrderService");
        result.ScenarioDescription.Should().Be("Migrate to v2 contract");
        result.TenantId.Should().Be("tenant-1");
    }

    // ── Guard clauses ──

    [Fact]
    public void Simulate_EmptyServiceName_ShouldThrow()
    {
        var act = () => ImpactSimulation.Simulate(
            "",
            ImpactSimulationScenario.EndpointRemoval,
            "Description.",
            null, null, 0, 50, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Simulate_WhitespaceServiceName_ShouldThrow()
    {
        var act = () => ImpactSimulation.Simulate(
            "   ",
            ImpactSimulationScenario.EndpointRemoval,
            "Description.",
            null, null, 0, 50, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Simulate_EmptyScenarioDescription_ShouldThrow()
    {
        var act = () => ImpactSimulation.Simulate(
            "OrderService",
            ImpactSimulationScenario.EndpointRemoval,
            "",
            null, null, 0, 50, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Simulate_WhitespaceScenarioDescription_ShouldThrow()
    {
        var act = () => ImpactSimulation.Simulate(
            "OrderService",
            ImpactSimulationScenario.EndpointRemoval,
            "   ",
            null, null, 0, 50, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Simulate_ServiceNameTooLong_ShouldThrow()
    {
        var act = () => ImpactSimulation.Simulate(
            new string('x', 201),
            ImpactSimulationScenario.EndpointRemoval,
            "Description.",
            null, null, 0, 50, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Simulate_ScenarioDescriptionTooLong_ShouldThrow()
    {
        var act = () => ImpactSimulation.Simulate(
            "OrderService",
            ImpactSimulationScenario.EndpointRemoval,
            new string('x', 4001),
            null, null, 0, 50, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Simulate_NegativeRiskPercent_ShouldThrow()
    {
        var act = () => ImpactSimulation.Simulate(
            "OrderService",
            ImpactSimulationScenario.EndpointRemoval,
            "Description.",
            null, null, 0, -1, null, FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Simulate_RiskPercentAbove100_ShouldThrow()
    {
        var act = () => ImpactSimulation.Simulate(
            "OrderService",
            ImpactSimulationScenario.EndpointRemoval,
            "Description.",
            null, null, 0, 101, null, FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Simulate_NegativeCascadeDepth_ShouldThrow()
    {
        var act = () => ImpactSimulation.Simulate(
            "OrderService",
            ImpactSimulationScenario.EndpointRemoval,
            "Description.",
            null, null, -1, 50, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Strongly typed Id ──

    [Fact]
    public void ImpactSimulationId_New_ShouldGenerateUniqueIds()
    {
        var id1 = ImpactSimulationId.New();
        var id2 = ImpactSimulationId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void ImpactSimulationId_From_ShouldPreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = ImpactSimulationId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──

    private static ImpactSimulation CreateValid() => ImpactSimulation.Simulate(
        serviceName: "OrderService",
        scenario: ImpactSimulationScenario.EndpointRemoval,
        scenarioDescription: "Remove GET /orders/{id} endpoint.",
        affectedServices: "[\"BillingService\",\"ShippingService\"]",
        brokenConsumers: "[\"MobileApp\",\"ReportingService\"]",
        transitiveCascadeDepth: 3,
        riskPercent: 75,
        mitigationRecommendations: "[\"Notify consumers 2 weeks before removal\"]",
        simulatedAt: FixedNow,
        tenantId: "tenant-abc");
}
