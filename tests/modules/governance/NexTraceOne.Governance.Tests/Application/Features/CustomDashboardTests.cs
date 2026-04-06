using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.CreateCustomDashboard;
using NexTraceOne.Governance.Application.Features.GetCustomDashboard;
using NexTraceOne.Governance.Application.Features.ListCustomDashboards;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes unitários para as features de Custom Dashboards.
/// </summary>
public sealed class CustomDashboardTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public CustomDashboardTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── CreateCustomDashboard ─────────────────────────────────────────────

    [Fact]
    public async Task CreateCustomDashboard_ValidCommand_ReturnsValidResponse()
    {
        var handler = new CreateCustomDashboard.Handler(_clock);
        var command = new CreateCustomDashboard.Command(
            TenantId: "tenant-1",
            UserId: "user-1",
            Name: "My Dashboard",
            Description: "Test",
            Layout: "two-column",
            WidgetIds: ["dora-metrics", "incident-summary"],
            Persona: "Engineer");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DashboardId.Should().NotBe(Guid.Empty);
        result.Value.Name.Should().Be("My Dashboard");
        result.Value.Layout.Should().Be("two-column");
        result.Value.Persona.Should().Be("Engineer");
        result.Value.WidgetCount.Should().Be(2);
        result.Value.CreatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void CreateCustomDashboard_ValidatorRejectsEmptyName()
    {
        var validator = new CreateCustomDashboard.Validator();
        var command = new CreateCustomDashboard.Command(
            TenantId: "t",
            UserId: "u",
            Name: "",
            Description: null,
            Layout: "grid",
            WidgetIds: ["dora-metrics"],
            Persona: "Engineer");

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void CreateCustomDashboard_ValidatorRejectsTooManyWidgets()
    {
        var validator = new CreateCustomDashboard.Validator();
        var tooManyWidgets = Enumerable.Range(1, 21).Select(i => $"widget-{i}").ToList();

        var command = new CreateCustomDashboard.Command(
            TenantId: "t",
            UserId: "u",
            Name: "Dashboard",
            Description: null,
            Layout: "grid",
            WidgetIds: tooManyWidgets,
            Persona: "Engineer");

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    // ── ListCustomDashboards ──────────────────────────────────────────────

    [Fact]
    public async Task ListCustomDashboards_ReturnsThreeDemoDashboards()
    {
        var handler = new ListCustomDashboards.Handler(_clock);
        var query = new ListCustomDashboards.Query(TenantId: "tenant-1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public void ListCustomDashboards_ValidatorRejectsPageZero()
    {
        var validator = new ListCustomDashboards.Validator();
        var query = new ListCustomDashboards.Query(TenantId: "t", Page: 0);

        var result = validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    // ── GetCustomDashboard ────────────────────────────────────────────────

    [Fact]
    public async Task GetCustomDashboard_ReturnsDashboardWithSixWidgets()
    {
        var handler = new GetCustomDashboard.Handler(_clock);
        var dashboardId = Guid.NewGuid();
        var query = new GetCustomDashboard.Query(dashboardId, "tenant-1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DashboardId.Should().Be(dashboardId);
        result.Value.WidgetIds.Count.Should().Be(6);
        result.Value.Name.Should().NotBeNullOrEmpty();
        result.Value.IsShared.Should().BeTrue();
    }
}
