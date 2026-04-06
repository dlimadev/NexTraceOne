using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using GetCanonicalEntityImpactCascadeFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetCanonicalEntityImpactCascade.GetCanonicalEntityImpactCascade;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler GetCanonicalEntityImpactCascade — análise em cascata do impacto de entidades canónicas.
/// </summary>
public sealed class CanonicalEntityImpactCascadeTests
{
    private static readonly Guid EntityId = Guid.NewGuid();
    private const string EntityName = "CustomerAddress";

    private static CanonicalEntity CreateEntity(string name = EntityName)
    {
        var entity = CanonicalEntity.Create(name, "Test entity", "payments", "entity", "team-a", """{"type":"object"}""");
        return entity;
    }

    private static ContractVersion CreateVersion(string spec)
        => ContractVersion.Import(Guid.NewGuid(), "1.0.0", spec, "json", "upload", ContractProtocol.OpenApi).Value;

    private GetCanonicalEntityImpactCascadeFeature.Handler CreateHandler(
        ICanonicalEntityRepository entityRepo,
        IContractVersionRepository contractRepo)
        => new(entityRepo, contractRepo);

    [Fact]
    public async Task Handle_ShouldReturnError_WhenEntityNotFound()
    {
        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        entityRepo.GetByIdAsync(Arg.Any<CanonicalEntityId>(), Arg.Any<CancellationToken>())
            .Returns((CanonicalEntity?)null);

        var sut = CreateHandler(entityRepo, contractRepo);
        var result = await sut.Handle(new GetCanonicalEntityImpactCascadeFeature.Query(EntityId, 2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnRootEntity_WhenNoContractsAffected()
    {
        var entity = CreateEntity();
        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();

        entityRepo.GetByIdAsync(Arg.Any<CanonicalEntityId>(), Arg.Any<CancellationToken>())
            .Returns(entity);
        contractRepo.SearchAsync(Arg.Any<ContractProtocol?>(), Arg.Any<ContractLifecycleState?>(),
                Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ContractVersion>)[], 0));

        var sut = CreateHandler(entityRepo, contractRepo);
        var result = await sut.Handle(new GetCanonicalEntityImpactCascadeFeature.Query(EntityId, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RootEntityName.Should().Be(EntityName);
        result.Value.TotalContractsAffected.Should().Be(0);
        result.Value.RiskLevel.Should().Be("None");
    }

    [Fact]
    public async Task Handle_ShouldComputeMediumRisk_WhenFiveContractsAffected()
    {
        var entity = CreateEntity();
        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var spec = "{\"openapi\":\"3.1.0\",\"components\":{\"schemas\":{\"" + EntityName + "\":{}}},\"paths\":{\"/test\":{\"get\":{}}}}";
        var contracts = Enumerable.Range(0, 5).Select(_ => CreateVersion(spec)).ToList();

        entityRepo.GetByIdAsync(Arg.Any<CanonicalEntityId>(), Arg.Any<CancellationToken>())
            .Returns(entity);
        contractRepo.SearchAsync(Arg.Any<ContractProtocol?>(), Arg.Any<ContractLifecycleState?>(),
                Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ContractVersion>)contracts, contracts.Count));

        var sut = CreateHandler(entityRepo, contractRepo);
        var result = await sut.Handle(new GetCanonicalEntityImpactCascadeFeature.Query(EntityId, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalContractsAffected.Should().Be(5);
        result.Value.RiskLevel.Should().Be("Medium");
    }

    [Fact]
    public async Task Handle_ShouldReturnCriticalRisk_WhenManyContractsAffected()
    {
        var entity = CreateEntity();
        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var spec = "{\"" + EntityName + "\":true}";
        var contracts = Enumerable.Range(0, 30).Select(_ => CreateVersion(spec)).ToList();

        entityRepo.GetByIdAsync(Arg.Any<CanonicalEntityId>(), Arg.Any<CancellationToken>())
            .Returns(entity);
        contractRepo.SearchAsync(Arg.Any<ContractProtocol?>(), Arg.Any<ContractLifecycleState?>(),
                Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ContractVersion>)contracts, contracts.Count));

        var sut = CreateHandler(entityRepo, contractRepo);
        var result = await sut.Handle(new GetCanonicalEntityImpactCascadeFeature.Query(EntityId, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RiskLevel.Should().Be("Critical");
    }

    [Fact]
    public async Task Handle_ShouldExtractSchemaRefs_ForCascadeDepth2()
    {
        var entity = CreateEntity();
        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();

        // Spec com $ref para entidade relacionada
        var specWithRef = "{\"openapi\":\"3.1.0\",\"paths\":{\"/test\":{\"get\":{}}},\"components\":{\"schemas\":{\"" + EntityName + "\":{\"$ref\":\"#/components/schemas/PaymentMethod\"}}}}";
        var rootContracts = new List<ContractVersion> { CreateVersion(specWithRef) };
        var childContracts = new List<ContractVersion>();

        entityRepo.GetByIdAsync(Arg.Any<CanonicalEntityId>(), Arg.Any<CancellationToken>())
            .Returns(entity);

        // Para root entity retorna contrato com $ref
        contractRepo.SearchAsync(Arg.Any<ContractProtocol?>(), Arg.Any<ContractLifecycleState?>(),
                Arg.Any<Guid?>(), Arg.Is<string?>(s => s == EntityName), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ContractVersion>)rootContracts, 1));
        // Para entidade filha não retorna contratos
        contractRepo.SearchAsync(Arg.Any<ContractProtocol?>(), Arg.Any<ContractLifecycleState?>(),
                Arg.Any<Guid?>(), Arg.Is<string?>(s => s != EntityName), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ContractVersion>)childContracts, 0));

        var sut = CreateHandler(entityRepo, contractRepo);
        var result = await sut.Handle(new GetCanonicalEntityImpactCascadeFeature.Query(EntityId, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Deve ter encontrado PaymentMethod como entidade relacionada
        result.Value!.TotalUniqueEntitiesInCascade.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenEntityIdIsEmpty()
    {
        var validator = new GetCanonicalEntityImpactCascadeFeature.Validator();
        var vr = await validator.ValidateAsync(new GetCanonicalEntityImpactCascadeFeature.Query(Guid.Empty, 2));
        vr.IsValid.Should().BeFalse();
    }
}
