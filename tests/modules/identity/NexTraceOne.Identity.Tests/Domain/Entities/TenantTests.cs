using FluentAssertions;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Tests.Domain.Entities;

/// <summary>
/// Testes do aggregate Tenant — validam criação, atualização, ativação e desativação.
/// </summary>
public sealed class TenantTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_ReturnActiveTenant_WithCorrectFields()
    {
        var tenant = Tenant.Create("Banco XYZ", "banco-xyz", Now);

        tenant.Id.Should().NotBe(default);
        tenant.Name.Should().Be("Banco XYZ");
        tenant.Slug.Should().Be("banco-xyz");
        tenant.IsActive.Should().BeTrue();
        tenant.CreatedAt.Should().Be(Now);
        tenant.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_Should_NormalizeSluToLowerCase()
    {
        var tenant = Tenant.Create("Corp", "MY-SLUG", Now);

        tenant.Slug.Should().Be("my-slug");
    }

    [Fact]
    public void Create_Should_ThrowWhenNameIsEmpty()
    {
        var act = () => Tenant.Create("", "slug", Now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_ThrowWhenSlugIsEmpty()
    {
        var act = () => Tenant.Create("Name", "", Now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_Should_SetIsActiveToFalse()
    {
        var tenant = Tenant.Create("Corp", "corp", Now);
        var later = Now.AddHours(1);

        tenant.Deactivate(later);

        tenant.IsActive.Should().BeFalse();
        tenant.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void Activate_Should_SetIsActiveToTrue()
    {
        var tenant = Tenant.Create("Corp", "corp", Now);
        tenant.Deactivate(Now.AddMinutes(30));

        tenant.Activate(Now.AddHours(1));

        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateName_Should_ChangeNameAndSetUpdatedAt()
    {
        var tenant = Tenant.Create("Old Name", "slug", Now);
        var later = Now.AddDays(1);

        tenant.UpdateName("New Name", later);

        tenant.Name.Should().Be("New Name");
        tenant.UpdatedAt.Should().Be(later);
    }
}
