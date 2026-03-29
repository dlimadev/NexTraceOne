using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterMainframeSystem;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Application.Handlers;

/// <summary>
/// Testes do handler RegisterMainframeSystem do sub-domínio Legacy Assets.
/// Cobre criação com sucesso, duplicado e commit do unit of work.
/// </summary>
public sealed class RegisterMainframeSystemHandlerTests
{
    private static IMainframeSystemRepository CreateRepository() =>
        Substitute.For<IMainframeSystemRepository>();

    private static ILegacyAssetsUnitOfWork CreateUnitOfWork() =>
        Substitute.For<ILegacyAssetsUnitOfWork>();

    private static RegisterMainframeSystem.Command CreateValidCommand() =>
        new("PRD-SYS-01", "Banking", "Platform-Team", "SYSPLEX1", "LPAR01", "CICSPRD1");

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAndReturn()
    {
        var repo = CreateRepository();
        repo.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((MainframeSystem?)null);
        var unitOfWork = CreateUnitOfWork();
        var handler = new RegisterMainframeSystem.Handler(repo, unitOfWork);

        var result = await handler.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("PRD-SYS-01");
        result.Value.Domain.Should().Be("Banking");
        result.Value.TeamName.Should().Be("Platform-Team");
        result.Value.SysplexName.Should().Be("SYSPLEX1");
        result.Value.LparName.Should().Be("LPAR01");
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldReturnError()
    {
        var repo = CreateRepository();
        var existingSystem = MainframeSystem.Create(
            "PRD-SYS-01", "Banking", "Platform-Team",
            LparReference.Create("SYS", "LPAR"));
        repo.GetByNameAsync("PRD-SYS-01", Arg.Any<CancellationToken>())
            .Returns(existingSystem);
        var unitOfWork = CreateUnitOfWork();
        var handler = new RegisterMainframeSystem.Handler(repo, unitOfWork);

        var result = await handler.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyExists");
    }

    [Fact]
    public async Task Handler_ShouldCallAdd()
    {
        var repo = CreateRepository();
        repo.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((MainframeSystem?)null);
        var unitOfWork = CreateUnitOfWork();
        var handler = new RegisterMainframeSystem.Handler(repo, unitOfWork);

        await handler.Handle(CreateValidCommand(), CancellationToken.None);

        repo.Received(1).Add(Arg.Any<MainframeSystem>());
    }

    [Fact]
    public async Task Handler_ShouldCommitUnitOfWork()
    {
        var repo = CreateRepository();
        repo.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((MainframeSystem?)null);
        var unitOfWork = CreateUnitOfWork();
        var handler = new RegisterMainframeSystem.Handler(repo, unitOfWork);

        await handler.Handle(CreateValidCommand(), CancellationToken.None);

        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicate_ShouldNotCommit()
    {
        var repo = CreateRepository();
        var existingSystem = MainframeSystem.Create(
            "PRD-SYS-01", "Banking", "Platform-Team",
            LparReference.Create("SYS", "LPAR"));
        repo.GetByNameAsync("PRD-SYS-01", Arg.Any<CancellationToken>())
            .Returns(existingSystem);
        var unitOfWork = CreateUnitOfWork();
        var handler = new RegisterMainframeSystem.Handler(repo, unitOfWork);

        await handler.Handle(CreateValidCommand(), CancellationToken.None);

        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
