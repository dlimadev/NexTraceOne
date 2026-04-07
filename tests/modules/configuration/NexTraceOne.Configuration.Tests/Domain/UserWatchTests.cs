using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade UserWatch.
/// Valida criação, invariantes e actualização do nível de notificação.
/// </summary>
public sealed class UserWatchTests
{
    // ── Create — happy path ────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldReturnWatch()
    {
        var now = DateTimeOffset.UtcNow;
        var watch = UserWatch.Create("user1", "tenant1", "service", "svc-123", "all", now);

        watch.Should().NotBeNull();
        watch.UserId.Should().Be("user1");
        watch.TenantId.Should().Be("tenant1");
        watch.EntityType.Should().Be("service");
        watch.EntityId.Should().Be("svc-123");
        watch.NotifyLevel.Should().Be("all");
        watch.CreatedAt.Should().Be(now);
        watch.Id.Should().NotBeNull();
        watch.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("service")]
    [InlineData("contract")]
    [InlineData("change")]
    [InlineData("incident")]
    [InlineData("runbook")]
    public void Create_WithAllValidEntityTypes_ShouldSucceed(string entityType)
    {
        var watch = UserWatch.Create("user1", "tenant1", entityType, "id-1", "all", DateTimeOffset.UtcNow);
        watch.EntityType.Should().Be(entityType.ToLower());
    }

    // ── Create — guard clauses ─────────────────────────────────────────

    [Fact]
    public void Create_WithInvalidEntityType_ShouldThrowArgumentException()
    {
        var now = DateTimeOffset.UtcNow;
        var act = () => UserWatch.Create("user1", "tenant1", "invalid-type", "id1", "all", now);
        act.Should().Throw<ArgumentException>().WithMessage("*EntityType*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyUserId_ShouldThrow(string userId)
    {
        var act = () => UserWatch.Create(userId, "tenant1", "service", "id1", "all", DateTimeOffset.UtcNow);
        act.Should().Throw<Exception>();
    }

    // ── NotifyLevel normalisation ──────────────────────────────────────

    [Theory]
    [InlineData("all")]
    [InlineData("critical")]
    [InlineData("none")]
    public void Create_WithValidNotifyLevel_ShouldPreserveLevel(string level)
    {
        var watch = UserWatch.Create("user1", "tenant1", "service", "id1", level, DateTimeOffset.UtcNow);
        watch.NotifyLevel.Should().Be(level);
    }

    [Fact]
    public void Create_WithInvalidNotifyLevel_ShouldDefaultToAll()
    {
        var watch = UserWatch.Create("user1", "tenant1", "incident", "i-1", "invalid", DateTimeOffset.UtcNow);
        watch.NotifyLevel.Should().Be("all");
    }

    // ── UpdateNotifyLevel ──────────────────────────────────────────────

    [Fact]
    public void UpdateNotifyLevel_ShouldChangeLevel()
    {
        var now = DateTimeOffset.UtcNow;
        var watch = UserWatch.Create("user1", "tenant1", "contract", "c-1", "all", now);

        watch.UpdateNotifyLevel("critical", now.AddMinutes(1));

        watch.NotifyLevel.Should().Be("critical");
        watch.UpdatedAt.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public void UpdateNotifyLevel_WithInvalidLevel_ShouldDefaultToAll()
    {
        var now = DateTimeOffset.UtcNow;
        var watch = UserWatch.Create("user1", "tenant1", "incident", "i-1", "critical", now);

        watch.UpdateNotifyLevel("invalid", now.AddMinutes(1));

        watch.NotifyLevel.Should().Be("all");
    }

    // ── EntityType casing ──────────────────────────────────────────────

    [Fact]
    public void Create_WithUpperCaseEntityType_ShouldNormaliseToLower()
    {
        var watch = UserWatch.Create("user1", "tenant1", "SERVICE", "id1", "all", DateTimeOffset.UtcNow);
        watch.EntityType.Should().Be("service");
    }
}
