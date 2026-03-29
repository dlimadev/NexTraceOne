using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade CopybookDiffRecord do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes e factory method.
/// </summary>
public sealed class CopybookDiffRecordTests
{
    private static CopybookId CreateCopybookId() => CopybookId.New();
    private static CopybookVersionId CreateVersionId() => CopybookVersionId.New();

    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var copybookId = CreateCopybookId();
        var baseId = CreateVersionId();
        var targetId = CreateVersionId();

        var record = CopybookDiffRecord.Create(
            copybookId, baseId, targetId,
            ChangeLevel.Breaking, 2, 1, 3, "[{\"test\":true}]");

        record.CopybookId.Should().Be(copybookId);
        record.BaseVersionId.Should().Be(baseId);
        record.TargetVersionId.Should().Be(targetId);
        record.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        record.BreakingChangeCount.Should().Be(2);
        record.AdditiveChangeCount.Should().Be(1);
        record.NonBreakingChangeCount.Should().Be(3);
        record.ChangesJson.Should().Be("[{\"test\":true}]");
        record.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldSetComputedAt()
    {
        var record = CopybookDiffRecord.Create(
            CreateCopybookId(), CreateVersionId(), CreateVersionId(),
            ChangeLevel.NonBreaking, 0, 0, 1, "[]");

        record.ComputedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithNullCopybookId_ShouldThrow()
    {
        var act = () => CopybookDiffRecord.Create(
            null!, CreateVersionId(), CreateVersionId(),
            ChangeLevel.Breaking, 1, 0, 0, "[]");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullBaseVersionId_ShouldThrow()
    {
        var act = () => CopybookDiffRecord.Create(
            CreateCopybookId(), null!, CreateVersionId(),
            ChangeLevel.Breaking, 1, 0, 0, "[]");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullTargetVersionId_ShouldThrow()
    {
        var act = () => CopybookDiffRecord.Create(
            CreateCopybookId(), CreateVersionId(), null!,
            ChangeLevel.Breaking, 1, 0, 0, "[]");

        act.Should().Throw<ArgumentNullException>();
    }
}
