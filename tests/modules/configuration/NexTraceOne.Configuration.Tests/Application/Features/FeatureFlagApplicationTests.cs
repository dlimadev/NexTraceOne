using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.IntegrationEvents;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

using GetEffective = NexTraceOne.Configuration.Application.Features.GetEffectiveFeatureFlag.GetEffectiveFeatureFlag;
using GetFlags = NexTraceOne.Configuration.Application.Features.GetFeatureFlags.GetFeatureFlags;
using RemoveOverride = NexTraceOne.Configuration.Application.Features.RemoveFeatureFlagOverride.RemoveFeatureFlagOverride;
using SetOverride = NexTraceOne.Configuration.Application.Features.SetFeatureFlagOverride.SetFeatureFlagOverride;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes das features GetEffectiveFeatureFlag, GetFeatureFlags,
/// SetFeatureFlagOverride e RemoveFeatureFlagOverride —
/// gestão e resolução de feature flags por âmbito.
/// </summary>
public sealed class FeatureFlagApplicationTests
{
    private static readonly ConfigurationScope[] AllScopes =
        [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment];

    private static ICurrentUser CreateUser(string id = "user-123")
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.Id.Returns(id);
        user.Name.Returns("Test User");
        user.Email.Returns($"{id}@test.com");
        return user;
    }

    private static FeatureFlagDefinition CreateDefinition(
        string key = "ai.assistant.enabled",
        bool defaultEnabled = false,
        bool isEditable = true,
        ConfigurationScope[]? allowedScopes = null)
    {
        return FeatureFlagDefinition.Create(
            key: key,
            displayName: "AI Assistant",
            allowedScopes: allowedScopes ?? AllScopes,
            description: "Enables the AI assistant feature",
            defaultEnabled: defaultEnabled,
            isEditable: isEditable);
    }

    // ── GetEffectiveFeatureFlag ───────────────────────────────────────────

    [Fact]
    public async Task GetEffectiveFeatureFlag_Should_Return_Default_When_No_Override_Exists()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        var definition = CreateDefinition(defaultEnabled: true);

        repo.GetDefinitionByKeyAsync("ai.assistant.enabled", Arg.Any<CancellationToken>())
            .Returns(definition);
        repo.GetAllEntriesByKeyAsync("ai.assistant.enabled", Arg.Any<CancellationToken>())
            .Returns(new List<FeatureFlagEntry>());

        var sut = new GetEffective.Handler(repo);
        var result = await sut.Handle(
            new GetEffective.Query("ai.assistant.enabled", ConfigurationScope.Tenant, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Flag.Should().NotBeNull();
        result.Value.Flag!.IsEnabled.Should().BeTrue();
        result.Value.Flag.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetEffectiveFeatureFlag_Should_Return_NotFound_When_Definition_Missing()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        repo.GetDefinitionByKeyAsync("missing.flag", Arg.Any<CancellationToken>())
            .Returns((FeatureFlagDefinition?)null);

        var sut = new GetEffective.Handler(repo);
        var result = await sut.Handle(
            new GetEffective.Query("missing.flag", ConfigurationScope.System, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FEATURE_FLAG_NOT_FOUND");
    }

    // ── GetFeatureFlags ──────────────────────────────────────────────────

    [Fact]
    public async Task GetFeatureFlags_Should_Return_All_Definitions()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        var definitions = new List<FeatureFlagDefinition>
        {
            CreateDefinition("flag.one"),
            CreateDefinition("flag.two"),
        };
        repo.GetAllDefinitionsAsync(Arg.Any<CancellationToken>()).Returns(definitions);

        var sut = new GetFlags.Handler(repo);
        var result = await sut.Handle(new GetFlags.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(d => d.Key).Should().Contain("flag.one").And.Contain("flag.two");
    }

    // ── SetFeatureFlagOverride ───────────────────────────────────────────

    [Fact]
    public async Task SetFeatureFlagOverride_Should_Create_Entry_When_Valid()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();
        var definition = CreateDefinition();

        repo.GetDefinitionByKeyAsync("ai.assistant.enabled", Arg.Any<CancellationToken>())
            .Returns(definition);
        repo.GetEntryByKeyAndScopeAsync("ai.assistant.enabled", ConfigurationScope.Tenant, null, Arg.Any<CancellationToken>())
            .Returns((FeatureFlagEntry?)null);

        var sut = new SetOverride.Handler(repo, cache, currentUser, uow, Substitute.For<IEventBus>());
        var result = await sut.Handle(
            new SetOverride.Command("ai.assistant.enabled", ConfigurationScope.Tenant, null, true, "Enable for tenant"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("ai.assistant.enabled");
        result.Value.IsEnabled.Should().BeTrue();
        await repo.Received(1).AddEntryAsync(Arg.Any<FeatureFlagEntry>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetFeatureFlagOverride_Should_Publish_Integration_Event()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();
        var eventBus = Substitute.For<IEventBus>();
        var definition = CreateDefinition();

        repo.GetDefinitionByKeyAsync("ai.assistant.enabled", Arg.Any<CancellationToken>())
            .Returns(definition);
        repo.GetEntryByKeyAndScopeAsync("ai.assistant.enabled", ConfigurationScope.Tenant, null, Arg.Any<CancellationToken>())
            .Returns((FeatureFlagEntry?)null);

        var sut = new SetOverride.Handler(repo, cache, currentUser, uow, eventBus);
        var result = await sut.Handle(
            new SetOverride.Command("ai.assistant.enabled", ConfigurationScope.Tenant, null, true, "Enable for tenant"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await eventBus.Received(1).PublishAsync(
            Arg.Is<ConfigurationIntegrationEvents.ConfigurationValueChanged>(e =>
                e.Key == "ai.assistant.enabled" &&
                e.NewValue == "True"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetFeatureFlagOverride_Should_Fail_When_Definition_Not_Found()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();

        repo.GetDefinitionByKeyAsync("missing.flag", Arg.Any<CancellationToken>())
            .Returns((FeatureFlagDefinition?)null);

        var sut = new SetOverride.Handler(repo, cache, currentUser, uow, Substitute.For<IEventBus>());
        var result = await sut.Handle(
            new SetOverride.Command("missing.flag", ConfigurationScope.System, null, true, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FEATURE_FLAG_NOT_FOUND");
    }

    [Fact]
    public async Task SetFeatureFlagOverride_Should_Fail_When_Not_Editable()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();
        var definition = CreateDefinition(isEditable: false);

        repo.GetDefinitionByKeyAsync("ai.assistant.enabled", Arg.Any<CancellationToken>())
            .Returns(definition);

        var sut = new SetOverride.Handler(repo, cache, currentUser, uow, Substitute.For<IEventBus>());
        var result = await sut.Handle(
            new SetOverride.Command("ai.assistant.enabled", ConfigurationScope.System, null, true, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FEATURE_FLAG_NOT_EDITABLE");
    }

    // ── RemoveFeatureFlagOverride ────────────────────────────────────────

    [Fact]
    public async Task RemoveFeatureFlagOverride_Should_Delete_When_Entry_Exists()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();

        var definition = CreateDefinition();
        var entry = FeatureFlagEntry.Create(
            definitionId: definition.Id,
            key: "ai.assistant.enabled",
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "user-123");

        repo.GetEntryByKeyAndScopeAsync("ai.assistant.enabled", ConfigurationScope.Tenant, null, Arg.Any<CancellationToken>())
            .Returns(entry);

        var sut = new RemoveOverride.Handler(repo, cache, currentUser, uow);
        var result = await sut.Handle(
            new RemoveOverride.Command("ai.assistant.enabled", ConfigurationScope.Tenant, null, "Cleanup"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        await repo.Received(1).DeleteEntryAsync(entry, Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveFeatureFlagOverride_Should_Fail_When_Entry_Not_Found()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();

        repo.GetEntryByKeyAndScopeAsync("missing.flag", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns((FeatureFlagEntry?)null);

        var sut = new RemoveOverride.Handler(repo, cache, currentUser, uow);
        var result = await sut.Handle(
            new RemoveOverride.Command("missing.flag", ConfigurationScope.System, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FEATURE_FLAG_ENTRY_NOT_FOUND");
    }
}
