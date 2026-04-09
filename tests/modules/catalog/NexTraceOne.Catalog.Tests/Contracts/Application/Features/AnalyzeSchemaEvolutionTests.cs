using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using AnalyzeSchemaEvolutionFeature = NexTraceOne.Catalog.Application.Contracts.Features.AnalyzeSchemaEvolution.AnalyzeSchemaEvolution;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler AnalyzeSchemaEvolution — criação de análise de evolução de schema.
/// Valida a criação e persistência da análise com diferentes cenários de compatibilidade.
/// </summary>
public sealed class AnalyzeSchemaEvolutionTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 07, 01, 12, 0, 0, TimeSpan.Zero);

    private static IContractsUnitOfWork CreateUnitOfWork() => Substitute.For<IContractsUnitOfWork>();

    [Fact]
    public async Task Handle_Should_CreateAndPersistAdvice()
    {
        var repository = Substitute.For<ISchemaEvolutionAdviceRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new AnalyzeSchemaEvolutionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        var command = new AnalyzeSchemaEvolutionFeature.Command(
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
            "schema-agent");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractName.Should().Be("UserService API");
        result.Value.CompatibilityLevel.Should().Be(CompatibilityLevel.BreakingChange);
        result.Value.CompatibilityScore.Should().Be(35);
        result.Value.RecommendedStrategy.Should().Be(MigrationStrategy.DualWrite);
        result.Value.AffectedConsumerCount.Should().Be(1);
        result.Value.AnalyzedAt.Should().Be(FixedNow);
        result.Value.AdviceId.Should().NotBeEmpty();

        await repository.Received(1).AddAsync(Arg.Any<SchemaEvolutionAdvice>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnResponse_When_FullyCompatible()
    {
        var repository = Substitute.For<ISchemaEvolutionAdviceRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new AnalyzeSchemaEvolutionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        var command = new AnalyzeSchemaEvolutionFeature.Command(
            Guid.NewGuid(),
            "PaymentService API",
            "1.0.0",
            "1.0.1",
            CompatibilityLevel.FullyCompatible,
            100,
            null, null, null, null, null,
            0,
            MigrationStrategy.Versioning,
            null, null, null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompatibilityLevel.Should().Be(CompatibilityLevel.FullyCompatible);
        result.Value.CompatibilityScore.Should().Be(100);
        result.Value.AffectedConsumerCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_CommitUnitOfWork()
    {
        var repository = Substitute.For<ISchemaEvolutionAdviceRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new AnalyzeSchemaEvolutionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        var command = new AnalyzeSchemaEvolutionFeature.Command(
            Guid.NewGuid(),
            "Test API",
            "1.0.0",
            "1.1.0",
            CompatibilityLevel.BackwardCompatible,
            85,
            null, null, null, null, null,
            0,
            MigrationStrategy.LazyMigration,
            null, null, null);

        await sut.Handle(command, CancellationToken.None);

        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
