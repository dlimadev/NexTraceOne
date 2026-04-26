using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetDashboardHistory;
using NexTraceOne.Governance.Application.Features.RevertDashboard;
using NexTraceOne.Governance.Application.Features.ShareDashboard;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests;

/// <summary>
/// Unit tests for Wave V3.1 — Dashboard Intelligence Foundation.
/// Covers: DashboardRevision, SharingPolicy, CustomDashboard new behaviors,
/// GetDashboardHistory, RevertDashboard, ShareDashboard.
/// </summary>
public sealed class V31_DashboardIntelligenceFoundationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);

    // ── DashboardRevision.Create ────────────────────────────────────────────

    [Fact]
    public void DashboardRevision_Create_WithValidData_ShouldReturnRevision()
    {
        var dashboardId = new CustomDashboardId(Guid.NewGuid());
        var revision = DashboardRevision.Create(
            dashboardId, 1, "My Dashboard", "desc", "grid",
            """[{"id":"w1"}]""", "[]", "user1", "tenant1", FixedNow, "initial");

        revision.DashboardId.Should().Be(dashboardId);
        revision.RevisionNumber.Should().Be(1);
        revision.Name.Should().Be("My Dashboard");
        revision.Layout.Should().Be("grid");
        revision.AuthorUserId.Should().Be("user1");
        revision.TenantId.Should().Be("tenant1");
        revision.ChangeNote.Should().Be("initial");
        revision.CreatedAt.Should().Be(FixedNow);
        revision.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void DashboardRevision_Create_WithNullDashboardId_ShouldThrow()
    {
        Action act = () => DashboardRevision.Create(
            null!, 1, "Name", null, "grid", "[]", "[]", "user1", "tenant1", FixedNow);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void DashboardRevision_Create_WithZeroRevisionNumber_ShouldThrow()
    {
        var dashboardId = new CustomDashboardId(Guid.NewGuid());
        Action act = () => DashboardRevision.Create(
            dashboardId, 0, "Name", null, "grid", "[]", "[]", "user1", "tenant1", FixedNow);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void DashboardRevision_Create_WithEmptyName_ShouldThrow()
    {
        var dashboardId = new CustomDashboardId(Guid.NewGuid());
        Action act = () => DashboardRevision.Create(
            dashboardId, 1, "  ", null, "grid", "[]", "[]", "user1", "tenant1", FixedNow);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void DashboardRevision_Create_WithEmptyAuthorUserId_ShouldThrow()
    {
        var dashboardId = new CustomDashboardId(Guid.NewGuid());
        Action act = () => DashboardRevision.Create(
            dashboardId, 1, "Name", null, "grid", "[]", "[]", "", "tenant1", FixedNow);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void DashboardRevision_Create_TrimsChangeNote()
    {
        var dashboardId = new CustomDashboardId(Guid.NewGuid());
        var revision = DashboardRevision.Create(
            dashboardId, 1, "Name", null, "grid", "[]", "[]", "user1", "tenant1", FixedNow,
            changeNote: "  fix typo  ");

        revision.ChangeNote.Should().Be("fix typo");
    }

    // ── SharingPolicy ───────────────────────────────────────────────────────

    [Fact]
    public void SharingPolicy_Private_ShouldHavePrivateScope()
    {
        var policy = SharingPolicy.Private;

        policy.Scope.Should().Be(DashboardSharingScope.Private);
        policy.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void SharingPolicy_FromLegacyIsShared_True_ShouldReturnTenantRead()
    {
        var policy = SharingPolicy.FromLegacyIsShared(true);

        policy.Scope.Should().Be(DashboardSharingScope.Tenant);
        policy.Permission.Should().Be(DashboardSharingPermission.Read);
        policy.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void SharingPolicy_FromLegacyIsShared_False_ShouldReturnPrivate()
    {
        var policy = SharingPolicy.FromLegacyIsShared(false);

        policy.Scope.Should().Be(DashboardSharingScope.Private);
        policy.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void SharingPolicy_HasActivePublicLink_WithNoExpiry_ShouldBeActive()
    {
        var policy = new SharingPolicy(DashboardSharingScope.PublicLink, DashboardSharingPermission.Read, null);

        policy.HasActivePublicLink(FixedNow).Should().BeTrue();
    }

    [Fact]
    public void SharingPolicy_HasActivePublicLink_WithFutureExpiry_ShouldBeActive()
    {
        var policy = new SharingPolicy(
            DashboardSharingScope.PublicLink,
            DashboardSharingPermission.Read,
            FixedNow.AddDays(7));

        policy.HasActivePublicLink(FixedNow).Should().BeTrue();
    }

    [Fact]
    public void SharingPolicy_HasActivePublicLink_WithPastExpiry_ShouldBeInactive()
    {
        var policy = new SharingPolicy(
            DashboardSharingScope.PublicLink,
            DashboardSharingPermission.Read,
            FixedNow.AddDays(-1));

        policy.HasActivePublicLink(FixedNow).Should().BeFalse();
    }

    [Fact]
    public void SharingPolicy_HasActivePublicLink_WhenScopeIsNotPublicLink_ShouldBeFalse()
    {
        var policy = new SharingPolicy(DashboardSharingScope.Tenant, DashboardSharingPermission.Read);

        policy.HasActivePublicLink(FixedNow).Should().BeFalse();
    }

    // ── CustomDashboard V3.1 behaviors ─────────────────────────────────────

    private static CustomDashboard CreateTestDashboard(bool isSystem = false)
        => CustomDashboard.Create(
            "Test Dashboard", "desc", "grid", "Engineer",
            [new DashboardWidget(Guid.NewGuid().ToString(), "w1", new WidgetPosition(0, 0, 2, 2), new WidgetConfig())],
            "tenant1", "user1", FixedNow, isSystem: isSystem);

    [Fact]
    public void CustomDashboard_Create_ShouldHavePrivateSharingByDefault()
    {
        var dashboard = CreateTestDashboard();

        dashboard.SharingPolicy.Scope.Should().Be(DashboardSharingScope.Private);
        dashboard.IsShared.Should().BeFalse();
        dashboard.CurrentRevisionNumber.Should().Be(0);
    }

    [Fact]
    public void CustomDashboard_Update_ShouldIncrementRevisionNumber()
    {
        var dashboard = CreateTestDashboard();
        dashboard.Update("Updated", null, "grid", dashboard.Widgets, null, FixedNow.AddHours(1));

        dashboard.CurrentRevisionNumber.Should().Be(1);
    }

    [Fact]
    public void CustomDashboard_SetSharingPolicy_ShouldUpdatePolicyAndIsShared()
    {
        var dashboard = CreateTestDashboard();
        var policy = new SharingPolicy(DashboardSharingScope.Team, DashboardSharingPermission.Read);

        dashboard.SetSharingPolicy(policy, FixedNow);

        dashboard.SharingPolicy.Scope.Should().Be(DashboardSharingScope.Team);
        dashboard.IsShared.Should().BeTrue();
    }

    [Fact]
    public void CustomDashboard_SetShared_True_ShouldUseLegacyTenantRead()
    {
        var dashboard = CreateTestDashboard();
        dashboard.SetShared(true, FixedNow);

        dashboard.SharingPolicy.Scope.Should().Be(DashboardSharingScope.Tenant);
        dashboard.SharingPolicy.Permission.Should().Be(DashboardSharingPermission.Read);
        dashboard.IsShared.Should().BeTrue();
    }

    [Fact]
    public void CustomDashboard_SetShared_False_ShouldMakeDashboardPrivate()
    {
        var dashboard = CreateTestDashboard();
        dashboard.SetShared(true, FixedNow);
        dashboard.SetShared(false, FixedNow.AddHours(1));

        dashboard.IsShared.Should().BeFalse();
        dashboard.SharingPolicy.Scope.Should().Be(DashboardSharingScope.Private);
    }

    [Fact]
    public void CustomDashboard_SetVariables_ShouldStoreVariables()
    {
        var dashboard = CreateTestDashboard();
        var variables = new List<DashboardVariable>
        {
            new("$service", "Service", DashboardVariableType.Service, null, DashboardVariableSource.Catalog)
        };

        dashboard.SetVariables(variables, FixedNow);

        dashboard.Variables.Should().HaveCount(1);
        dashboard.Variables[0].Key.Should().Be("$service");
    }

    [Fact]
    public void CustomDashboard_CreateRevisionSnapshot_ShouldCaptureDashboardState()
    {
        var dashboard = CreateTestDashboard();
        dashboard.Update("Updated Name", null, "grid", dashboard.Widgets, null, FixedNow);

        var revision = dashboard.CreateRevisionSnapshot("""[{"id":"w1"}]""", "[]", "user1", FixedNow, "test commit");

        revision.RevisionNumber.Should().Be(1);
        revision.Name.Should().Be("Updated Name");
        revision.AuthorUserId.Should().Be("user1");
        revision.ChangeNote.Should().Be("test commit");
        revision.DashboardId.Should().Be(dashboard.Id);
        revision.TenantId.Should().Be("tenant1");
    }

    // ── GetDashboardHistory.Handler ─────────────────────────────────────────

    private static DashboardRevision MakeRevision(CustomDashboardId dashboardId, int number)
        => DashboardRevision.Create(
            dashboardId, number, "Rev " + number, null, "grid",
            """[{"id":"w1"}]""", "[]", "user1", "tenant1", FixedNow.AddMinutes(-number));

    [Fact]
    public async Task GetDashboardHistory_Handle_DashboardFound_ShouldReturnRevisions()
    {
        var dashboard = CreateTestDashboard();
        var revRepository = Substitute.For<IDashboardRevisionRepository>();
        var dashRepository = Substitute.For<ICustomDashboardRepository>();

        dashRepository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var revisions = new List<DashboardRevision>
        {
            MakeRevision(dashboard.Id, 2),
            MakeRevision(dashboard.Id, 1)
        };
        revRepository.CountByDashboardIdAsync(dashboard.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(2));
        revRepository.ListByDashboardIdAsync(dashboard.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<DashboardRevision>>(revisions));

        var handler = new GetDashboardHistory.Handler(dashRepository, revRepository);
        var query = new GetDashboardHistory.Query(dashboard.Id.Value, "tenant1", 20);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRevisions.Should().Be(2);
        result.Value.Revisions.Should().HaveCount(2);
        result.Value.Revisions[0].WidgetCount.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardHistory_Handle_DashboardNotFound_ShouldReturnNotFound()
    {
        var revRepository = Substitute.For<IDashboardRevisionRepository>();
        var dashRepository = Substitute.For<ICustomDashboardRepository>();

        dashRepository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(null));

        var handler = new GetDashboardHistory.Handler(dashRepository, revRepository);
        var query = new GetDashboardHistory.Query(Guid.NewGuid(), "tenant1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.NotFound");
    }

    [Fact]
    public async Task GetDashboardHistory_Handle_WrongTenant_ShouldReturnForbidden()
    {
        var dashboard = CreateTestDashboard();
        var revRepository = Substitute.For<IDashboardRevisionRepository>();
        var dashRepository = Substitute.For<ICustomDashboardRepository>();

        dashRepository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new GetDashboardHistory.Handler(dashRepository, revRepository);
        var query = new GetDashboardHistory.Query(dashboard.Id.Value, "other-tenant");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.Forbidden");
    }

    // ── RevertDashboard.Handler ─────────────────────────────────────────────

    [Fact]
    public async Task RevertDashboard_Handle_ValidCommand_ShouldRevertAndCreateRevisions()
    {
        var dashboard = CreateTestDashboard();
        dashboard.Update("v2", null, "grid", dashboard.Widgets, null, FixedNow);

        var dashRepository = Substitute.For<ICustomDashboardRepository>();
        var revRepository = Substitute.For<IDashboardRevisionRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        clock.UtcNow.Returns(FixedNow);
        unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        dashRepository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var targetRev = MakeRevision(dashboard.Id, 1);
        revRepository.GetByRevisionNumberAsync(dashboard.Id, 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DashboardRevision?>(targetRev));

        var handler = new RevertDashboard.Handler(dashRepository, revRepository, unitOfWork, clock);
        var command = new RevertDashboard.Command(dashboard.Id.Value, "tenant1", "user1", 1, "Reverting to v1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RevertedFromRevision.Should().Be(1);
        await revRepository.Received(2).AddAsync(Arg.Any<DashboardRevision>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevertDashboard_Handle_DashboardNotFound_ShouldReturnNotFound()
    {
        var dashRepository = Substitute.For<ICustomDashboardRepository>();
        var revRepository = Substitute.For<IDashboardRevisionRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        dashRepository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(null));

        var handler = new RevertDashboard.Handler(dashRepository, revRepository, unitOfWork, clock);
        var command = new RevertDashboard.Command(Guid.NewGuid(), "tenant1", "user1", 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.NotFound");
    }

    [Fact]
    public async Task RevertDashboard_Handle_WrongTenant_ShouldReturnForbidden()
    {
        var dashboard = CreateTestDashboard();
        var dashRepository = Substitute.For<ICustomDashboardRepository>();
        var revRepository = Substitute.For<IDashboardRevisionRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        dashRepository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new RevertDashboard.Handler(dashRepository, revRepository, unitOfWork, clock);
        var command = new RevertDashboard.Command(dashboard.Id.Value, "other-tenant", "user1", 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.Forbidden");
    }

    [Fact]
    public async Task RevertDashboard_Handle_SystemDashboard_ShouldReturnBusinessError()
    {
        var dashboard = CreateTestDashboard(isSystem: true);
        var dashRepository = Substitute.For<ICustomDashboardRepository>();
        var revRepository = Substitute.For<IDashboardRevisionRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        dashRepository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new RevertDashboard.Handler(dashRepository, revRepository, unitOfWork, clock);
        var command = new RevertDashboard.Command(dashboard.Id.Value, "tenant1", "user1", 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.SystemDashboardReadOnly");
    }

    [Fact]
    public async Task RevertDashboard_Handle_RevisionNotFound_ShouldReturnNotFound()
    {
        var dashboard = CreateTestDashboard();
        var dashRepository = Substitute.For<ICustomDashboardRepository>();
        var revRepository = Substitute.For<IDashboardRevisionRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        dashRepository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));
        revRepository.GetByRevisionNumberAsync(dashboard.Id, 99, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DashboardRevision?>(null));

        var handler = new RevertDashboard.Handler(dashRepository, revRepository, unitOfWork, clock);
        var command = new RevertDashboard.Command(dashboard.Id.Value, "tenant1", "user1", 99);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DashboardRevision.NotFound");
    }

    // ── ShareDashboard.Handler ──────────────────────────────────────────────

    [Fact]
    public async Task ShareDashboard_Handle_ValidTeamScope_ShouldApplyPolicy()
    {
        var dashboard = CreateTestDashboard();
        var repository = Substitute.For<ICustomDashboardRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        clock.UtcNow.Returns(FixedNow);
        unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new ShareDashboard.Handler(repository, unitOfWork, clock);
        var command = new ShareDashboard.Command(
            dashboard.Id.Value, "tenant1", "user1",
            DashboardSharingScope.Team, DashboardSharingPermission.Read);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Scope.Should().Be(DashboardSharingScope.Team);
        result.Value.IsVisible.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShareDashboard_Handle_DashboardNotFound_ShouldReturnNotFound()
    {
        var repository = Substitute.For<ICustomDashboardRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(null));

        var handler = new ShareDashboard.Handler(repository, unitOfWork, clock);
        var command = new ShareDashboard.Command(
            Guid.NewGuid(), "tenant1", "user1",
            DashboardSharingScope.Tenant, DashboardSharingPermission.Read);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.NotFound");
    }

    [Fact]
    public async Task ShareDashboard_Handle_WrongTenant_ShouldReturnForbidden()
    {
        var dashboard = CreateTestDashboard();
        var repository = Substitute.For<ICustomDashboardRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new ShareDashboard.Handler(repository, unitOfWork, clock);
        var command = new ShareDashboard.Command(
            dashboard.Id.Value, "other-tenant", "user1",
            DashboardSharingScope.Tenant, DashboardSharingPermission.Read);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.Forbidden");
    }

    [Fact]
    public async Task ShareDashboard_Handle_SystemDashboardPublicLink_ShouldReturnBusinessError()
    {
        var dashboard = CreateTestDashboard(isSystem: true);
        var repository = Substitute.For<ICustomDashboardRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new ShareDashboard.Handler(repository, unitOfWork, clock);
        var command = new ShareDashboard.Command(
            dashboard.Id.Value, "tenant1", "user1",
            DashboardSharingScope.PublicLink, DashboardSharingPermission.Read);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.SystemDashboardPublicLinkNotAllowed");
    }

    [Fact]
    public async Task ShareDashboard_Handle_Private_ShouldMakeDashboardNotVisible()
    {
        var dashboard = CreateTestDashboard();
        dashboard.SetShared(true, FixedNow.AddDays(-1));

        var repository = Substitute.For<ICustomDashboardRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        clock.UtcNow.Returns(FixedNow);
        unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(dashboard));

        var handler = new ShareDashboard.Handler(repository, unitOfWork, clock);
        var command = new ShareDashboard.Command(
            dashboard.Id.Value, "tenant1", "user1",
            DashboardSharingScope.Private, DashboardSharingPermission.Read);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsVisible.Should().BeFalse();
    }

    // ── Validators ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboardHistoryValidator_MaxResultsOutOfRange_ShouldFail()
    {
        var validator = new GetDashboardHistory.Validator();
        var query = new GetDashboardHistory.Query(Guid.NewGuid(), "tenant1", MaxResults: 0);

        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(query.MaxResults));
    }

    [Fact]
    public async Task GetDashboardHistoryValidator_ValidQuery_ShouldPass()
    {
        var validator = new GetDashboardHistory.Validator();
        var query = new GetDashboardHistory.Query(Guid.NewGuid(), "tenant1", MaxResults: 50);

        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RevertDashboardValidator_ZeroRevisionNumber_ShouldFail()
    {
        var validator = new RevertDashboard.Validator();
        var command = new RevertDashboard.Command(Guid.NewGuid(), "tenant1", "user1", 0);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.TargetRevisionNumber));
    }

    [Fact]
    public async Task RevertDashboardValidator_EmptyUserId_ShouldFail()
    {
        var validator = new RevertDashboard.Validator();
        var command = new RevertDashboard.Command(Guid.NewGuid(), "tenant1", "", 1);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UserId));
    }

    [Fact]
    public async Task ShareDashboardValidator_EmptyTenantId_ShouldFail()
    {
        var validator = new ShareDashboard.Validator();
        var command = new ShareDashboard.Command(
            Guid.NewGuid(), "", "user1",
            DashboardSharingScope.Team, DashboardSharingPermission.Read);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.TenantId));
    }

    [Fact]
    public async Task ShareDashboardValidator_InvalidScope_ShouldFail()
    {
        var validator = new ShareDashboard.Validator();
        var command = new ShareDashboard.Command(
            Guid.NewGuid(), "tenant1", "user1",
            (DashboardSharingScope)99, DashboardSharingPermission.Read);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Scope));
    }

    [Fact]
    public async Task ShareDashboardValidator_ValidCommand_ShouldPass()
    {
        var validator = new ShareDashboard.Validator();
        var command = new ShareDashboard.Command(
            Guid.NewGuid(), "tenant1", "user1",
            DashboardSharingScope.Tenant, DashboardSharingPermission.Read);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
