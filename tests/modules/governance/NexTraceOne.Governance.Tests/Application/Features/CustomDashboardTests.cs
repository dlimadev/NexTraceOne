using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.CreateCustomDashboard;
using NexTraceOne.Governance.Application.Features.GetCustomDashboard;
using NexTraceOne.Governance.Application.Features.ListCustomDashboards;
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
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public CustomDashboardTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
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
}
