using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Tests.Seed;

/// <summary>
/// Testes de unidade para o <see cref="SeedingResult"/> e o contrato do seeder.
/// Valida as propriedades derivadas e os cenários de execução.
/// </summary>
public sealed class SeedingResultTests
{
    // ── SeedingResult propriedades ─────────────────────────────────────

    [Fact]
    public void Total_ShouldBeAddedPlusSkipped()
    {
        var result = new SeedingResult(Added: 100, Skipped: 245);
        result.Total.Should().Be(345);
    }

    [Fact]
    public void IsFirstRun_ShouldBeTrueWhenNothingSkipped()
    {
        var result = new SeedingResult(Added: 345, Skipped: 0);
        result.IsFirstRun.Should().BeTrue();
    }

    [Fact]
    public void IsFirstRun_ShouldBeFalseWhenSomeSkipped()
    {
        var result = new SeedingResult(Added: 5, Skipped: 340);
        result.IsFirstRun.Should().BeFalse();
    }

    [Fact]
    public void IsFirstRun_ShouldBeFalseWhenNothingAdded()
    {
        var result = new SeedingResult(Added: 0, Skipped: 345);
        result.IsFirstRun.Should().BeFalse();
    }

    [Fact]
    public void IsNoOp_ShouldBeTrueWhenNothingAdded()
    {
        var result = new SeedingResult(Added: 0, Skipped: 345);
        result.IsNoOp.Should().BeTrue();
    }

    [Fact]
    public void IsNoOp_ShouldBeFalseWhenSomeAdded()
    {
        var result = new SeedingResult(Added: 1, Skipped: 344);
        result.IsNoOp.Should().BeFalse();
    }

    [Fact]
    public void IsNoOp_ShouldBeFalseOnFirstRun()
    {
        var result = new SeedingResult(Added: 345, Skipped: 0);
        result.IsNoOp.Should().BeFalse();
    }

    [Fact]
    public void ZeroResult_ShouldBeNoOpAndNotFirstRun()
    {
        var result = new SeedingResult(Added: 0, Skipped: 0);
        result.IsNoOp.Should().BeTrue();
        result.IsFirstRun.Should().BeFalse();
        result.Total.Should().Be(0);
    }

    // ── Idempotency contract invariants ───────────────────────────────

    [Fact]
    public void SeedingResult_ShouldBeValueEqual()
    {
        var r1 = new SeedingResult(Added: 10, Skipped: 5);
        var r2 = new SeedingResult(Added: 10, Skipped: 5);
        r1.Should().Be(r2);
    }

    [Fact]
    public void SeedingResult_WithDifferentValues_ShouldNotBeEqual()
    {
        var r1 = new SeedingResult(Added: 10, Skipped: 5);
        var r2 = new SeedingResult(Added: 5, Skipped: 10);
        r1.Should().NotBe(r2);
    }

    // ── Boundary cases ─────────────────────────────────────────────────

    [Theory]
    [InlineData(345, 0)]    // First run — fresh DB
    [InlineData(0, 345)]    // Re-run — all definitions already exist
    [InlineData(10, 335)]   // Partial run — some new definitions added
    public void SeedingResult_CommonScenarios_ShouldHaveCorrectTotal(int added, int skipped)
    {
        var result = new SeedingResult(added, skipped);
        result.Total.Should().Be(added + skipped);
    }
}
