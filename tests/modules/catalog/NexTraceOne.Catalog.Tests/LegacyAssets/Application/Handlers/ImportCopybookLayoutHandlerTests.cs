using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.ImportCopybookLayout;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Application.Handlers;

/// <summary>
/// Testes do handler ImportCopybookLayout do sub-domínio Legacy Assets.
/// Cobre importação com sucesso, copybook não encontrado e parse com falha.
/// </summary>
public sealed class ImportCopybookLayoutHandlerTests
{
    private const string ValidCopybookText =
        """
               01 CUSTOMER-REC.
                   05 CUST-ID     PIC 9(8).
                   05 CUST-NAME   PIC X(30).
                   05 CUST-BALANCE PIC S9(9)V99.
        """;

    private static ICopybookRepository CreateCopybookRepo() =>
        Substitute.For<ICopybookRepository>();

    private static ICopybookVersionRepository CreateVersionRepo() =>
        Substitute.For<ICopybookVersionRepository>();

    private static ILegacyAssetsUnitOfWork CreateUnitOfWork() =>
        Substitute.For<ILegacyAssetsUnitOfWork>();

    private static Copybook CreateExistingCopybook()
    {
        var layout = CopybookLayout.Create(3, 80, "FB");
        return Copybook.Create("CPY-CUSTOMER", MainframeSystemId.New(), layout);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateVersionAndReturn()
    {
        var copybookRepo = CreateCopybookRepo();
        var copybook = CreateExistingCopybook();
        copybookRepo.GetByIdAsync(Arg.Any<CopybookId>(), Arg.Any<CancellationToken>())
            .Returns(copybook);
        var versionRepo = CreateVersionRepo();
        var unitOfWork = CreateUnitOfWork();
        var handler = new ImportCopybookLayout.Handler(copybookRepo, versionRepo, unitOfWork);

        var command = new ImportCopybookLayout.Command(copybook.Id.Value, ValidCopybookText, "v1.0");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.VersionLabel.Should().Be("v1.0");
        result.Value.CopybookId.Should().Be(copybook.Id.Value);
        result.Value.FieldCount.Should().BeGreaterThan(0);
        result.Value.TotalLength.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddVersionToRepository()
    {
        var copybookRepo = CreateCopybookRepo();
        var copybook = CreateExistingCopybook();
        copybookRepo.GetByIdAsync(Arg.Any<CopybookId>(), Arg.Any<CancellationToken>())
            .Returns(copybook);
        var versionRepo = CreateVersionRepo();
        var unitOfWork = CreateUnitOfWork();
        var handler = new ImportCopybookLayout.Handler(copybookRepo, versionRepo, unitOfWork);

        var command = new ImportCopybookLayout.Command(copybook.Id.Value, ValidCopybookText, "v1.0");
        await handler.Handle(command, CancellationToken.None);

        versionRepo.Received(1).Add(Arg.Any<CopybookVersion>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCommitUnitOfWork()
    {
        var copybookRepo = CreateCopybookRepo();
        var copybook = CreateExistingCopybook();
        copybookRepo.GetByIdAsync(Arg.Any<CopybookId>(), Arg.Any<CancellationToken>())
            .Returns(copybook);
        var versionRepo = CreateVersionRepo();
        var unitOfWork = CreateUnitOfWork();
        var handler = new ImportCopybookLayout.Handler(copybookRepo, versionRepo, unitOfWork);

        var command = new ImportCopybookLayout.Command(copybook.Id.Value, ValidCopybookText, "v1.0");
        await handler.Handle(command, CancellationToken.None);

        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCopybookNotFound_ShouldReturnError()
    {
        var copybookRepo = CreateCopybookRepo();
        copybookRepo.GetByIdAsync(Arg.Any<CopybookId>(), Arg.Any<CancellationToken>())
            .Returns((Copybook?)null);
        var versionRepo = CreateVersionRepo();
        var unitOfWork = CreateUnitOfWork();
        var handler = new ImportCopybookLayout.Handler(copybookRepo, versionRepo, unitOfWork);

        var command = new ImportCopybookLayout.Command(Guid.NewGuid(), ValidCopybookText, "v1.0");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WhenCopybookNotFound_ShouldNotCommit()
    {
        var copybookRepo = CreateCopybookRepo();
        copybookRepo.GetByIdAsync(Arg.Any<CopybookId>(), Arg.Any<CancellationToken>())
            .Returns((Copybook?)null);
        var versionRepo = CreateVersionRepo();
        var unitOfWork = CreateUnitOfWork();
        var handler = new ImportCopybookLayout.Handler(copybookRepo, versionRepo, unitOfWork);

        var command = new ImportCopybookLayout.Command(Guid.NewGuid(), ValidCopybookText, "v1.0");
        await handler.Handle(command, CancellationToken.None);

        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidCopybookText_ShouldReturnParseError()
    {
        var copybookRepo = CreateCopybookRepo();
        var copybook = CreateExistingCopybook();
        copybookRepo.GetByIdAsync(Arg.Any<CopybookId>(), Arg.Any<CancellationToken>())
            .Returns(copybook);
        var versionRepo = CreateVersionRepo();
        var unitOfWork = CreateUnitOfWork();
        var handler = new ImportCopybookLayout.Handler(copybookRepo, versionRepo, unitOfWork);

        // Text with only whitespace content that CopybookParser will reject
        var command = new ImportCopybookLayout.Command(copybook.Id.Value, "    ", "v1.0");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ParseFailed");
    }
}
