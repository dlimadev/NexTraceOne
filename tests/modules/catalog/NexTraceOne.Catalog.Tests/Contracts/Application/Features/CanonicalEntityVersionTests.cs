using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using ListCanonicalEntityVersionsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListCanonicalEntityVersions.ListCanonicalEntityVersions;
using DiffCanonicalEntityVersionsFeature = NexTraceOne.Catalog.Application.Contracts.Features.DiffCanonicalEntityVersions.DiffCanonicalEntityVersions;
using SearchCanonicalEntitiesFeature = NexTraceOne.Catalog.Application.Contracts.Features.SearchCanonicalEntities.SearchCanonicalEntities;
using GetCanonicalEntityUsagesFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetCanonicalEntityUsages.GetCanonicalEntityUsages;
using GetCanonicalEntityImpactFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetCanonicalEntityImpact.GetCanonicalEntityImpact;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes das features de CanonicalEntityVersion, SearchCanonicalEntities,
/// GetCanonicalEntityUsages e GetCanonicalEntityImpact.
/// </summary>
public sealed class CanonicalEntityVersionTests
{
    private const string SampleSchemaV1 = """{"type":"object","properties":{"name":{"type":"string"},"email":{"type":"string"}}}""";
    private const string SampleSchemaV2 = """{"type":"object","properties":{"name":{"type":"string"},"email":{"type":"string"},"phone":{"type":"string"}}}""";
    private const string SampleSpec = """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private static CanonicalEntity CreateTestEntity()
    {
        return CanonicalEntity.Create(
            name: "CustomerAddress",
            description: "Standard customer address model",
            domain: "customer",
            category: "entity",
            owner: "team-a",
            schemaContent: SampleSchemaV1);
    }

    private static CanonicalEntityVersion CreateTestVersion(
        CanonicalEntityId entityId, string version, string schema)
    {
        return CanonicalEntityVersion.Create(
            entityId, version, schema, "json-schema", $"Version {version}", "admin@test.com");
    }

    // ── CanonicalEntityVersion.Create ───────────────────────────────

    [Fact]
    public void CanonicalEntityVersion_Create_Should_SetFields()
    {
        var entityId = CanonicalEntityId.New();
        var version = CanonicalEntityVersion.Create(
            entityId, "1.0.0", SampleSchemaV1, "json-schema", "Initial version", "admin@test.com");

        version.Should().NotBeNull();
        version.Id.Value.Should().NotBe(Guid.Empty);
        version.CanonicalEntityId.Should().Be(entityId);
        version.Version.Should().Be("1.0.0");
        version.SchemaContent.Should().Be(SampleSchemaV1);
        version.SchemaFormat.Should().Be("json-schema");
        version.ChangeDescription.Should().Be("Initial version");
        version.PublishedBy.Should().Be("admin@test.com");
        version.PublishedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── ListCanonicalEntityVersions ────────────────────────────────

    [Fact]
    public async Task ListCanonicalEntityVersions_Should_ReturnVersions()
    {
        var entity = CreateTestEntity();
        var v1 = CreateTestVersion(entity.Id, "1.0.0", SampleSchemaV1);
        var v2 = CreateTestVersion(entity.Id, "2.0.0", SampleSchemaV2);

        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var versionRepo = Substitute.For<ICanonicalEntityVersionRepository>();

        entityRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);
        versionRepo.ListByEntityIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(new List<CanonicalEntityVersion> { v2, v1 });

        var handler = new ListCanonicalEntityVersionsFeature.Handler(entityRepo, versionRepo);
        var result = await handler.Handle(
            new ListCanonicalEntityVersionsFeature.Query(entity.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Versions.Should().HaveCount(2);
        result.Value.EntityName.Should().Be("CustomerAddress");
        result.Value.Versions[0].Version.Should().Be("2.0.0");
        result.Value.Versions[1].Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task ListCanonicalEntityVersions_Should_ReturnEmpty_When_NoVersions()
    {
        var entity = CreateTestEntity();

        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var versionRepo = Substitute.For<ICanonicalEntityVersionRepository>();

        entityRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);
        versionRepo.ListByEntityIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(new List<CanonicalEntityVersion>());

        var handler = new ListCanonicalEntityVersionsFeature.Handler(entityRepo, versionRepo);
        var result = await handler.Handle(
            new ListCanonicalEntityVersionsFeature.Query(entity.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Versions.Should().BeEmpty();
    }

    // ── DiffCanonicalEntityVersions ────────────────────────────────

    [Fact]
    public async Task DiffCanonicalEntityVersions_Should_ReturnDiff()
    {
        var entity = CreateTestEntity();
        var v1 = CreateTestVersion(entity.Id, "1.0.0", SampleSchemaV1);
        var v2 = CreateTestVersion(entity.Id, "2.0.0", SampleSchemaV2);

        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var versionRepo = Substitute.For<ICanonicalEntityVersionRepository>();

        entityRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);
        versionRepo.GetByVersionAsync(entity.Id, "1.0.0", Arg.Any<CancellationToken>())
            .Returns(v1);
        versionRepo.GetByVersionAsync(entity.Id, "2.0.0", Arg.Any<CancellationToken>())
            .Returns(v2);

        var handler = new DiffCanonicalEntityVersionsFeature.Handler(entityRepo, versionRepo);
        var result = await handler.Handle(
            new DiffCanonicalEntityVersionsFeature.Query(entity.Id.Value, "1.0.0", "2.0.0"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FromVersion.Should().Be("1.0.0");
        result.Value.ToVersion.Should().Be("2.0.0");
        result.Value.AddedFields.Should().Contain(f => f.Contains("phone"));
        result.Value.RemovedFields.Should().BeEmpty();
    }

    [Fact]
    public async Task DiffCanonicalEntityVersions_Should_Error_When_VersionNotFound()
    {
        var entity = CreateTestEntity();

        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var versionRepo = Substitute.For<ICanonicalEntityVersionRepository>();

        entityRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);
        versionRepo.GetByVersionAsync(entity.Id, "1.0.0", Arg.Any<CancellationToken>())
            .Returns((CanonicalEntityVersion?)null);

        var handler = new DiffCanonicalEntityVersionsFeature.Handler(entityRepo, versionRepo);
        var result = await handler.Handle(
            new DiffCanonicalEntityVersionsFeature.Query(entity.Id.Value, "1.0.0", "2.0.0"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.CanonicalEntityVersion.NotFound");
    }

    // ── SearchCanonicalEntities ────────────────────────────────────

    [Fact]
    public async Task SearchCanonicalEntities_Should_ReturnResults()
    {
        var entity = CreateTestEntity();
        var entityRepo = Substitute.For<ICanonicalEntityRepository>();

        entityRepo.SearchAsync("Customer", null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<CanonicalEntity> { entity }, 1));

        var handler = new SearchCanonicalEntitiesFeature.Handler(entityRepo);
        var result = await handler.Handle(
            new SearchCanonicalEntitiesFeature.Query("Customer", null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].Name.Should().Be("CustomerAddress");
        result.Value.Items[0].Domain.Should().Be("customer");
    }

    [Fact]
    public async Task SearchCanonicalEntities_Should_ReturnEmpty_When_NoMatch()
    {
        var entityRepo = Substitute.For<ICanonicalEntityRepository>();

        entityRepo.SearchAsync("NonExistent", null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<CanonicalEntity>(), 0));

        var handler = new SearchCanonicalEntitiesFeature.Handler(entityRepo);
        var result = await handler.Handle(
            new SearchCanonicalEntitiesFeature.Query("NonExistent", null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── GetCanonicalEntityUsages ───────────────────────────────────

    [Fact]
    public async Task GetCanonicalEntityUsages_Should_ReturnUsages()
    {
        var entity = CreateTestEntity();
        var contractResult = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0",
            """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0.0"},"components":{"schemas":{"CustomerAddress":{"type":"object"}}}}""",
            "json", "upload");
        var contract = contractResult.Value;

        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();

        entityRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);
        contractRepo.SearchAsync(null, null, null, "CustomerAddress", 1, 500, Arg.Any<CancellationToken>())
            .Returns((new List<ContractVersion> { contract }, 1));

        var handler = new GetCanonicalEntityUsagesFeature.Handler(entityRepo, contractRepo);
        var result = await handler.Handle(
            new GetCanonicalEntityUsagesFeature.Query(entity.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalUsages.Should().Be(1);
        result.Value.Usages.Should().HaveCount(1);
    }

    // ── GetCanonicalEntityImpact ──────────────────────────────────

    [Fact]
    public async Task GetCanonicalEntityImpact_Should_ReturnImpactedContracts()
    {
        var entity = CreateTestEntity();
        var contractResult = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0",
            """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0.0"},"components":{"schemas":{"CustomerAddress":{"type":"object"}}}}""",
            "json", "upload");
        var contract = contractResult.Value;

        var entityRepo = Substitute.For<ICanonicalEntityRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();

        entityRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);
        contractRepo.SearchAsync(null, null, null, "CustomerAddress", 1, 500, Arg.Any<CancellationToken>())
            .Returns((new List<ContractVersion> { contract }, 1));

        var handler = new GetCanonicalEntityImpactFeature.Handler(entityRepo, contractRepo);
        var result = await handler.Handle(
            new GetCanonicalEntityImpactFeature.Query(entity.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalImpacted.Should().Be(1);
        result.Value.RiskLevel.Should().Be("Low");
        result.Value.ImpactedContracts.Should().HaveCount(1);
        result.Value.Criticality.Should().Be("Medium");
    }
}
