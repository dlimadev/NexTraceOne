using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Tests.Domain.Entities;

/// <summary>
/// Testes de unidade para a entidade AuditEvent.
/// Valida factory method, guard clauses, imutabilidade e linkagem com chain.
/// </summary>
public sealed class AuditEventTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Record_ValidInput_ShouldCreateEvent()
    {
        var evt = AuditEvent.Record("IdentityAccess", "UserCreated", "user-1", "User", "admin@tenant.com", _now, _tenantId);

        evt.Should().NotBeNull();
        evt.Id.Value.Should().NotBeEmpty();
        evt.SourceModule.Should().Be("IdentityAccess");
        evt.ActionType.Should().Be("UserCreated");
        evt.ResourceId.Should().Be("user-1");
        evt.ResourceType.Should().Be("User");
        evt.PerformedBy.Should().Be("admin@tenant.com");
        evt.OccurredAt.Should().Be(_now);
        evt.TenantId.Should().Be(_tenantId);
        evt.Payload.Should().BeNull();
        evt.ChainLink.Should().BeNull();
    }

    [Fact]
    public void Record_WithPayload_ShouldStorePayload()
    {
        var payload = """{"oldValue":"A","newValue":"B"}""";
        var evt = AuditEvent.Record("Catalog", "ServiceUpdated", "svc-1", "Service", "user@org.com", _now, _tenantId, payload);

        evt.Payload.Should().Be(payload);
    }

    [Fact]
    public void Record_WithCorrelationId_ShouldStoreCorrelation()
    {
        var evt = AuditEvent.Record("Catalog", "ServiceUpdated", "svc-1", "Service", "user@org.com", _now, _tenantId, null, "corr-xyz");

        evt.CorrelationId.Should().Be("corr-xyz");
    }

    [Fact]
    public void Record_GeneratesUniqueIds()
    {
        var evt1 = AuditEvent.Record("M", "A", "r1", "T", "u", _now, _tenantId);
        var evt2 = AuditEvent.Record("M", "A", "r2", "T", "u", _now, _tenantId);

        evt1.Id.Should().NotBe(evt2.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Record_EmptySourceModule_ShouldThrow(string? sourceModule)
    {
        var act = () => AuditEvent.Record(sourceModule!, "Action", "r1", "Type", "user", _now, _tenantId);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Record_EmptyActionType_ShouldThrow(string? actionType)
    {
        var act = () => AuditEvent.Record("Module", actionType!, "r1", "Type", "user", _now, _tenantId);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Record_EmptyResourceId_ShouldThrow(string? resourceId)
    {
        var act = () => AuditEvent.Record("Module", "Action", resourceId!, "Type", "user", _now, _tenantId);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Record_EmptyResourceType_ShouldThrow(string? resourceType)
    {
        var act = () => AuditEvent.Record("Module", "Action", "r1", resourceType!, "user", _now, _tenantId);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Record_EmptyPerformedBy_ShouldThrow(string? performedBy)
    {
        var act = () => AuditEvent.Record("Module", "Action", "r1", "Type", performedBy!, _now, _tenantId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LinkToChain_ValidLink_ShouldSetChainLink()
    {
        var evt = AuditEvent.Record("Module", "Action", "r1", "Type", "user", _now, _tenantId);
        var link = AuditChainLink.Create(evt, 1, string.Empty, _now);

        evt.LinkToChain(link);

        evt.ChainLink.Should().NotBeNull();
        evt.ChainLink.Should().Be(link);
    }

    [Fact]
    public void LinkToChain_NullLink_ShouldThrow()
    {
        var evt = AuditEvent.Record("Module", "Action", "r1", "Type", "user", _now, _tenantId);

        var act = () => evt.LinkToChain(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
