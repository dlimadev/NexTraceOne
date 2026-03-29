using FluentAssertions;

using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.LegacyAssets.Services;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Services;

/// <summary>
/// Testes para o CopybookDiffAdapter e a integração com o ContractDiffCalculator multi-protocolo.
/// </summary>
public sealed class CopybookDiffAdapterTests
{
    private const string BaseCopybook = """
           01  SIMPLE-RECORD.
               05  FIELD-A           PIC X(10).
               05  FIELD-B           PIC 9(5).
        """;

    private const string TargetCopybookFieldAdded = """
           01  SIMPLE-RECORD.
               05  FIELD-A           PIC X(10).
               05  FIELD-B           PIC 9(5).
               05  FIELD-C           PIC X(20).
        """;

    [Fact]
    public void ComputeDiff_ShouldParseAndDiff_WhenValidCopybooks()
    {
        var result = CopybookDiffAdapter.ComputeDiff(BaseCopybook, TargetCopybookFieldAdded);

        result.AdditiveChanges.Should().ContainSingle(c => c.ChangeType == "FieldAdded");
        result.BreakingChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
    }

    [Fact]
    public void ContractDiffCalculator_ShouldRouteToCopybookAdapter_WhenProtocolIsCopybook()
    {
        var result = ContractDiffCalculator.ComputeDiff(
            BaseCopybook, TargetCopybookFieldAdded, ContractProtocol.Copybook);

        result.AdditiveChanges.Should().ContainSingle(c => c.ChangeType == "FieldAdded");
        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
    }

    [Fact]
    public void ContractDiffCalculator_ShouldReturnBreaking_WhenFieldRemoved()
    {
        const string targetMissing = """
               01  SIMPLE-RECORD.
                   05  FIELD-A           PIC X(10).
            """;

        var result = ContractDiffCalculator.ComputeDiff(
            BaseCopybook, targetMissing, ContractProtocol.Copybook);

        result.BreakingChanges.Should().Contain(c => c.ChangeType == "FieldRemoved");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }
}
