using NexTraceOne.Notifications.Infrastructure.Intelligence;

namespace NexTraceOne.Notifications.Tests.Intelligence;

/// <summary>
/// Testes para o NotificationGroupingService da Fase 6.
/// Valida geração de chaves de correlação e agrupamento.
/// </summary>
public sealed class NotificationGroupingServiceTests
{
    [Fact]
    public void GenerateCorrelationKey_ShouldIncludeAllParts()
    {
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var key = service.GenerateCorrelationKey(
            tenantId,
            "IncidentCreated",
            "OperationalIntelligence",
            "Incident",
            "123");

        key.Should().Contain(tenantId.ToString("N"));
        key.Should().Contain("OperationalIntelligence");
        key.Should().Contain("IncidentCreated");
        key.Should().Contain("Incident");
        key.Should().Contain("123");
    }

    [Fact]
    public void GenerateCorrelationKey_SameInput_ShouldBeDeterministic()
    {
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var key1 = service.GenerateCorrelationKey(
            tenantId, "Event", "Module", "Type", "Id");
        var key2 = service.GenerateCorrelationKey(
            tenantId, "Event", "Module", "Type", "Id");

        key1.Should().Be(key2);
    }

    [Fact]
    public void GenerateCorrelationKey_DifferentTenants_ShouldDiffer()
    {
        var service = CreateService();

        var key1 = service.GenerateCorrelationKey(
            Guid.NewGuid(), "Event", "Module", "Type", "Id");
        var key2 = service.GenerateCorrelationKey(
            Guid.NewGuid(), "Event", "Module", "Type", "Id");

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void GenerateCorrelationKey_NullEntityType_ShouldExcludeFromKey()
    {
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var key1 = service.GenerateCorrelationKey(
            tenantId, "Event", "Module", null, null);
        var key2 = service.GenerateCorrelationKey(
            tenantId, "Event", "Module", "Type", "Id");

        key1.Should().NotBe(key2);
        key1.Split('|').Should().HaveCount(3); // tenant|module|event
        key2.Split('|').Should().HaveCount(5); // tenant|module|event|type|id
    }

    [Fact]
    public void GenerateCorrelationKey_DifferentEvents_ShouldDiffer()
    {
        var service = CreateService();
        var tenantId = Guid.NewGuid();

        var key1 = service.GenerateCorrelationKey(
            tenantId, "IncidentCreated", "Module", "Incident", "123");
        var key2 = service.GenerateCorrelationKey(
            tenantId, "IncidentEscalated", "Module", "Incident", "123");

        key1.Should().NotBe(key2);
    }

    /// <summary>
    /// Creates a grouping service instance.
    /// Note: ResolveGroupAsync requires a DbContext so it's not unit-testable here.
    /// The correlation key generation is the core unit-testable logic.
    /// </summary>
    private static NotificationGroupingService CreateService()
    {
        // NotificationGroupingService requires DbContext for ResolveGroupAsync.
        // For unit tests, we test GenerateCorrelationKey which is pure.
        // ResolveGroupAsync would need integration tests.
        return new NotificationGroupingService(null!);
    }
}
