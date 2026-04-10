using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using GetSchemaEvolutionAdviceFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetSchemaEvolutionAdvice.GetSchemaEvolutionAdvice;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler GetSchemaEvolutionAdvice — obtenção de análise por identificador.
/// Valida cenários de sucesso (encontrado) e falha (não encontrado).
/// </summary>
public sealed class GetSchemaEvolutionAdviceTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 07, 01, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Should_ReturnAdvice_When_Found()
    {
        var repository = Substitute.For<ISchemaEvolutionAdviceRepository>();
        var sut = new GetSchemaEvolutionAdviceFeature.Handler(repository);

        var advice = SchemaEvolutionAdvice.Analyze(
            Guid.NewGuid(),
            "UserService API",
            "1.0.0",
            "2.0.0",
            CompatibilityLevel.BreakingChange,
            35,
            "[\"email\"]",
            "[\"legacyId\"]",
            "[\"name\"]",
            "[\"legacyId\"]",
            "[\"OrderService\"]",
            1,
            MigrationStrategy.DualWrite,
            "{\"plan\":\"dual-write\"}",
            "[\"Notify consumers\"]",
            "[\"Field removed\"]",
            FixedNow,
            "schema-agent");

        repository.GetByIdAsync(Arg.Any<SchemaEvolutionAdviceId>(), Arg.Any<CancellationToken>())
            .Returns(advice);

        var result = await sut.Handle(
            new GetSchemaEvolutionAdviceFeature.Query(advice.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AdviceId.Should().Be(advice.Id.Value);
        result.Value.ContractName.Should().Be("UserService API");
        result.Value.CompatibilityLevel.Should().Be(CompatibilityLevel.BreakingChange);
        result.Value.CompatibilityScore.Should().Be(35);
        result.Value.RecommendedStrategy.Should().Be(MigrationStrategy.DualWrite);
        result.Value.FieldsAdded.Should().Be("[\"email\"]");
        result.Value.FieldsRemoved.Should().Be("[\"legacyId\"]");
        result.Value.FieldsModified.Should().Be("[\"name\"]");
        result.Value.AffectedConsumerCount.Should().Be(1);
        result.Value.AnalyzedByAgentName.Should().Be("schema-agent");
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_AdviceDoesNotExist()
    {
        var repository = Substitute.For<ISchemaEvolutionAdviceRepository>();
        var sut = new GetSchemaEvolutionAdviceFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<SchemaEvolutionAdviceId>(), Arg.Any<CancellationToken>())
            .Returns((SchemaEvolutionAdvice?)null);

        var adviceId = Guid.NewGuid();
        var result = await sut.Handle(
            new GetSchemaEvolutionAdviceFeature.Query(adviceId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.SchemaEvolutionAdvice.NotFound");
    }
}
