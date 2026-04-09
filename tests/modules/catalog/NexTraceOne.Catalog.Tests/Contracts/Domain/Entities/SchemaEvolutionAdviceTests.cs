using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="SchemaEvolutionAdvice"/>.
/// Valida criação via factory method, guarda de parâmetros e imutabilidade.
/// </summary>
public sealed class SchemaEvolutionAdviceTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 07, 01, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Analyze_Should_SetAllFields()
    {
        var apiAssetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var advice = SchemaEvolutionAdvice.Analyze(
            apiAssetId,
            "UserService API",
            "1.0.0",
            "2.0.0",
            CompatibilityLevel.BreakingChange,
            35,
            "[\"email\"]",
            "[\"legacyId\"]",
            "[\"name\"]",
            "[\"legacyId\",\"name\"]",
            "[\"OrderService\",\"BillingService\"]",
            2,
            MigrationStrategy.DualWrite,
            "{\"plan\":\"dual-write for 30 days\"}",
            "[\"Notify consumers 2 weeks before removal\"]",
            "[\"Field legacyId is used by 2 consumers\"]",
            FixedNow,
            "schema-evolution-agent",
            tenantId);

        advice.Id.Value.Should().NotBeEmpty();
        advice.ApiAssetId.Should().Be(apiAssetId);
        advice.ContractName.Should().Be("UserService API");
        advice.SourceVersion.Should().Be("1.0.0");
        advice.TargetVersion.Should().Be("2.0.0");
        advice.CompatibilityLevel.Should().Be(CompatibilityLevel.BreakingChange);
        advice.CompatibilityScore.Should().Be(35);
        advice.FieldsAdded.Should().Be("[\"email\"]");
        advice.FieldsRemoved.Should().Be("[\"legacyId\"]");
        advice.FieldsModified.Should().Be("[\"name\"]");
        advice.FieldsInUseByConsumers.Should().Be("[\"legacyId\",\"name\"]");
        advice.AffectedConsumers.Should().Be("[\"OrderService\",\"BillingService\"]");
        advice.AffectedConsumerCount.Should().Be(2);
        advice.RecommendedStrategy.Should().Be(MigrationStrategy.DualWrite);
        advice.StrategyDetails.Should().Contain("dual-write");
        advice.Recommendations.Should().Contain("Notify consumers");
        advice.Warnings.Should().Contain("legacyId");
        advice.AnalyzedAt.Should().Be(FixedNow);
        advice.AnalyzedByAgentName.Should().Be("schema-evolution-agent");
        advice.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Analyze_Should_AllowNullOptionalFields()
    {
        var advice = SchemaEvolutionAdvice.Analyze(
            Guid.NewGuid(),
            "PaymentService API",
            "1.0.0",
            "1.1.0",
            CompatibilityLevel.FullyCompatible,
            100,
            null,
            null,
            null,
            null,
            null,
            0,
            MigrationStrategy.Versioning,
            null,
            null,
            null,
            FixedNow);

        advice.FieldsAdded.Should().BeNull();
        advice.FieldsRemoved.Should().BeNull();
        advice.FieldsModified.Should().BeNull();
        advice.FieldsInUseByConsumers.Should().BeNull();
        advice.AffectedConsumers.Should().BeNull();
        advice.AffectedConsumerCount.Should().Be(0);
        advice.AnalyzedByAgentName.Should().BeNull();
        advice.TenantId.Should().BeNull();
    }

    [Fact]
    public void Analyze_Should_ThrowForDefaultApiAssetId()
    {
        var act = () => SchemaEvolutionAdvice.Analyze(
            Guid.Empty,
            "Test",
            "1.0.0",
            "2.0.0",
            CompatibilityLevel.BackwardCompatible,
            80,
            null, null, null, null, null,
            0,
            MigrationStrategy.Versioning,
            null, null, null,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Analyze_Should_ThrowForEmptyContractName()
    {
        var act = () => SchemaEvolutionAdvice.Analyze(
            Guid.NewGuid(),
            "",
            "1.0.0",
            "2.0.0",
            CompatibilityLevel.BackwardCompatible,
            80,
            null, null, null, null, null,
            0,
            MigrationStrategy.Versioning,
            null, null, null,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Analyze_Should_ThrowForScoreAbove100()
    {
        var act = () => SchemaEvolutionAdvice.Analyze(
            Guid.NewGuid(),
            "Test API",
            "1.0.0",
            "2.0.0",
            CompatibilityLevel.BackwardCompatible,
            101,
            null, null, null, null, null,
            0,
            MigrationStrategy.Versioning,
            null, null, null,
            FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Analyze_Should_ThrowForNegativeScore()
    {
        var act = () => SchemaEvolutionAdvice.Analyze(
            Guid.NewGuid(),
            "Test API",
            "1.0.0",
            "2.0.0",
            CompatibilityLevel.BackwardCompatible,
            -1,
            null, null, null, null, null,
            0,
            MigrationStrategy.Versioning,
            null, null, null,
            FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Analyze_Should_ThrowForNegativeConsumerCount()
    {
        var act = () => SchemaEvolutionAdvice.Analyze(
            Guid.NewGuid(),
            "Test API",
            "1.0.0",
            "2.0.0",
            CompatibilityLevel.BackwardCompatible,
            80,
            null, null, null, null, null,
            -1,
            MigrationStrategy.Versioning,
            null, null, null,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SchemaEvolutionAdviceId_New_Should_GenerateUniqueIds()
    {
        var id1 = SchemaEvolutionAdviceId.New();
        var id2 = SchemaEvolutionAdviceId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void SchemaEvolutionAdviceId_From_Should_PreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = SchemaEvolutionAdviceId.From(guid);

        id.Value.Should().Be(guid);
    }
}
