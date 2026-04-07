using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

using GetPrefsFeature = NexTraceOne.Configuration.Application.Features.GetUserPreferences.GetUserPreferences;
using SetPrefFeature = NexTraceOne.Configuration.Application.Features.SetUserPreference.SetUserPreference;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de GetUserPreferences e SetUserPreference — personalização de plataforma por utilizador.
/// </summary>
public sealed class UserPreferencesTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static EffectiveConfigurationDto CreateEffective(string key, string? value) =>
        new(key, value, "Tenant", null, false, false, key, "string", false, 1);

    private static ICurrentUser CreateAuthenticatedUser(string id = "user-123", string name = "Test User")
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.Id.Returns(id);
        user.Name.Returns(name);
        user.Email.Returns($"{id}@test.com");
        return user;
    }

    private static ICurrentUser CreateAnonymousUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(false);
        return user;
    }

    // ── GetUserPreferences ──────────────────────────────────

    [Fact]
    public async Task GetPreferences_Should_Return_User_Entries()
    {
        var configService = Substitute.For<IConfigurationResolutionService>();
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var currentUser = CreateAuthenticatedUser();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        entryRepo.GetAllByScopeAsync(ConfigurationScope.User, "user-123", Arg.Any<CancellationToken>())
            .Returns(new List<ConfigurationEntry>());

        configService.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateEffective("platform.sidebar.user_customization.enabled", "true"));

        var sut = new GetPrefsFeature.Handler(configService, entryRepo, currentUser, dt);
        var result = await sut.Handle(new GetPrefsFeature.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be("user-123");
        result.Value.SidebarCustomizationEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetPreferences_Should_Fail_When_Not_Authenticated()
    {
        var configService = Substitute.For<IConfigurationResolutionService>();
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var currentUser = CreateAnonymousUser();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        var sut = new GetPrefsFeature.Handler(configService, entryRepo, currentUser, dt);
        var result = await sut.Handle(new GetPrefsFeature.Query(null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── SetUserPreference ───────────────────────────────────

    [Fact]
    public async Task SetPreference_Should_Create_New_Entry()
    {
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var currentUser = CreateAuthenticatedUser();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var definition = ConfigurationDefinition.Create(
            "platform.sidebar.pinned_items",
            "Pinned sidebar items",
            ConfigurationCategory.Functional,
            ConfigurationValueType.Json,
            [ConfigurationScope.User],
            defaultValue: "[]");
        defRepo.GetByKeyAsync("platform.sidebar.pinned_items", Arg.Any<CancellationToken>()).Returns(definition);
        entryRepo.GetByKeyAndScopeAsync("platform.sidebar.pinned_items", ConfigurationScope.User, "user-123", Arg.Any<CancellationToken>())
            .Returns((ConfigurationEntry?)null);

        var sut = new SetPrefFeature.Handler(entryRepo, defRepo, currentUser, uow);
        var result = await sut.Handle(
            new SetPrefFeature.Command("platform.sidebar.pinned_items", """["catalog","changes"]"""),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("platform.sidebar.pinned_items");
        await entryRepo.Received(1).AddAsync(Arg.Any<ConfigurationEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetPreference_Should_Fail_When_Scope_Not_Allowed()
    {
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var currentUser = CreateAuthenticatedUser();
        var uow = Substitute.For<IUnitOfWork>();

        // Definition only allows System scope, not User
        var definition = ConfigurationDefinition.Create(
            "platform.system.only",
            "System only config",
            ConfigurationCategory.Functional,
            ConfigurationValueType.String,
            [ConfigurationScope.System],
            defaultValue: "default");
        defRepo.GetByKeyAsync("platform.system.only", Arg.Any<CancellationToken>()).Returns(definition);

        var sut = new SetPrefFeature.Handler(entryRepo, defRepo, currentUser, uow);
        var result = await sut.Handle(
            new SetPrefFeature.Command("platform.system.only", "value"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ScopeNotAllowed");
    }

    [Fact]
    public async Task SetPreference_Should_Fail_When_Not_Authenticated()
    {
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var currentUser = CreateAnonymousUser();
        var uow = Substitute.For<IUnitOfWork>();

        var sut = new SetPrefFeature.Handler(entryRepo, defRepo, currentUser, uow);
        var result = await sut.Handle(
            new SetPrefFeature.Command("platform.sidebar.pinned_items", "[]"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SetPreference_Should_Fail_When_Definition_Not_Found()
    {
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var currentUser = CreateAuthenticatedUser();
        var uow = Substitute.For<IUnitOfWork>();

        defRepo.GetByKeyAsync("platform.nonexistent", Arg.Any<CancellationToken>()).Returns((ConfigurationDefinition?)null);

        var sut = new SetPrefFeature.Handler(entryRepo, defRepo, currentUser, uow);
        var result = await sut.Handle(
            new SetPrefFeature.Command("platform.nonexistent", "value"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("DefinitionNotFound");
    }
}
