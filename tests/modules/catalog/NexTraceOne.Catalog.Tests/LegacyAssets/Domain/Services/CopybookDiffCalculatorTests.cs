using FluentAssertions;

using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Services;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Services;

/// <summary>
/// Testes para o CopybookDiffCalculator — valida detecção de mudanças breaking e non-breaking
/// entre versões de copybook COBOL.
/// </summary>
public sealed class CopybookDiffCalculatorTests
{
    private const string BaseCopybook = """
           01  CUSTOMER-RECORD.
               05  CUST-ID            PIC 9(10).
               05  CUST-NAME          PIC X(30).
               05  CUST-BALANCE       PIC S9(9)V99 COMP-3.
               05  CUST-STATUS        PIC X(1).
                   88  ACTIVE         VALUE 'A'.
                   88  INACTIVE       VALUE 'I'.
        """;

    [Fact]
    public void ComputeDiff_IdenticalCopybooks_ShouldReturnNonBreaking()
    {
        var baseLayout = CopybookParser.Parse(BaseCopybook);
        var targetLayout = CopybookParser.Parse(BaseCopybook);

        var result = CopybookDiffCalculator.ComputeDiff(baseLayout, targetLayout);

        result.BreakingChanges.Should().BeEmpty();
        result.AdditiveChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
    }

    [Fact]
    public void ComputeDiff_FieldAddedAtEnd_ShouldBeAdditive()
    {
        const string targetCopybook = """
               01  CUSTOMER-RECORD.
                   05  CUST-ID            PIC 9(10).
                   05  CUST-NAME          PIC X(30).
                   05  CUST-BALANCE       PIC S9(9)V99 COMP-3.
                   05  CUST-STATUS        PIC X(1).
                       88  ACTIVE         VALUE 'A'.
                       88  INACTIVE       VALUE 'I'.
                   05  CUST-EMAIL         PIC X(50).
            """;

        var baseLayout = CopybookParser.Parse(BaseCopybook);
        var targetLayout = CopybookParser.Parse(targetCopybook);

        var result = CopybookDiffCalculator.ComputeDiff(baseLayout, targetLayout);

        result.AdditiveChanges.Should().ContainSingle(c => c.ChangeType == "FieldAdded");
        result.BreakingChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
    }

    [Fact]
    public void ComputeDiff_FieldRemoved_ShouldBeBreaking()
    {
        const string targetCopybook = """
               01  CUSTOMER-RECORD.
                   05  CUST-ID            PIC 9(10).
                   05  CUST-NAME          PIC X(30).
                   05  CUST-STATUS        PIC X(1).
            """;

        var baseLayout = CopybookParser.Parse(BaseCopybook);
        var targetLayout = CopybookParser.Parse(targetCopybook);

        var result = CopybookDiffCalculator.ComputeDiff(baseLayout, targetLayout);

        result.BreakingChanges.Should().Contain(c => c.ChangeType == "FieldRemoved");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_FieldTypeChanged_ShouldBeBreaking()
    {
        const string targetCopybook = """
               01  CUSTOMER-RECORD.
                   05  CUST-ID            PIC X(10).
                   05  CUST-NAME          PIC X(30).
                   05  CUST-BALANCE       PIC S9(9)V99 COMP-3.
                   05  CUST-STATUS        PIC X(1).
                       88  ACTIVE         VALUE 'A'.
                       88  INACTIVE       VALUE 'I'.
            """;

        var baseLayout = CopybookParser.Parse(BaseCopybook);
        var targetLayout = CopybookParser.Parse(targetCopybook);

        var result = CopybookDiffCalculator.ComputeDiff(baseLayout, targetLayout);

        result.BreakingChanges.Should().Contain(c =>
            c.ChangeType == "FieldTypeChanged" && c.Path == "CUST-ID");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_FieldLengthChanged_ShouldBeBreaking()
    {
        const string targetCopybook = """
               01  CUSTOMER-RECORD.
                   05  CUST-ID            PIC 9(10).
                   05  CUST-NAME          PIC X(50).
                   05  CUST-BALANCE       PIC S9(9)V99 COMP-3.
                   05  CUST-STATUS        PIC X(1).
                       88  ACTIVE         VALUE 'A'.
                       88  INACTIVE       VALUE 'I'.
            """;

        var baseLayout = CopybookParser.Parse(BaseCopybook);
        var targetLayout = CopybookParser.Parse(targetCopybook);

        var result = CopybookDiffCalculator.ComputeDiff(baseLayout, targetLayout);

        result.BreakingChanges.Should().Contain(c =>
            c.ChangeType == "FieldLengthChanged" && c.Path == "CUST-NAME");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_ConditionValueChanged_ShouldBeNonBreaking()
    {
        const string targetCopybook = """
               01  CUSTOMER-RECORD.
                   05  CUST-ID            PIC 9(10).
                   05  CUST-NAME          PIC X(30).
                   05  CUST-BALANCE       PIC S9(9)V99 COMP-3.
                   05  CUST-STATUS        PIC X(1).
                       88  ACTIVE         VALUE 'Y'.
                       88  INACTIVE       VALUE 'N'.
            """;

        var baseLayout = CopybookParser.Parse(BaseCopybook);
        var targetLayout = CopybookParser.Parse(targetCopybook);

        var result = CopybookDiffCalculator.ComputeDiff(baseLayout, targetLayout);

        result.NonBreakingChanges.Should().Contain(c => c.ChangeType == "ConditionValueChanged");
        result.BreakingChanges.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_OccursCountChanged_ShouldBeBreaking()
    {
        const string baseCopy = """
               01  RECORD-A.
                   05  ITEMS           OCCURS 3 TIMES.
                       10  ITEM-NAME  PIC X(10).
            """;

        const string targetCopy = """
               01  RECORD-A.
                   05  ITEMS           OCCURS 5 TIMES.
                       10  ITEM-NAME  PIC X(10).
            """;

        var baseLayout = CopybookParser.Parse(baseCopy);
        var targetLayout = CopybookParser.Parse(targetCopy);

        var result = CopybookDiffCalculator.ComputeDiff(baseLayout, targetLayout);

        result.BreakingChanges.Should().Contain(c => c.ChangeType == "OccursCountChanged");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_WithNullBaseLayout_ShouldThrow()
    {
        var layout = CopybookParser.Parse(BaseCopybook);

        var act = () => CopybookDiffCalculator.ComputeDiff(null!, layout);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeDiff_WithNullTargetLayout_ShouldThrow()
    {
        var layout = CopybookParser.Parse(BaseCopybook);

        var act = () => CopybookDiffCalculator.ComputeDiff(layout, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeDiff_ConditionAdded_ShouldBeNonBreaking()
    {
        const string targetCopybook = """
               01  CUSTOMER-RECORD.
                   05  CUST-ID            PIC 9(10).
                   05  CUST-NAME          PIC X(30).
                   05  CUST-BALANCE       PIC S9(9)V99 COMP-3.
                   05  CUST-STATUS        PIC X(1).
                       88  ACTIVE         VALUE 'A'.
                       88  INACTIVE       VALUE 'I'.
                       88  SUSPENDED      VALUE 'S'.
            """;

        var baseLayout = CopybookParser.Parse(BaseCopybook);
        var targetLayout = CopybookParser.Parse(targetCopybook);

        var result = CopybookDiffCalculator.ComputeDiff(baseLayout, targetLayout);

        result.NonBreakingChanges.Should().Contain(c =>
            c.ChangeType == "ConditionAdded" && c.Path == "SUSPENDED");
    }
}
