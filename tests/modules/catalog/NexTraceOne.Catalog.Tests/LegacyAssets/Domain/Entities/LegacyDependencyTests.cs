using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade LegacyDependency do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes e atualização de descrição.
/// </summary>
public sealed class LegacyDependencyTests
{
    private static LegacyDependency CreateDependency() =>
        LegacyDependency.Create(
            Guid.NewGuid(), MainframeAssetType.Program,
            Guid.NewGuid(), MainframeAssetType.Copybook,
            "COPY");

    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var dep = LegacyDependency.Create(
            sourceId, MainframeAssetType.Program,
            targetId, MainframeAssetType.Copybook,
            "COPY");

        dep.SourceAssetId.Should().Be(sourceId);
        dep.SourceAssetType.Should().Be(MainframeAssetType.Program);
        dep.TargetAssetId.Should().Be(targetId);
        dep.TargetAssetType.Should().Be(MainframeAssetType.Copybook);
        dep.DependencyType.Should().Be("COPY");
        dep.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithEmptySourceId_ShouldThrow()
    {
        var act = () => LegacyDependency.Create(
            Guid.Empty, MainframeAssetType.Program,
            Guid.NewGuid(), MainframeAssetType.Copybook,
            "COPY");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyTargetId_ShouldThrow()
    {
        var act = () => LegacyDependency.Create(
            Guid.NewGuid(), MainframeAssetType.Program,
            Guid.Empty, MainframeAssetType.Copybook,
            "COPY");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullDependencyType_ShouldThrow()
    {
        var act = () => LegacyDependency.Create(
            Guid.NewGuid(), MainframeAssetType.Program,
            Guid.NewGuid(), MainframeAssetType.Copybook,
            null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDescription_ShouldUpdateDescription()
    {
        var dep = CreateDependency();

        dep.UpdateDescription("Includes customer copybook");

        dep.Description.Should().Be("Includes customer copybook");
    }
}
