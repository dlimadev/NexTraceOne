using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="SemanticDiffResult"/>.
/// Valida criação via factory method Generate, guarda de parâmetros, limites de negócio,
/// trimming de strings e imutabilidade.
/// </summary>
public sealed class SemanticDiffResultTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 07, 01, 12, 0, 0, TimeSpan.Zero);

    // ── Factory method: Generate — valid scenarios ──

    [Fact]
    public void Generate_ValidInputs_ShouldSetAllFields()
    {
        var result = CreateValid();

        result.Id.Value.Should().NotBeEmpty();
        result.ContractVersionFromId.Should().Be("v1.0.0");
        result.ContractVersionToId.Should().Be("v2.0.0");
        result.NaturalLanguageSummary.Should().Be("Removed field legacyId, added field email.");
        result.Classification.Should().Be(SemanticDiffClassification.Breaking);
        result.AffectedConsumers.Should().Be("[\"OrderService\",\"BillingService\"]");
        result.MitigationSuggestions.Should().Be("[\"Notify consumers 2 weeks before removal\"]");
        result.CompatibilityScore.Should().Be(35);
        result.GeneratedByModel.Should().Be("gpt-4o");
        result.GeneratedAt.Should().Be(FixedNow);
        result.TenantId.Should().Be("tenant-abc");
    }

    [Fact]
    public void Generate_NullOptionalFields_ShouldBeValid()
    {
        var result = SemanticDiffResult.Generate(
            "v1.0.0",
            "v1.1.0",
            "Minor additions only.",
            SemanticDiffClassification.Enhancement,
            null,
            null,
            100,
            "gpt-4o",
            FixedNow);

        result.AffectedConsumers.Should().BeNull();
        result.MitigationSuggestions.Should().BeNull();
        result.TenantId.Should().BeNull();
    }

    [Fact]
    public void Generate_BoundaryScore0_ShouldBeValid()
    {
        var result = SemanticDiffResult.Generate(
            "v1.0.0",
            "v2.0.0",
            "Complete incompatibility.",
            SemanticDiffClassification.Breaking,
            null,
            null,
            0,
            "gpt-4o",
            FixedNow);

        result.CompatibilityScore.Should().Be(0);
    }

    [Fact]
    public void Generate_BoundaryScore100_ShouldBeValid()
    {
        var result = SemanticDiffResult.Generate(
            "v1.0.0",
            "v1.0.1",
            "Fully compatible patch.",
            SemanticDiffClassification.NonBreaking,
            null,
            null,
            100,
            "gpt-4o",
            FixedNow);

        result.CompatibilityScore.Should().Be(100);
    }

    [Fact]
    public void Generate_AllClassifications_ShouldBeAccepted()
    {
        foreach (var classification in Enum.GetValues<SemanticDiffClassification>())
        {
            var result = SemanticDiffResult.Generate(
                "v1.0.0",
                "v2.0.0",
                "Test summary for classification.",
                classification,
                null,
                null,
                50,
                "test-model",
                FixedNow);

            result.Classification.Should().Be(classification);
        }
    }

    [Fact]
    public void Generate_TrimsStrings()
    {
        var result = SemanticDiffResult.Generate(
            "  v1.0.0  ",
            "  v2.0.0  ",
            "  Some summary  ",
            SemanticDiffClassification.NonBreaking,
            null,
            null,
            80,
            "  gpt-4o  ",
            FixedNow,
            "  tenant-1  ");

        result.ContractVersionFromId.Should().Be("v1.0.0");
        result.ContractVersionToId.Should().Be("v2.0.0");
        result.NaturalLanguageSummary.Should().Be("Some summary");
        result.GeneratedByModel.Should().Be("gpt-4o");
        result.TenantId.Should().Be("tenant-1");
    }

    // ── Guard clauses ──

    [Fact]
    public void Generate_EmptyContractVersionFromId_ShouldThrow()
    {
        var act = () => SemanticDiffResult.Generate(
            "",
            "v2.0.0",
            "Summary.",
            SemanticDiffClassification.Breaking,
            null, null, 50, "gpt-4o", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_EmptyContractVersionToId_ShouldThrow()
    {
        var act = () => SemanticDiffResult.Generate(
            "v1.0.0",
            "",
            "Summary.",
            SemanticDiffClassification.Breaking,
            null, null, 50, "gpt-4o", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_EmptyNaturalLanguageSummary_ShouldThrow()
    {
        var act = () => SemanticDiffResult.Generate(
            "v1.0.0",
            "v2.0.0",
            "",
            SemanticDiffClassification.Breaking,
            null, null, 50, "gpt-4o", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_EmptyGeneratedByModel_ShouldThrow()
    {
        var act = () => SemanticDiffResult.Generate(
            "v1.0.0",
            "v2.0.0",
            "Summary.",
            SemanticDiffClassification.Breaking,
            null, null, 50, "", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_ScoreAbove100_ShouldThrow()
    {
        var act = () => SemanticDiffResult.Generate(
            "v1.0.0",
            "v2.0.0",
            "Summary.",
            SemanticDiffClassification.Breaking,
            null, null, 101, "gpt-4o", FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Generate_NegativeScore_ShouldThrow()
    {
        var act = () => SemanticDiffResult.Generate(
            "v1.0.0",
            "v2.0.0",
            "Summary.",
            SemanticDiffClassification.Breaking,
            null, null, -1, "gpt-4o", FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Generate_ContractVersionFromIdTooLong_ShouldThrow()
    {
        var act = () => SemanticDiffResult.Generate(
            new string('x', 201),
            "v2.0.0",
            "Summary.",
            SemanticDiffClassification.Breaking,
            null, null, 50, "gpt-4o", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_ContractVersionToIdTooLong_ShouldThrow()
    {
        var act = () => SemanticDiffResult.Generate(
            "v1.0.0",
            new string('x', 201),
            "Summary.",
            SemanticDiffClassification.Breaking,
            null, null, 50, "gpt-4o", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_NaturalLanguageSummaryTooLong_ShouldThrow()
    {
        var act = () => SemanticDiffResult.Generate(
            "v1.0.0",
            "v2.0.0",
            new string('x', 8001),
            SemanticDiffClassification.Breaking,
            null, null, 50, "gpt-4o", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_GeneratedByModelTooLong_ShouldThrow()
    {
        var act = () => SemanticDiffResult.Generate(
            "v1.0.0",
            "v2.0.0",
            "Summary.",
            SemanticDiffClassification.Breaking,
            null, null, 50, new string('x', 201), FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Strongly typed Id ──

    [Fact]
    public void SemanticDiffResultId_New_ShouldGenerateUniqueIds()
    {
        var id1 = SemanticDiffResultId.New();
        var id2 = SemanticDiffResultId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void SemanticDiffResultId_From_ShouldPreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = SemanticDiffResultId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──

    private static SemanticDiffResult CreateValid() => SemanticDiffResult.Generate(
        contractVersionFromId: "v1.0.0",
        contractVersionToId: "v2.0.0",
        naturalLanguageSummary: "Removed field legacyId, added field email.",
        classification: SemanticDiffClassification.Breaking,
        affectedConsumers: "[\"OrderService\",\"BillingService\"]",
        mitigationSuggestions: "[\"Notify consumers 2 weeks before removal\"]",
        compatibilityScore: 35,
        generatedByModel: "gpt-4o",
        generatedAt: FixedNow,
        tenantId: "tenant-abc");
}
