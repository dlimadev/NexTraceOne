using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.DiffCopybookVersions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Application.Handlers;

/// <summary>
/// Testes do handler DiffCopybookVersions do sub-domínio Legacy Assets.
/// Cobre diff com sucesso, versão não encontrada, e classificação de change level.
/// </summary>
public sealed class DiffCopybookVersionsHandlerTests
{
    private const string BaseVersionContent =
        """
               01 CUSTOMER-REC.
                   05 CUST-ID     PIC 9(8).
                   05 CUST-NAME   PIC X(30).
        """;

    private const string TargetVersionContentBreaking =
        """
               01 CUSTOMER-REC.
                   05 CUST-ID     PIC 9(10).
                   05 CUST-NAME   PIC X(30).
        """;

    private const string TargetVersionContentAdditive =
        """
               01 CUSTOMER-REC.
                   05 CUST-ID     PIC 9(8).
                   05 CUST-NAME   PIC X(30).
                   05 CUST-EMAIL  PIC X(50).
        """;

    private static ICopybookVersionRepository CreateVersionRepo() =>
        Substitute.For<ICopybookVersionRepository>();

    private static CopybookVersion CreateVersion(string content, string label)
    {
        return CopybookVersion.Create(
            CopybookId.New(), label, content, 3, 80, "FB");
    }

    [Fact]
    public async Task Handle_WithBreakingChange_ShouldReturnBreakingLevel()
    {
        var versionRepo = CreateVersionRepo();
        var baseVersion = CreateVersion(BaseVersionContent, "v1.0");
        var targetVersion = CreateVersion(TargetVersionContentBreaking, "v2.0");

        versionRepo.GetByIdAsync(baseVersion.Id, Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        versionRepo.GetByIdAsync(targetVersion.Id, Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var handler = new DiffCopybookVersions.Handler(versionRepo);
        var query = new DiffCopybookVersions.Query(
            Guid.NewGuid(), baseVersion.Id.Value, targetVersion.Id.Value);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.Value.HasBreakingChanges.Should().BeTrue();
        result.Value.BreakingChanges.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithAdditiveChange_ShouldReturnAdditiveLevel()
    {
        var versionRepo = CreateVersionRepo();
        var baseVersion = CreateVersion(BaseVersionContent, "v1.0");
        var targetVersion = CreateVersion(TargetVersionContentAdditive, "v2.0");

        versionRepo.GetByIdAsync(baseVersion.Id, Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        versionRepo.GetByIdAsync(targetVersion.Id, Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var handler = new DiffCopybookVersions.Handler(versionRepo);
        var query = new DiffCopybookVersions.Query(
            Guid.NewGuid(), baseVersion.Id.Value, targetVersion.Id.Value);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeLevel.Should().Be(ChangeLevel.Additive);
        result.Value.HasBreakingChanges.Should().BeFalse();
        result.Value.AdditiveChanges.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithSameContent_ShouldReturnNonBreakingLevel()
    {
        var versionRepo = CreateVersionRepo();
        var baseVersion = CreateVersion(BaseVersionContent, "v1.0");
        var targetVersion = CreateVersion(BaseVersionContent, "v2.0");

        versionRepo.GetByIdAsync(baseVersion.Id, Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        versionRepo.GetByIdAsync(targetVersion.Id, Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var handler = new DiffCopybookVersions.Handler(versionRepo);
        var query = new DiffCopybookVersions.Query(
            Guid.NewGuid(), baseVersion.Id.Value, targetVersion.Id.Value);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasBreakingChanges.Should().BeFalse();
        result.Value.BreakingChanges.Should().BeEmpty();
        result.Value.AdditiveChanges.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenBaseVersionNotFound_ShouldReturnError()
    {
        var versionRepo = CreateVersionRepo();
        versionRepo.GetByIdAsync(Arg.Any<CopybookVersionId>(), Arg.Any<CancellationToken>())
            .Returns((CopybookVersion?)null);

        var handler = new DiffCopybookVersions.Handler(versionRepo);
        var query = new DiffCopybookVersions.Query(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WhenTargetVersionNotFound_ShouldReturnError()
    {
        var versionRepo = CreateVersionRepo();
        var baseVersion = CreateVersion(BaseVersionContent, "v1.0");
        versionRepo.GetByIdAsync(baseVersion.Id, Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        versionRepo.GetByIdAsync(Arg.Is<CopybookVersionId>(id => id != baseVersion.Id), Arg.Any<CancellationToken>())
            .Returns((CopybookVersion?)null);

        var handler = new DiffCopybookVersions.Handler(versionRepo);
        var query = new DiffCopybookVersions.Query(
            Guid.NewGuid(), baseVersion.Id.Value, Guid.NewGuid());
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnVersionLabelsInResponse()
    {
        var versionRepo = CreateVersionRepo();
        var baseVersion = CreateVersion(BaseVersionContent, "v1.0");
        var targetVersion = CreateVersion(BaseVersionContent, "v2.0");

        versionRepo.GetByIdAsync(baseVersion.Id, Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        versionRepo.GetByIdAsync(targetVersion.Id, Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var handler = new DiffCopybookVersions.Handler(versionRepo);
        var query = new DiffCopybookVersions.Query(
            Guid.NewGuid(), baseVersion.Id.Value, targetVersion.Id.Value);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseVersion.Should().Be("v1.0");
        result.Value.TargetVersion.Should().Be("v2.0");
    }
}
