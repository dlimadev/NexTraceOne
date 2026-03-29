using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade CopybookContractMapping do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes e factory method.
/// </summary>
public sealed class CopybookContractMappingTests
{
    private static CopybookId CreateCopybookId() => CopybookId.New();

    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var copybookId = CreateCopybookId();
        var contractVersionId = Guid.NewGuid();

        var mapping = CopybookContractMapping.Create(copybookId, contractVersionId, "REST-to-COBOL");

        mapping.CopybookId.Should().Be(copybookId);
        mapping.ContractVersionId.Should().Be(contractVersionId);
        mapping.MappingType.Should().Be("REST-to-COBOL");
        mapping.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var mapping = CopybookContractMapping.Create(
            CreateCopybookId(), Guid.NewGuid(), "Event-to-COBOL");

        mapping.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_ShouldTrimMappingType()
    {
        var mapping = CopybookContractMapping.Create(
            CreateCopybookId(), Guid.NewGuid(), "  REST-to-COBOL  ");

        mapping.MappingType.Should().Be("REST-to-COBOL");
    }

    [Fact]
    public void Create_WithNullCopybookId_ShouldThrow()
    {
        var act = () => CopybookContractMapping.Create(null!, Guid.NewGuid(), "REST-to-COBOL");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithEmptyContractVersionId_ShouldThrow()
    {
        var act = () => CopybookContractMapping.Create(
            CreateCopybookId(), Guid.Empty, "REST-to-COBOL");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullMappingType_ShouldThrow()
    {
        var act = () => CopybookContractMapping.Create(
            CreateCopybookId(), Guid.NewGuid(), null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyMappingType_ShouldThrow()
    {
        var act = () => CopybookContractMapping.Create(
            CreateCopybookId(), Guid.NewGuid(), "");

        act.Should().Throw<ArgumentException>();
    }
}
