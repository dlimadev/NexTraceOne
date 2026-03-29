using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.ValueObjects;

/// <summary>
/// Testes do value object LparReference do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes e igualdade estrutural.
/// </summary>
public sealed class LparReferenceTests
{
    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var lpar = LparReference.Create("SYSPLEX1", "LPAR01", "CICSPRD1");

        lpar.SysplexName.Should().Be("SYSPLEX1");
        lpar.LparName.Should().Be("LPAR01");
        lpar.RegionName.Should().Be("CICSPRD1");
    }

    [Fact]
    public void Create_WithNullSysplex_ShouldThrow()
    {
        var act = () => LparReference.Create(null!, "LPAR01");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullLpar_ShouldThrow()
    {
        var act = () => LparReference.Create("SYSPLEX1", null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullRegion_ShouldDefaultToEmpty()
    {
        var lpar = LparReference.Create("SYSPLEX1", "LPAR01");

        lpar.RegionName.Should().BeEmpty();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var lpar1 = LparReference.Create("SYSPLEX1", "LPAR01", "CICSPRD1");
        var lpar2 = LparReference.Create("SYSPLEX1", "LPAR01", "CICSPRD1");

        lpar1.Should().Be(lpar2);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        var lpar1 = LparReference.Create("SYSPLEX1", "LPAR01", "CICSPRD1");
        var lpar2 = LparReference.Create("SYSPLEX2", "LPAR02", "CICSPRD2");

        lpar1.Should().NotBe(lpar2);
    }
}
