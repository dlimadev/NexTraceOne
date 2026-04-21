using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CloseReleaseWindow;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IsChangeWindowOpen;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleaseWindows;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RegisterReleaseWindow;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave F.1 — Release Calendar.
/// Cobre: ReleaseCalendarEntry domain, RegisterReleaseWindow, CloseReleaseWindow,
/// ListReleaseWindows, IsChangeWindowOpen.
/// </summary>
public sealed class ReleaseCalendarTests
{
    private readonly IReleaseCalendarRepository _repo = Substitute.For<IReleaseCalendarRepository>();
    private readonly IChangeIntelligenceUnitOfWork _uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset Now = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);

    public ReleaseCalendarTests()
    {
        _clock.UtcNow.Returns(Now);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    // ── Domain entity tests ───────────────────────────────────────────────

    [Fact]
    public void ReleaseCalendarEntry_Register_WithValidWindow_Succeeds()
    {
        var result = ReleaseCalendarEntry.Register(
            "tenant-1", "Q4 Freeze", ReleaseWindowType.Freeze,
            Now, Now.AddDays(7));

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be("tenant-1");
        result.Value.Name.Should().Be("Q4 Freeze");
        result.Value.WindowType.Should().Be(ReleaseWindowType.Freeze);
        result.Value.Status.Should().Be(ReleaseWindowStatus.Active);
        result.Value.BlocksChanges.Should().BeTrue();
        result.Value.IsHotfixOnly.Should().BeFalse();
    }

    [Fact]
    public void ReleaseCalendarEntry_Register_WithEndsBeforeStarts_Fails()
    {
        var result = ReleaseCalendarEntry.Register(
            "tenant-1", "Bad Window", ReleaseWindowType.Scheduled,
            Now.AddDays(7), Now); // ends before starts

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("release_calendar.invalid_window");
    }

    [Fact]
    public void ReleaseCalendarEntry_Freeze_BlocksChanges_IsTrue()
    {
        var result = ReleaseCalendarEntry.Register(
            "t", "Freeze", ReleaseWindowType.Freeze, Now, Now.AddHours(24));
        result.Value.BlocksChanges.Should().BeTrue();
        result.Value.IsHotfixOnly.Should().BeFalse();
    }

    [Fact]
    public void ReleaseCalendarEntry_HotfixAllowed_IsHotfixOnly_IsTrue()
    {
        var result = ReleaseCalendarEntry.Register(
            "t", "Hotfix", ReleaseWindowType.HotfixAllowed, Now, Now.AddHours(4));
        result.Value.IsHotfixOnly.Should().BeTrue();
        result.Value.BlocksChanges.Should().BeFalse();
    }

    [Fact]
    public void ReleaseCalendarEntry_Scheduled_NeitherBlocksNorHotfix()
    {
        var result = ReleaseCalendarEntry.Register(
            "t", "Deploy", ReleaseWindowType.Scheduled, Now, Now.AddHours(2));
        result.Value.BlocksChanges.Should().BeFalse();
        result.Value.IsHotfixOnly.Should().BeFalse();
    }

    [Fact]
    public void ReleaseCalendarEntry_Close_SetsStatusClosed()
    {
        var entry = ReleaseCalendarEntry.Register(
            "tenant-1", "Freeze", ReleaseWindowType.Freeze, Now, Now.AddDays(7)).Value;

        var result = entry.Close("admin", Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(ReleaseWindowStatus.Closed);
        entry.ClosedByUserId.Should().Be("admin");
        entry.ClosedAt.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public void ReleaseCalendarEntry_CloseAlreadyClosed_Fails()
    {
        var entry = ReleaseCalendarEntry.Register(
            "t", "Freeze", ReleaseWindowType.Freeze, Now, Now.AddDays(7)).Value;
        entry.Close("admin", Now);

        var result = entry.Close("admin", Now.AddHours(1));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("release_calendar.already_closed");
    }

    [Fact]
    public void ReleaseCalendarEntry_Cancel_BeforeStart_Succeeds()
    {
        var future = Now.AddDays(10);
        var entry = ReleaseCalendarEntry.Register(
            "t", "Future Freeze", ReleaseWindowType.Freeze, future, future.AddDays(7)).Value;

        var result = entry.Cancel("admin", Now); // now < future

        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(ReleaseWindowStatus.Cancelled);
    }

    [Fact]
    public void ReleaseCalendarEntry_Cancel_AfterStart_Fails()
    {
        var past = Now.AddDays(-1);
        var entry = ReleaseCalendarEntry.Register(
            "t", "Past", ReleaseWindowType.Freeze, past, Now.AddDays(7)).Value;

        var result = entry.Cancel("admin", Now); // now >= past

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("release_calendar.window_already_started");
    }

    [Fact]
    public void ReleaseCalendarEntry_IsActiveAt_ReturnsTrueWhenInWindow()
    {
        var entry = ReleaseCalendarEntry.Register(
            "t", "W", ReleaseWindowType.Freeze, Now, Now.AddDays(7)).Value;

        entry.IsActiveAt(Now.AddHours(1)).Should().BeTrue();
    }

    [Fact]
    public void ReleaseCalendarEntry_IsActiveAt_ReturnsFalseOutsideWindow()
    {
        var entry = ReleaseCalendarEntry.Register(
            "t", "W", ReleaseWindowType.Freeze, Now, Now.AddDays(7)).Value;

        entry.IsActiveAt(Now.AddDays(8)).Should().BeFalse();
    }

    [Fact]
    public void ReleaseCalendarEntry_IsActiveAt_WithEnvFilter_MatchesCorrectEnv()
    {
        var result = ReleaseCalendarEntry.Register(
            "t", "Prod Freeze", ReleaseWindowType.Freeze, Now, Now.AddDays(7),
            environmentFilter: "production");
        var entry = result.Value;

        entry.IsActiveAt(Now.AddHours(1), "production").Should().BeTrue();
        entry.IsActiveAt(Now.AddHours(1), "staging").Should().BeFalse();
    }

    // ── Feature handler tests ─────────────────────────────────────────────

    [Fact]
    public async Task RegisterReleaseWindow_ValidCommand_AddsAndCommits()
    {
        var handler = new RegisterReleaseWindow.Handler(_repo, _uow);
        var cmd = new RegisterReleaseWindow.Command(
            "tenant-1", "Deploy Window", ReleaseWindowType.Scheduled,
            Now, Now.AddHours(4));

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Deploy Window");
        result.Value.WindowType.Should().Be(ReleaseWindowType.Scheduled);
        result.Value.BlocksChanges.Should().BeFalse();
        _repo.Received(1).Add(Arg.Any<ReleaseCalendarEntry>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterReleaseWindow_InvalidDates_ReturnsFailure()
    {
        var handler = new RegisterReleaseWindow.Handler(_repo, _uow);
        var cmd = new RegisterReleaseWindow.Command(
            "tenant-1", "Bad", ReleaseWindowType.Freeze,
            Now.AddDays(5), Now); // ends before starts

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CloseReleaseWindow_ValidWindow_Closes()
    {
        var entry = ReleaseCalendarEntry.Register(
            "tenant-1", "Freeze", ReleaseWindowType.Freeze, Now, Now.AddDays(7)).Value;
        _repo.GetByIdAsync(Arg.Any<ReleaseCalendarEntryId>(), Arg.Any<CancellationToken>())
            .Returns(entry);

        var handler = new CloseReleaseWindow.Handler(_repo, _uow, _clock);
        var cmd = new CloseReleaseWindow.Command(
            entry.Id.Value, "tenant-1", "admin", CloseReleaseWindow.CloseAction.Close);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReleaseWindowStatus.Closed);
    }

    [Fact]
    public async Task CloseReleaseWindow_NotFound_ReturnsNotFound()
    {
        _repo.GetByIdAsync(Arg.Any<ReleaseCalendarEntryId>(), Arg.Any<CancellationToken>())
            .Returns((ReleaseCalendarEntry?)null);

        var handler = new CloseReleaseWindow.Handler(_repo, _uow, _clock);
        var cmd = new CloseReleaseWindow.Command(Guid.NewGuid(), "tenant-1", "admin", CloseReleaseWindow.CloseAction.Close);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("release_calendar.not_found");
    }

    [Fact]
    public async Task ListReleaseWindows_ReturnsMappedDtos()
    {
        var entry = ReleaseCalendarEntry.Register(
            "t", "Q4 Freeze", ReleaseWindowType.Freeze, Now, Now.AddDays(7)).Value;
        _repo.ListAsync("t", null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ReleaseCalendarEntry> { entry });

        var handler = new ListReleaseWindows.Handler(_repo);
        var result = await handler.Handle(new ListReleaseWindows.Query("t"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Windows[0].Name.Should().Be("Q4 Freeze");
        result.Value.Windows[0].BlocksChanges.Should().BeTrue();
    }

    [Fact]
    public async Task IsChangeWindowOpen_WithFreezeWindow_ReturnsBlocked()
    {
        var freeze = ReleaseCalendarEntry.Register(
            "t", "Freeze", ReleaseWindowType.Freeze, Now.AddHours(-1), Now.AddDays(7)).Value;
        _repo.ListActiveAtAsync("t", Arg.Any<DateTimeOffset>(), "production", Arg.Any<CancellationToken>())
            .Returns(new List<ReleaseCalendarEntry> { freeze });

        var handler = new IsChangeWindowOpen.Handler(_repo, _clock);
        var result = await handler.Handle(
            new IsChangeWindowOpen.Query("t", "production"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOpen.Should().BeFalse();
        result.Value.BlockingWindows.Should().HaveCount(1);
        result.Value.BlockingWindows[0].WindowType.Should().Be(ReleaseWindowType.Freeze);
    }

    [Fact]
    public async Task IsChangeWindowOpen_NoWindows_ReturnsOpen()
    {
        _repo.ListActiveAtAsync("t", Arg.Any<DateTimeOffset>(), "production", Arg.Any<CancellationToken>())
            .Returns(new List<ReleaseCalendarEntry>());

        var handler = new IsChangeWindowOpen.Handler(_repo, _clock);
        var result = await handler.Handle(
            new IsChangeWindowOpen.Query("t", "production"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOpen.Should().BeTrue();
        result.Value.BlockingWindows.Should().BeEmpty();
    }

    [Fact]
    public async Task IsChangeWindowOpen_WithScheduledWindow_ReturnsOpen()
    {
        var scheduled = ReleaseCalendarEntry.Register(
            "t", "Deploy", ReleaseWindowType.Scheduled, Now.AddHours(-1), Now.AddHours(4)).Value;
        _repo.ListActiveAtAsync("t", Arg.Any<DateTimeOffset>(), "prod", Arg.Any<CancellationToken>())
            .Returns(new List<ReleaseCalendarEntry> { scheduled });

        var handler = new IsChangeWindowOpen.Handler(_repo, _clock);
        var result = await handler.Handle(
            new IsChangeWindowOpen.Query("t", "prod"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOpen.Should().BeTrue();
        result.Value.BlockingWindows.Should().BeEmpty();
    }
}
