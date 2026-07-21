using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetServiceFeatureFlagDashboard;
using NexTraceOne.Catalog.Application.Contracts.Features.ToggleServiceFeatureFlag;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para a vista de feature flags do detalhe do serviço:
/// GetServiceFeatureFlagDashboard (agregação) e ToggleServiceFeatureFlag (activa/desactiva).
/// </summary>
public sealed class ServiceFeatureFlagDashboardTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ff-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static FeatureFlagRecord MakeFlag(
        string serviceId = "svc-1",
        string flagKey = "flag-1",
        bool isEnabled = true,
        string? ownerId = "owner-1",
        DateTimeOffset? lastToggledAt = null,
        string? enabledEnvJson = null) =>
        FeatureFlagRecord.Create(
            TenantId, serviceId, flagKey, FeatureFlagRecord.FlagType.Release,
            isEnabled, enabledEnvJson, ownerId, lastToggledAt, null, FixedNow.AddDays(-30));

    // ── Dashboard ────────────────────────────────────────────────────────────

    private static GetServiceFeatureFlagDashboard.Handler CreateDashboardHandler(
        IReadOnlyList<FeatureFlagRecord> flags)
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        repo.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(flags);
        return new GetServiceFeatureFlagDashboard.Handler(repo);
    }

    [Fact]
    public async Task Dashboard_EmptyTenant_ReturnsZeroedDashboard()
    {
        var handler = CreateDashboardHandler([]);

        var result = await handler.Handle(new GetServiceFeatureFlagDashboard.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFlags.Should().Be(0);
        result.Value.EnabledFlags.Should().Be(0);
        result.Value.DisabledFlags.Should().Be(0);
        result.Value.AffectedServices.Should().Be(0);
        result.Value.Flags.Should().BeEmpty();
    }

    [Fact]
    public async Task Dashboard_CountsEnabledDisabledAndServices()
    {
        var flags = new[]
        {
            MakeFlag("svc-1", "a", isEnabled: true),
            MakeFlag("svc-1", "b", isEnabled: false),
            MakeFlag("svc-2", "c", isEnabled: true),
        };
        var handler = CreateDashboardHandler(flags);

        var result = await handler.Handle(new GetServiceFeatureFlagDashboard.Query(TenantId), CancellationToken.None);

        result.Value.TotalFlags.Should().Be(3);
        result.Value.EnabledFlags.Should().Be(2);
        result.Value.DisabledFlags.Should().Be(1);
        result.Value.AffectedServices.Should().Be(2);
    }

    [Fact]
    public async Task Dashboard_MapsFieldsFromRecord()
    {
        var toggledAt = FixedNow.AddDays(-2);
        var flags = new[] { MakeFlag("svc-9", "my-flag", isEnabled: true, ownerId: "team-x", lastToggledAt: toggledAt) };
        var handler = CreateDashboardHandler(flags);

        var result = await handler.Handle(new GetServiceFeatureFlagDashboard.Query(TenantId), CancellationToken.None);

        var flag = result.Value.Flags.Single();
        flag.ServiceId.Should().Be("svc-9");
        flag.FlagKey.Should().Be("my-flag");
        flag.DisplayName.Should().Be("my-flag");
        flag.Enabled.Should().BeTrue();
        flag.UpdatedBy.Should().Be("team-x");
        flag.UpdatedAt.Should().Be(toggledAt);
        flag.Id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Dashboard_UpdatedAt_FallsBackToCreatedAt_WhenNeverToggled()
    {
        var flags = new[] { MakeFlag(lastToggledAt: null) };
        var handler = CreateDashboardHandler(flags);

        var result = await handler.Handle(new GetServiceFeatureFlagDashboard.Query(TenantId), CancellationToken.None);

        result.Value.Flags.Single().UpdatedAt.Should().Be(FixedNow.AddDays(-30));
    }

    [Fact]
    public async Task Dashboard_ResolvesEnvironmentFromJson()
    {
        var flags = new[] { MakeFlag(enabledEnvJson: "[\"staging\",\"prod\"]") };
        var handler = CreateDashboardHandler(flags);

        var result = await handler.Handle(new GetServiceFeatureFlagDashboard.Query(TenantId), CancellationToken.None);

        result.Value.Flags.Single().Environment.Should().Contain("staging");
    }

    [Fact]
    public async Task Dashboard_ResolvesEnvironmentDefault_WhenNoEnvJson()
    {
        var handler = CreateDashboardHandler([MakeFlag(enabledEnvJson: null)]);

        var result = await handler.Handle(new GetServiceFeatureFlagDashboard.Query(TenantId), CancellationToken.None);

        result.Value.Flags.Single().Environment.Should().Be("default");
    }

    [Fact]
    public void Dashboard_EmptyTenantId_ValidationFails()
    {
        var result = new GetServiceFeatureFlagDashboard.Validator()
            .Validate(new GetServiceFeatureFlagDashboard.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ── Toggle ───────────────────────────────────────────────────────────────

    private static (ToggleServiceFeatureFlag.Handler Handler, IFeatureFlagRepository Repo) CreateToggleHandler(
        FeatureFlagRecord? found)
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(found);
        return (new ToggleServiceFeatureFlag.Handler(repo, CreateClock()), repo);
    }

    [Fact]
    public async Task Toggle_ExistingFlag_UpdatesEnabledState()
    {
        var flag = MakeFlag(isEnabled: false);
        var (handler, _) = CreateToggleHandler(flag);

        var result = await handler.Handle(
            new ToggleServiceFeatureFlag.Command(flag.Id, TenantId, Enabled: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Enabled.Should().BeTrue();
        flag.IsEnabled.Should().BeTrue();
        flag.LastToggledAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Toggle_Disable_SetsEnabledFalse()
    {
        var flag = MakeFlag(isEnabled: true);
        var (handler, _) = CreateToggleHandler(flag);

        var result = await handler.Handle(
            new ToggleServiceFeatureFlag.Command(flag.Id, TenantId, Enabled: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        flag.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Toggle_UnknownFlag_ReturnsNotFound()
    {
        var (handler, _) = CreateToggleHandler(found: null);

        var result = await handler.Handle(
            new ToggleServiceFeatureFlag.Command(Guid.NewGuid(), TenantId, Enabled: true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FeatureFlag.NotFound");
    }

    [Fact]
    public void Toggle_EmptyFlagId_ValidationFails()
    {
        var result = new ToggleServiceFeatureFlag.Validator()
            .Validate(new ToggleServiceFeatureFlag.Command(Guid.Empty, TenantId, true));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Toggle_ValidCommand_PassesValidation()
    {
        var result = new ToggleServiceFeatureFlag.Validator()
            .Validate(new ToggleServiceFeatureFlag.Command(Guid.NewGuid(), TenantId, true));
        result.IsValid.Should().BeTrue();
    }
}
