using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateScheduledReport.CreateScheduledReport;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteScheduledReport.DeleteScheduledReport;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListScheduledReports.ListScheduledReports;
using ToggleFeature = NexTraceOne.Configuration.Application.Features.ToggleScheduledReport.ToggleScheduledReport;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de CreateScheduledReport, DeleteScheduledReport, ListScheduledReports e ToggleScheduledReport —
/// gestão de relatórios programados por utilizador.
/// </summary>
public sealed class ScheduledReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static ICurrentUser CreateAuthenticatedUser(string id = "user-123")
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.Id.Returns(id);
        user.Name.Returns("Test User");
        user.Email.Returns($"{id}@test.com");
        return user;
    }

    private static ICurrentUser CreateAnonymousUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(false);
        return user;
    }

    private static ICurrentTenant CreateTenant(string id = "00000000-0000-0000-0000-000000000001")
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(Guid.Parse(id));
        return tenant;
    }

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ── CreateScheduledReport ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateScheduledReport_Should_Create_When_Authenticated()
    {
        var repo = Substitute.For<IScheduledReportRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("Weekly Summary", "service-health", "{\"env\":\"production\"}", "weekly", "[\"admin@test.com\"]", "pdf"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Weekly Summary");
        result.Value.Schedule.Should().Be("weekly");
        result.Value.Format.Should().Be("pdf");
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(FixedNow);
        await repo.Received(1).AddAsync(Arg.Any<ScheduledReport>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateScheduledReport_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IScheduledReportRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("Report", "service-health", "{}", "daily", "[]", "csv"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── DeleteScheduledReport ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteScheduledReport_Should_Delete_When_Owner()
    {
        var repo = Substitute.For<IScheduledReportRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var report = ScheduledReport.Create("00000000-0000-0000-0000-000000000001", "user-123", "My Report", "service-health", "{}", "weekly", "[]", "pdf", FixedNow);
        repo.GetByIdAsync(Arg.Any<ScheduledReportId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(report);

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(report.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReportId.Should().Be(report.Id.Value);
        await repo.Received(1).DeleteAsync(Arg.Any<ScheduledReportId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteScheduledReport_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IScheduledReportRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        repo.GetByIdAsync(Arg.Any<ScheduledReportId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ScheduledReport?)null);

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task DeleteScheduledReport_Should_Fail_When_Not_Owner()
    {
        var repo = Substitute.For<IScheduledReportRepository>();
        var currentUser = CreateAuthenticatedUser("other-user");
        var currentTenant = CreateTenant();

        var report = ScheduledReport.Create("00000000-0000-0000-0000-000000000001", "user-123", "My Report", "service-health", "{}", "weekly", "[]", "pdf", FixedNow);
        repo.GetByIdAsync(Arg.Any<ScheduledReportId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(report);

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(report.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Forbidden");
    }

    // ── ListScheduledReports ──────────────────────────────────────────────────

    [Fact]
    public async Task ListScheduledReports_Should_Return_Reports()
    {
        var repo = Substitute.For<IScheduledReportRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var reports = new List<ScheduledReport>
        {
            ScheduledReport.Create("00000000-0000-0000-0000-000000000001", "user-123", "Report A", "service-health", "{}", "daily", "[]", "pdf", FixedNow),
            ScheduledReport.Create("00000000-0000-0000-0000-000000000001", "user-123", "Report B", "change-summary", "{}", "weekly", "[]", "csv", FixedNow),
        };
        repo.ListByTenantAsync(Arg.Any<string>(), "user-123", Arg.Any<CancellationToken>())
            .Returns(reports);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListScheduledReports_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IScheduledReportRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── ToggleScheduledReport ─────────────────────────────────────────────────

    [Fact]
    public async Task ToggleScheduledReport_Should_Toggle_When_Owner()
    {
        var repo = Substitute.For<IScheduledReportRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var report = ScheduledReport.Create("00000000-0000-0000-0000-000000000001", "user-123", "My Report", "service-health", "{}", "weekly", "[]", "pdf", FixedNow);
        repo.GetByIdAsync(Arg.Any<ScheduledReportId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(report);

        var sut = new ToggleFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new ToggleFeature.Command(report.Id.Value, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        report.IsEnabled.Should().BeFalse();
        await repo.Received(1).UpdateAsync(Arg.Any<ScheduledReport>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleScheduledReport_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IScheduledReportRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        repo.GetByIdAsync(Arg.Any<ScheduledReportId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ScheduledReport?)null);

        var sut = new ToggleFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new ToggleFeature.Command(Guid.NewGuid(), true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
