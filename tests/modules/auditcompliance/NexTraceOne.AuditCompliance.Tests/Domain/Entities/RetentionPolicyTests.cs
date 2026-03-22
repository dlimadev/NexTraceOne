using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Tests.Domain.Entities;

/// <summary>
/// Testes de unidade para a entidade RetentionPolicy.
/// Valida criação, atualização de dias de retenção e desativação.
/// </summary>
public sealed class RetentionPolicyTests
{
    [Fact]
    public void Create_ValidInput_ShouldCreatePolicy()
    {
        var policy = RetentionPolicy.Create("default-90", 90);

        policy.Should().NotBeNull();
        policy.Id.Value.Should().NotBeEmpty();
        policy.Name.Should().Be("default-90");
        policy.RetentionDays.Should().Be(90);
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_MaxRetention_ShouldSucceed()
    {
        var policy = RetentionPolicy.Create("long-term", 3650);
        policy.RetentionDays.Should().Be(3650);
    }

    [Fact]
    public void Create_MinRetention_ShouldSucceed()
    {
        var policy = RetentionPolicy.Create("short-term", 1);
        policy.RetentionDays.Should().Be(1);
    }

    [Fact]
    public void Create_ZeroRetentionDays_ShouldThrow()
    {
        var act = () => RetentionPolicy.Create("invalid", 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeRetentionDays_ShouldThrow()
    {
        var act = () => RetentionPolicy.Create("invalid", -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyName_ShouldThrow(string? name)
    {
        var act = () => RetentionPolicy.Create(name!, 30);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateRetention_ValidDays_ShouldUpdate()
    {
        var policy = RetentionPolicy.Create("test", 30);
        policy.UpdateRetention(60);

        policy.RetentionDays.Should().Be(60);
    }

    [Fact]
    public void UpdateRetention_ZeroDays_ShouldThrow()
    {
        var policy = RetentionPolicy.Create("test", 30);
        var act = () => policy.UpdateRetention(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateRetention_NegativeDays_ShouldThrow()
    {
        var policy = RetentionPolicy.Create("test", 30);
        var act = () => policy.UpdateRetention(-5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var policy = RetentionPolicy.Create("test", 30);
        policy.IsActive.Should().BeTrue();

        policy.Deactivate();

        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ShouldRemainInactive()
    {
        var policy = RetentionPolicy.Create("test", 30);
        policy.Deactivate();
        policy.Deactivate();

        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var p1 = RetentionPolicy.Create("p1", 30);
        var p2 = RetentionPolicy.Create("p2", 60);

        p1.Id.Should().NotBe(p2.Id);
    }
}
