using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.ValueObjects;

/// <summary>
/// Testes do value object CicsRegion do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes, valores opcionais e igualdade.
/// </summary>
public sealed class CicsRegionTests
{
    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var region = CicsRegion.Create("CICSPRD1", "5.6", 1490);

        region.RegionName.Should().Be("CICSPRD1");
        region.CicsVersion.Should().Be("5.6");
        region.Port.Should().Be(1490);
    }

    [Fact]
    public void Create_WithNullRegionName_ShouldThrow()
    {
        var act = () => CicsRegion.Create(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithOptionalValues_ShouldDefaultToEmpty()
    {
        var region = CicsRegion.Create("CICSPRD1");

        region.CicsVersion.Should().BeEmpty();
        region.Port.Should().BeNull();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var region1 = CicsRegion.Create("CICSPRD1", "5.6", 1490);
        var region2 = CicsRegion.Create("CICSPRD1", "5.6", 1490);

        region1.Should().Be(region2);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        var region1 = CicsRegion.Create("CICSPRD1", "5.6", 1490);
        var region2 = CicsRegion.Create("CICSPRD2", "6.0", 1491);

        region1.Should().NotBe(region2);
    }
}
