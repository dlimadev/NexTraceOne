using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.ValueObjects;

/// <summary>
/// Testes do value object CopybookLayout do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes e valores default.
/// </summary>
public sealed class CopybookLayoutTests
{
    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var layout = CopybookLayout.Create(10, 80, "FB");

        layout.FieldCount.Should().Be(10);
        layout.TotalLength.Should().Be(80);
        layout.RecordFormat.Should().Be("FB");
    }

    [Fact]
    public void Create_WithNegativeFieldCount_ShouldThrow()
    {
        var act = () => CopybookLayout.Create(-1, 80, "FB");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeTotalLength_ShouldThrow()
    {
        var act = () => CopybookLayout.Create(10, -1, "FB");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNullRecordFormat_ShouldDefaultToEmpty()
    {
        var layout = CopybookLayout.Create(10, 80);

        layout.RecordFormat.Should().BeEmpty();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var layout1 = CopybookLayout.Create(10, 80, "FB");
        var layout2 = CopybookLayout.Create(10, 80, "FB");

        layout1.Should().Be(layout2);
    }
}
