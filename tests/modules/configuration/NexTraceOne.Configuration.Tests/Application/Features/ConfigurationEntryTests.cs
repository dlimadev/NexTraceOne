using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

using GetEntriesFeature = NexTraceOne.Configuration.Application.Features.GetEntries.GetEntries;
using GetEffectiveFeature = NexTraceOne.Configuration.Application.Features.GetEffectiveSettings.GetEffectiveSettings;
using SetValueFeature = NexTraceOne.Configuration.Application.Features.SetConfigurationValue.SetConfigurationValue;
using ToggleFeature = NexTraceOne.Configuration.Application.Features.ToggleConfiguration.ToggleConfiguration;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes das features GetEntries, SetConfigurationValue,
/// GetEffectiveSettings e ToggleConfiguration —
/// gestão de entradas de configuração por âmbito.
/// </summary>
public sealed class ConfigurationEntryTests
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

    private static ConfigurationDefinition CreateDefinition(
        string key = "platform.max.retries",
        ConfigurationValueType valueType = ConfigurationValueType.Integer,
        bool isEditable = true,
        bool isDeprecated = false,
        string? deprecatedMessage = null,
        bool isSensitive = false,
        ConfigurationScope[]? allowedScopes = null)
    {
        return ConfigurationDefinition.Create(
            key: key,
            displayName: "Max Retries",
            category: ConfigurationCategory.Functional,
            valueType: valueType,
            allowedScopes: allowedScopes ?? AllScopes,
            description: "Maximum number of retries",
            isEditable: isEditable,
            isDeprecated: isDeprecated,
            deprecatedMessage: deprecatedMessage,
            isSensitive: isSensitive);
    }

    private static ConfigurationEntry CreateEntry(
        ConfigurationDefinition definition,
        string? value = "5",
        ConfigurationScope scope = ConfigurationScope.System)
    {
        return ConfigurationEntry.Create(
            definitionId: definition.Id,
            key: definition.Key,
            scope: scope,
            createdBy: "user-123",
            value: value);
    }

    // ── GetEntries ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetEntries_Should_Return_Entries_For_Scope()
    {
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var securityService = Substitute.For<IConfigurationSecurityService>();
        var definition = CreateDefinition();
        var entries = new List<ConfigurationEntry> { CreateEntry(definition) };

        entryRepo.GetAllByScopeAsync(ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns(entries);

        var sut = new GetEntriesFeature.Handler(entryRepo, securityService);
        var result = await sut.Handle(
            new GetEntriesFeature.Query(ConfigurationScope.System, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].DefinitionKey.Should().Be("platform.max.retries");
        result.Value[0].Value.Should().Be("5");
    }

    [Fact]
    public async Task GetEntries_Should_Return_Empty_When_No_Entries()
    {
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var securityService = Substitute.For<IConfigurationSecurityService>();

        entryRepo.GetAllByScopeAsync(ConfigurationScope.Tenant, "tenant-1", Arg.Any<CancellationToken>())
            .Returns(new List<ConfigurationEntry>());

        var sut = new GetEntriesFeature.Handler(entryRepo, securityService);
        var result = await sut.Handle(
            new GetEntriesFeature.Query(ConfigurationScope.Tenant, "tenant-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ── SetConfigurationValue ────────────────────────────────────────────

    [Fact]
    public async Task SetConfigurationValue_Should_Create_Entry_When_Valid()
    {
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var securityService = Substitute.For<IConfigurationSecurityService>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();
        var definition = CreateDefinition();

        defRepo.GetByKeyAsync("platform.max.retries", Arg.Any<CancellationToken>())
            .Returns(definition);
        entryRepo.GetByKeyAndScopeAsync("platform.max.retries", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns((ConfigurationEntry?)null);

        var sut = new SetValueFeature.Handler(defRepo, entryRepo, auditRepo, securityService, cache, currentUser, uow);
        var result = await sut.Handle(
            new SetValueFeature.Command("platform.max.retries", ConfigurationScope.System, null, "10", "Increase retries"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DefinitionKey.Should().Be("platform.max.retries");
        result.Value.Value.Should().Be("10");
        await entryRepo.Received(1).AddAsync(Arg.Any<ConfigurationEntry>(), Arg.Any<CancellationToken>());
        await auditRepo.Received(1).AddAsync(Arg.Any<ConfigurationAuditEntry>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetConfigurationValue_Should_Fail_When_Definition_Not_Found()
    {
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var securityService = Substitute.For<IConfigurationSecurityService>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();

        defRepo.GetByKeyAsync("missing.key", Arg.Any<CancellationToken>())
            .Returns((ConfigurationDefinition?)null);

        var sut = new SetValueFeature.Handler(defRepo, entryRepo, auditRepo, securityService, cache, currentUser, uow);
        var result = await sut.Handle(
            new SetValueFeature.Command("missing.key", ConfigurationScope.System, null, "value", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CONFIG_DEFINITION_NOT_FOUND");
    }

    [Fact]
    public async Task SetConfigurationValue_Should_Fail_When_Value_Type_Invalid()
    {
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var securityService = Substitute.For<IConfigurationSecurityService>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();
        var definition = CreateDefinition(valueType: ConfigurationValueType.Integer);

        defRepo.GetByKeyAsync("platform.max.retries", Arg.Any<CancellationToken>())
            .Returns(definition);

        var sut = new SetValueFeature.Handler(defRepo, entryRepo, auditRepo, securityService, cache, currentUser, uow);
        var result = await sut.Handle(
            new SetValueFeature.Command("platform.max.retries", ConfigurationScope.System, null, "not-a-number", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CONFIG_VALUE_TYPE_INVALID");
    }

    // ── GetEffectiveSettings ─────────────────────────────────────────────

    [Fact]
    public async Task GetEffectiveSettings_Should_Return_Resolved_Value()
    {
        var resolutionService = Substitute.For<IConfigurationResolutionService>();
        var effective = new EffectiveConfigurationDto(
            Key: "platform.max.retries",
            EffectiveValue: "10",
            ResolvedScope: "System",
            ResolvedScopeReferenceId: null,
            IsInherited: false,
            IsDefault: false,
            DefinitionKey: "platform.max.retries",
            ValueType: "Integer",
            IsSensitive: false,
            Version: 1);

        resolutionService.ResolveEffectiveValueAsync("platform.max.retries", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns(effective);

        var sut = new GetEffectiveFeature.Handler(resolutionService);
        var result = await sut.Handle(
            new GetEffectiveFeature.Query("platform.max.retries", ConfigurationScope.System, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Setting.Should().NotBeNull();
        result.Value.Setting!.EffectiveValue.Should().Be("10");
    }

    [Fact]
    public async Task GetEffectiveSettings_Should_Return_NotFound_When_Key_Missing()
    {
        var resolutionService = Substitute.For<IConfigurationResolutionService>();

        resolutionService.ResolveEffectiveValueAsync("missing.key", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new GetEffectiveFeature.Handler(resolutionService);
        var result = await sut.Handle(
            new GetEffectiveFeature.Query("missing.key", ConfigurationScope.System, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CONFIG_KEY_NOT_FOUND");
    }

    // ── ToggleConfiguration ──────────────────────────────────────────────

    [Fact]
    public async Task ToggleConfiguration_Should_Deactivate_Active_Entry()
    {
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();

        var definition = CreateDefinition();
        var entry = CreateEntry(definition);

        entryRepo.GetByKeyAndScopeAsync("platform.max.retries", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns(entry);

        var sut = new ToggleFeature.Handler(entryRepo, auditRepo, cache, currentUser, uow);
        var result = await sut.Handle(
            new ToggleFeature.Command("platform.max.retries", ConfigurationScope.System, null, false, "Maintenance"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        entry.IsActive.Should().BeFalse();
        await entryRepo.Received(1).UpdateAsync(entry, Arg.Any<CancellationToken>());
        await auditRepo.Received(1).AddAsync(Arg.Any<ConfigurationAuditEntry>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleConfiguration_Should_Fail_When_Entry_Not_Found()
    {
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var cache = Substitute.For<IConfigurationCacheService>();
        var currentUser = CreateUser();
        var uow = Substitute.For<IUnitOfWork>();

        entryRepo.GetByKeyAndScopeAsync("missing.key", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns((ConfigurationEntry?)null);

        var sut = new ToggleFeature.Handler(entryRepo, auditRepo, cache, currentUser, uow);
        var result = await sut.Handle(
            new ToggleFeature.Command("missing.key", ConfigurationScope.System, null, false, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CONFIG_ENTRY_NOT_FOUND");
    }
}
