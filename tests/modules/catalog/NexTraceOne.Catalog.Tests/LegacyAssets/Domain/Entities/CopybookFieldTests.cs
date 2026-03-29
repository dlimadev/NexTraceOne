using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade CopybookField do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes, redefines e occurs.
/// </summary>
public sealed class CopybookFieldTests
{
    private static CopybookId CreateCopybookId() => CopybookId.New();

    private static CopybookField CreateField() =>
        CopybookField.Create(CreateCopybookId(), "CUSTOMER-NAME", 5, "PIC X(30)", 0, 30, 1);

    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var copybookId = CreateCopybookId();

        var field = CopybookField.Create(copybookId, "CUSTOMER-NAME", 5, "PIC X(30)", 0, 30, 1);

        field.CopybookId.Should().Be(copybookId);
        field.FieldName.Should().Be("CUSTOMER-NAME");
        field.Level.Should().Be(5);
        field.PicClause.Should().Be("PIC X(30)");
        field.Offset.Should().Be(0);
        field.Length.Should().Be(30);
        field.SortOrder.Should().Be(1);
        field.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithNullFieldName_ShouldThrow()
    {
        var act = () => CopybookField.Create(CreateCopybookId(), null!, 5, "PIC X(30)", 0, 30, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullCopybookId_ShouldThrow()
    {
        var act = () => CopybookField.Create(null!, "CUSTOMER-NAME", 5, "PIC X(30)", 0, 30, 1);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetRedefines_ShouldSetIsRedefinesToTrue()
    {
        var field = CreateField();

        field.SetRedefines("CUSTOMER-ADDR");

        field.IsRedefines.Should().BeTrue();
        field.RedefinesField.Should().Be("CUSTOMER-ADDR");
    }

    [Fact]
    public void SetOccurs_WithPositiveCount_ShouldSucceed()
    {
        var field = CreateField();

        field.SetOccurs(10);

        field.OccursCount.Should().Be(10);
    }

    [Fact]
    public void SetOccurs_WithZero_ShouldThrow()
    {
        var field = CreateField();

        var act = () => field.SetOccurs(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
