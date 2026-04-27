using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.CreateCustomDashboard;
using NexTraceOne.Governance.Application.Features.DeleteCustomDashboard;
using NexTraceOne.Governance.Application.Features.GetCustomDashboard;
using NexTraceOne.Governance.Application.Features.GetDashboardRenderData;
using NexTraceOne.Governance.Application.Features.ListCustomDashboards;
using NexTraceOne.Governance.Application.Features.UpdateCustomDashboard;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes unitários para as features de Custom Dashboards.
/// Os handlers agora usam repositórios reais — testes usam mocks via NSubstitute.
/// </summary>
public sealed class CustomDashboardTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ICustomDashboardRepository _repository = Substitute.For<ICustomDashboardRepository>();
    private readonly IDashboardRevisionRepository _revisionRepository = Substitute.For<IDashboardRevisionRepository>();
    private readonly IGovernanceUnitOfWork _unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

    public CustomDashboardTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _revisionRepository.AddAsync(Arg.Any<DashboardRevision>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static List<CreateCustomDashboard.WidgetInput> TwoWidgets() =>
    [
        new("dora-metrics", 0, 0, 2, 2),
        new("incident-summary", 2, 0, 2, 2)
    ];

    private static CustomDashboard MakeDashboard(string tenantId = "tenant-1")
    {
        var widgets = TwoWidgets().Select(w => new DashboardWidget(
            Guid.NewGuid().ToString(),
            w.Type,
            new WidgetPosition(w.PosX, w.PosY, w.Width, w.Height),
            new WidgetConfig())).ToList();

        return CustomDashboard.Create(
            "My Dashboard", "Test", "two-column",
            "Engineer", widgets, tenantId, "user-1", FixedNow);
    }

    // ── CreateCustomDashboard ─────────────────────────────────────────────

    [Fact]
    public async Task CreateCustomDashboard_ValidCommand_ReturnsValidResponse()
    {
        var handler = new CreateCustomDashboard.Handler(_repository, _unitOfWork, _clock);
        var command = new CreateCustomDashboard.Command(
            TenantId: "tenant-1",
            UserId: "user-1",
            Name: "My Dashboard",
            Description: "Test",
            Layout: "two-column",
            Widgets: TwoWidgets(),
            Persona: "Engineer");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DashboardId.Should().NotBe(Guid.Empty);
        result.Value.Name.Should().Be("My Dashboard");
        result.Value.Layout.Should().Be("two-column");
        result.Value.Persona.Should().Be("Engineer");
        result.Value.WidgetCount.Should().Be(2);
        result.Value.CreatedAt.Should().Be(FixedNow);

        await _repository.Received(1).AddAsync(Arg.Any<CustomDashboard>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
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
            Widgets: TwoWidgets(),
            Persona: "Engineer");

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void CreateCustomDashboard_ValidatorRejectsTooManyWidgets()
    {
        var validator = new CreateCustomDashboard.Validator();
        var tooManyWidgets = Enumerable.Range(1, 21)
            .Select(i => new CreateCustomDashboard.WidgetInput($"widget-{i}", 0, i, 2, 2))
            .ToList();

        var command = new CreateCustomDashboard.Command(
            TenantId: "t",
            UserId: "u",
            Name: "Dashboard",
            Description: null,
            Layout: "grid",
            Widgets: tooManyWidgets,
            Persona: "Engineer");

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    // ── ListCustomDashboards ──────────────────────────────────────────────

    [Fact]
    public async Task ListCustomDashboards_EmptyRepository_ReturnsEmptyList()
    {
        _repository.ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CustomDashboard>>([]));

        var handler = new ListCustomDashboards.Handler(_repository);
        var query = new ListCustomDashboards.Query(TenantId: "tenant-1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(0);
        result.Value.TotalCount.Should().Be(0);
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
    public async Task GetCustomDashboard_NotFound_ReturnsNotFoundError()
    {
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(null));

        var handler = new GetCustomDashboard.Handler(_repository);
        var dashboardId = Guid.NewGuid();
        var query = new GetCustomDashboard.Query(dashboardId, "tenant-1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.NotFound");
    }

    [Fact]
    public async Task GetCustomDashboard_Found_ReturnsWidgetsWithPosition()
    {
        var dashboard = MakeDashboard();
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new GetCustomDashboard.Handler(_repository);
        var query = new GetCustomDashboard.Query(dashboard.Id.Value, "tenant-1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Widgets.Should().HaveCount(2);
        result.Value.Widgets[0].Type.Should().Be("dora-metrics");
        result.Value.Widgets[0].PosX.Should().Be(0);
        result.Value.WidgetCount.Should().Be(2);
    }

    // ── UpdateCustomDashboard ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateCustomDashboard_ValidCommand_UpdatesWidgets()
    {
        var dashboard = MakeDashboard();
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new UpdateCustomDashboard.Handler(_repository, _revisionRepository, _unitOfWork, _clock);
        var newWidgets = new List<UpdateCustomDashboard.WidgetInput>
        {
            new(null, "service-scorecard", 0, 0, 3, 3, ServiceId: "svc-1")
        };

        var command = new UpdateCustomDashboard.Command(
            DashboardId: dashboard.Id.Value,
            TenantId: "tenant-1",
            UserId: "user-1",
            Name: "Updated",
            Description: "Updated desc",
            Layout: "grid",
            Widgets: newWidgets);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dashboard.Name.Should().Be("Updated");
        dashboard.Layout.Should().Be("grid");
        dashboard.WidgetCount.Should().Be(1);
        dashboard.Widgets[0].Type.Should().Be("service-scorecard");
        dashboard.Widgets[0].Config.ServiceId.Should().Be("svc-1");

        await _repository.Received(1).UpdateAsync(dashboard, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCustomDashboard_NotFound_ReturnsError()
    {
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(null));

        var handler = new UpdateCustomDashboard.Handler(_repository, _revisionRepository, _unitOfWork, _clock);
        var command = new UpdateCustomDashboard.Command(
            DashboardId: Guid.NewGuid(),
            TenantId: "tenant-1",
            UserId: "user-1",
            Name: "X",
            Description: null,
            Layout: "grid",
            Widgets: [new(null, "dora-metrics", 0, 0, 2, 2)]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.NotFound");
    }

    [Fact]
    public async Task UpdateCustomDashboard_WrongTenant_ReturnsForbidden()
    {
        var dashboard = MakeDashboard("tenant-A");
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new UpdateCustomDashboard.Handler(_repository, _revisionRepository, _unitOfWork, _clock);
        var command = new UpdateCustomDashboard.Command(
            DashboardId: dashboard.Id.Value,
            TenantId: "tenant-B",
            UserId: "user-1",
            Name: "X",
            Description: null,
            Layout: "grid",
            Widgets: [new(null, "dora-metrics", 0, 0, 2, 2)]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.Forbidden");
    }

    // ── DeleteCustomDashboard ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteCustomDashboard_ValidCommand_DeletesDashboard()
    {
        var dashboard = MakeDashboard();
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new DeleteCustomDashboard.Handler(_repository, _unitOfWork);
        var command = new DeleteCustomDashboard.Command(dashboard.Id.Value, "tenant-1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).DeleteAsync(dashboard, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCustomDashboard_NotFound_ReturnsError()
    {
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(null));

        var handler = new DeleteCustomDashboard.Handler(_repository, _unitOfWork);
        var command = new DeleteCustomDashboard.Command(Guid.NewGuid(), "tenant-1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.NotFound");
    }

    [Fact]
    public async Task DeleteCustomDashboard_SystemDashboard_ReturnsError()
    {
        var widgets = new List<DashboardWidget>
        {
            new(Guid.NewGuid().ToString(), "dora-metrics",
                new WidgetPosition(0, 0, 2, 2), new WidgetConfig())
        };

        var dashboard = CustomDashboard.Create(
            "System Dashboard", null, "grid", "Executive",
            widgets, "tenant-1", "admin", FixedNow, isSystem: true);

        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new DeleteCustomDashboard.Handler(_repository, _unitOfWork);
        var command = new DeleteCustomDashboard.Command(dashboard.Id.Value, "tenant-1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.SystemDashboardReadOnly");
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<CustomDashboard>(), Arg.Any<CancellationToken>());
    }

    // ── GetDashboardRenderData ────────────────────────────────────────────

    [Fact]
    public async Task GetDashboardRenderData_Found_ReturnsWidgetSlots()
    {
        var dashboard = MakeDashboard();
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new GetDashboardRenderData.Handler(_repository);
        var query = new GetDashboardRenderData.Query(
            DashboardId: dashboard.Id.Value,
            TenantId: "tenant-1",
            EnvironmentId: "env-prod",
            GlobalTimeRange: "7d");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Widgets.Should().HaveCount(2);
        result.Value.GlobalTimeRange.Should().Be("7d");
        result.Value.EnvironmentId.Should().Be("env-prod");
        result.Value.Widgets[0].Type.Should().Be("dora-metrics");
        result.Value.Widgets[0].EffectiveTimeRange.Should().Be("7d");
    }

    [Fact]
    public async Task GetDashboardRenderData_DefaultsTo24h_WhenNoGlobalRange()
    {
        var dashboard = MakeDashboard();
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new GetDashboardRenderData.Handler(_repository);
        var query = new GetDashboardRenderData.Query(
            DashboardId: dashboard.Id.Value,
            TenantId: "tenant-1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GlobalTimeRange.Should().Be("24h");
    }

    [Fact]
    public async Task GetDashboardRenderData_WrongTenant_ReturnsForbidden()
    {
        var dashboard = MakeDashboard("tenant-A");
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new GetDashboardRenderData.Handler(_repository);
        var query = new GetDashboardRenderData.Query(
            DashboardId: dashboard.Id.Value,
            TenantId: "tenant-B");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.Forbidden");
    }
}
