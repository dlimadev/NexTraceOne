using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.ValueObjects;

/// <summary>
/// Testes de domínio para o value object SemanticVersion.
/// </summary>
public sealed class SemanticVersionTests
{
    [Fact]
    public void Parse_Should_ReturnSemanticVersion_When_InputIsValid()
    {
        var result = SemanticVersion.Parse("1.2.3");

        result.Should().NotBeNull();
        result!.Major.Should().Be(1);
        result.Minor.Should().Be(2);
        result.Patch.Should().Be(3);
    }

    [Fact]
    public void Parse_Should_ReturnSemanticVersion_When_InputHasZeroMinor()
    {
        var result = SemanticVersion.Parse("0.1.0");

        result.Should().NotBeNull();
        result!.Major.Should().Be(0);
        result.Minor.Should().Be(1);
        result.Patch.Should().Be(0);
    }

    [Fact]
    public void Parse_Should_ReturnNull_When_InputIsInvalid()
    {
        var result = SemanticVersion.Parse("invalid");

        result.Should().BeNull();
    }

    [Fact]
    public void BumpMajor_Should_IncrementMajorAndResetMinorAndPatch()
    {
        var version = SemanticVersion.Parse("1.2.3")!;

        var bumped = version.BumpMajor();

        bumped.ToString().Should().Be("2.0.0");
    }

    [Fact]
    public void BumpMinor_Should_IncrementMinorAndResetPatch()
    {
        var version = SemanticVersion.Parse("1.2.3")!;

        var bumped = version.BumpMinor();

        bumped.ToString().Should().Be("1.3.0");
    }

    [Fact]
    public void BumpPatch_Should_IncrementPatch()
    {
        var version = SemanticVersion.Parse("1.2.3")!;

        var bumped = version.BumpPatch();

        bumped.ToString().Should().Be("1.2.4");
    }

    [Fact]
    public void ToString_Should_ReturnFormattedVersion()
    {
        var version = SemanticVersion.Parse("1.2.3")!;

        version.ToString().Should().Be("1.2.3");
    }
}
