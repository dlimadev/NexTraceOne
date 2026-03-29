using FluentAssertions;

using NexTraceOne.Catalog.Domain.LegacyAssets.Services;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Services;

/// <summary>
/// Testes para o CopybookParser — valida parsing de copybooks COBOL em campos estruturados.
/// </summary>
public sealed class CopybookParserTests
{
    private const string SimpleCustomerCopybook = """
           01  CUSTOMER-RECORD.
               05  CUST-ID            PIC 9(10).
               05  CUST-NAME.
                   10  FIRST-NAME    PIC X(30).
                   10  LAST-NAME     PIC X(30).
               05  CUST-BALANCE       PIC S9(9)V99 COMP-3.
               05  CUST-STATUS        PIC X(1).
                   88  ACTIVE         VALUE 'A'.
                   88  INACTIVE       VALUE 'I'.
               05  CUST-ADDRESSES     OCCURS 3 TIMES.
                   10  ADDR-LINE1    PIC X(50).
                   10  ADDR-LINE2    PIC X(50).
                   10  ADDR-CITY     PIC X(30).
               05  REDEF-AREA         REDEFINES CUST-NAME PIC X(60).
        """;

    [Fact]
    public void Parse_WithValidCopybook_ShouldExtractCopybookName()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        result.CopybookName.Should().Be("CUSTOMER-RECORD");
    }

    [Fact]
    public void Parse_WithValidCopybook_ShouldExtractAllFields()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        // 01 CUSTOMER-RECORD, 05 CUST-ID, 05 CUST-NAME, 10 FIRST-NAME, 10 LAST-NAME,
        // 05 CUST-BALANCE, 05 CUST-STATUS, 88 ACTIVE, 88 INACTIVE,
        // 05 CUST-ADDRESSES, 10 ADDR-LINE1, 10 ADDR-LINE2, 10 ADDR-CITY, 05 REDEF-AREA
        result.Fields.Should().HaveCountGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Parse_WithNumericField_ShouldComputeCorrectLength()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        var custId = result.Fields.First(f => f.Name == "CUST-ID");
        custId.DataType.Should().Be("numeric");
        custId.Length.Should().Be(10);
        custId.Offset.Should().Be(0);
    }

    [Fact]
    public void Parse_WithAlphanumericField_ShouldComputeCorrectLength()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        var firstName = result.Fields.First(f => f.Name == "FIRST-NAME");
        firstName.DataType.Should().Be("alphanumeric");
        firstName.Length.Should().Be(30);
    }

    [Fact]
    public void Parse_WithComp3Field_ShouldComputePackedLength()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        var balance = result.Fields.First(f => f.Name == "CUST-BALANCE");
        // S9(9)V99 = 11 digits total + 1 sign nibble = 12 nibbles / 2 = 6 bytes
        balance.Length.Should().Be(6);
        balance.DecimalPositions.Should().Be(2);
    }

    [Fact]
    public void Parse_WithGroupField_ShouldMarkAsGroup()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        var custName = result.Fields.First(f => f.Name == "CUST-NAME");
        custName.IsGroup.Should().BeTrue();
        custName.DataType.Should().Be("group");
        custName.Length.Should().Be(60); // FIRST-NAME(30) + LAST-NAME(30)
    }

    [Fact]
    public void Parse_With88Level_ShouldExtractConditionValues()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        var active = result.Fields.First(f => f.Name == "ACTIVE");
        active.Level.Should().Be(88);
        active.DataType.Should().Be("condition");
        active.ConditionValues.Should().Contain("A");
    }

    [Fact]
    public void Parse_WithOccurs_ShouldExtractCount()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        var addresses = result.Fields.First(f => f.Name == "CUST-ADDRESSES");
        addresses.OccursCount.Should().Be(3);
        addresses.IsGroup.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithRedefines_ShouldMarkRedefinesAndTarget()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        var redef = result.Fields.First(f => f.Name == "REDEF-AREA");
        redef.IsRedefines.Should().BeTrue();
        redef.RedefinesTarget.Should().Be("CUST-NAME");
    }

    [Fact]
    public void Parse_WithRedefines_ShouldNotAdvanceOffset()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        var redef = result.Fields.First(f => f.Name == "REDEF-AREA");
        var custName = result.Fields.First(f => f.Name == "CUST-NAME");

        // REDEFINES should overlay at the same offset as CUST-NAME
        redef.Offset.Should().Be(custName.Offset);
    }

    [Fact]
    public void Parse_WithNullInput_ShouldThrow()
    {
        var act = () => CopybookParser.Parse(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_WithWhitespaceInput_ShouldThrow()
    {
        var act = () => CopybookParser.Parse("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_WithRecordFormat_ShouldDefaultToFB()
    {
        var result = CopybookParser.Parse(SimpleCustomerCopybook);

        result.RecordFormat.Should().Be("FB");
    }

    [Fact]
    public void Parse_WithCommentLines_ShouldSkipComments()
    {
        const string copybook = """
              *  This is a comment line
               01  SIMPLE-RECORD.
              *  Another comment
                   05  FIELD-A           PIC X(10).
            """;

        var result = CopybookParser.Parse(copybook);

        result.Fields.Should().HaveCountGreaterThanOrEqualTo(2);
        result.CopybookName.Should().Be("SIMPLE-RECORD");
    }

    [Fact]
    public void Parse_WithLevel77_ShouldParseAsStandalone()
    {
        const string copybook = """
               77  STANDALONE-FIELD      PIC 9(5).
               01  RECORD-A.
                   05  FIELD-A           PIC X(10).
            """;

        var result = CopybookParser.Parse(copybook);

        var standalone = result.Fields.First(f => f.Name == "STANDALONE-FIELD");
        standalone.Level.Should().Be(77);
        standalone.DataType.Should().Be("numeric");
        standalone.Length.Should().Be(5);
    }

    [Fact]
    public void Parse_WithBinaryComp_ShouldComputeCorrectLength()
    {
        const string copybook = """
               01  BINARY-RECORD.
                   05  SMALL-BIN         PIC 9(4) COMP.
                   05  MEDIUM-BIN        PIC 9(9) COMP.
                   05  LARGE-BIN         PIC 9(18) COMP.
            """;

        var result = CopybookParser.Parse(copybook);

        result.Fields.First(f => f.Name == "SMALL-BIN").Length.Should().Be(2);
        result.Fields.First(f => f.Name == "MEDIUM-BIN").Length.Should().Be(4);
        result.Fields.First(f => f.Name == "LARGE-BIN").Length.Should().Be(8);
    }

    [Fact]
    public void Parse_WithAlphabeticField_ShouldDetectType()
    {
        const string copybook = """
               01  ALPHA-RECORD.
                   05  NAME-FIELD        PIC A(20).
            """;

        var result = CopybookParser.Parse(copybook);

        var field = result.Fields.First(f => f.Name == "NAME-FIELD");
        field.DataType.Should().Be("alphabetic");
        field.Length.Should().Be(20);
    }

    [Fact]
    public void Parse_TotalLength_ShouldMatchExpected()
    {
        const string copybook = """
               01  SIMPLE-RECORD.
                   05  FIELD-A           PIC X(10).
                   05  FIELD-B           PIC 9(5).
            """;

        var result = CopybookParser.Parse(copybook);

        result.TotalLength.Should().Be(15); // 10 + 5
    }

    [Fact]
    public void Parse_WithSignedDecimalDisplay_ShouldComputeCorrectly()
    {
        const string copybook = """
               01  DECIMAL-RECORD.
                   05  AMOUNT            PIC S9(7)V99.
            """;

        var result = CopybookParser.Parse(copybook);

        var field = result.Fields.First(f => f.Name == "AMOUNT");
        field.DataType.Should().Be("signed-decimal");
        field.DecimalPositions.Should().Be(2);
        // S(1) + 9(7) + V + 99 display = 1 + 7 + 2 = 10 bytes
        field.Length.Should().Be(10);
    }
}
