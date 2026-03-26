using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Application.Features.RemoveFeatureFlagOverride;
using NexTraceOne.Configuration.Application.Features.RemoveOverride;
using NexTraceOne.Configuration.Application.Features.SetConfigurationValue;
using NexTraceOne.Configuration.Application.Features.SetFeatureFlagOverride;
using NexTraceOne.Configuration.Application.Features.ToggleConfiguration;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Features;

/// <summary>
/// Testes que validam o tratamento de conflitos de concorrência otimista nos handlers de escrita.
/// Garante que <see cref="ConcurrencyException"/> é corretamente traduzida para
/// <c>CONFIG_CONCURRENCY_CONFLICT</c> / <c>FEATURE_FLAG_CONCURRENCY_CONFLICT</c>.
/// </summary>
public sealed class ConcurrencyConflictTests
{
    private static Task<int> ThrowConcurrencyException(string entityType)
        => Task.FromException<int>(new ConcurrencyException(entityType));

    // ── SetConfigurationValue ──────────────────────────────────────────

    [Fact]
    public async Task SetConfigurationValue_WhenCommitThrowsConcurrencyException_ReturnsConflictError()
    {
        var definitionRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var securitySvc = Substitute.For<IConfigurationSecurityService>();
        var cacheSvc = Substitute.For<IConfigurationCacheService>();
        var currentUser = Substitute.For<ICurrentUser>();
        var uow = Substitute.For<IUnitOfWork>();

        var definition = ConfigurationDefinition.Create(
            "platform.test", "Test", ConfigurationCategory.Functional, ConfigurationValueType.Boolean,
            [ConfigurationScope.System], defaultValue: "true", uiEditorType: "toggle", sortOrder: 1);

        definitionRepo.GetByKeyAsync("platform.test", Arg.Any<CancellationToken>()).Returns(definition);
        entryRepo.GetByKeyAndScopeAsync("platform.test", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns((ConfigurationEntry?)null);
        currentUser.Id.Returns("user-1");
        securitySvc.EncryptValue(Arg.Any<string>()).Returns(x => x.Arg<string>());
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(ThrowConcurrencyException("ConfigurationEntry"));

        var handler = new SetConfigurationValue.Handler(definitionRepo, entryRepo, auditRepo, securitySvc, cacheSvc, currentUser, uow);
        var result = await handler.Handle(
            new SetConfigurationValue.Command("platform.test", ConfigurationScope.System, null, "true", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CONFIG_CONCURRENCY_CONFLICT");
        result.Error.Type.Should().Be(NexTraceOne.BuildingBlocks.Core.Results.ErrorType.Conflict);
    }

    // ── ToggleConfiguration ────────────────────────────────────────────

    [Fact]
    public async Task ToggleConfiguration_WhenCommitThrowsConcurrencyException_ReturnsConflictError()
    {
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var cacheSvc = Substitute.For<IConfigurationCacheService>();
        var currentUser = Substitute.For<ICurrentUser>();
        var uow = Substitute.For<IUnitOfWork>();

        var definition = ConfigurationDefinition.Create(
            "platform.test", "Test", ConfigurationCategory.Functional, ConfigurationValueType.Boolean,
            [ConfigurationScope.System], defaultValue: "true", uiEditorType: "toggle", sortOrder: 1);

        var entry = ConfigurationEntry.Create(
            definition.Id, "platform.test", ConfigurationScope.System, "user-1",
            scopeReferenceId: null, value: "true", isSensitive: false, isEncrypted: false);

        entryRepo.GetByKeyAndScopeAsync("platform.test", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns(entry);
        currentUser.Id.Returns("user-1");
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(ThrowConcurrencyException("ConfigurationEntry"));

        var handler = new ToggleConfiguration.Handler(entryRepo, auditRepo, cacheSvc, currentUser, uow);
        var result = await handler.Handle(
            new ToggleConfiguration.Command("platform.test", ConfigurationScope.System, null, Activate: false, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CONFIG_CONCURRENCY_CONFLICT");
        result.Error.Type.Should().Be(NexTraceOne.BuildingBlocks.Core.Results.ErrorType.Conflict);
    }

    // ── RemoveOverride ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveOverride_WhenCommitThrowsConcurrencyException_ReturnsConflictError()
    {
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var cacheSvc = Substitute.For<IConfigurationCacheService>();
        var currentUser = Substitute.For<ICurrentUser>();
        var uow = Substitute.For<IUnitOfWork>();

        var definition = ConfigurationDefinition.Create(
            "platform.test", "Test", ConfigurationCategory.Functional, ConfigurationValueType.Boolean,
            [ConfigurationScope.System], defaultValue: "true", uiEditorType: "toggle", sortOrder: 1);

        var entry = ConfigurationEntry.Create(
            definition.Id, "platform.test", ConfigurationScope.System, "user-1",
            scopeReferenceId: null, value: "true", isSensitive: false, isEncrypted: false);

        entryRepo.GetByKeyAndScopeAsync("platform.test", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns(entry);
        currentUser.Id.Returns("user-1");
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(ThrowConcurrencyException("ConfigurationEntry"));

        var handler = new RemoveOverride.Handler(entryRepo, auditRepo, cacheSvc, currentUser, uow);
        var result = await handler.Handle(
            new RemoveOverride.Command("platform.test", ConfigurationScope.System, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CONFIG_CONCURRENCY_CONFLICT");
        result.Error.Type.Should().Be(NexTraceOne.BuildingBlocks.Core.Results.ErrorType.Conflict);
    }

    // ── SetFeatureFlagOverride ─────────────────────────────────────────

    [Fact]
    public async Task SetFeatureFlagOverride_WhenCommitThrowsConcurrencyException_ReturnsConflictError()
    {
        var repository = Substitute.For<IFeatureFlagRepository>();
        var cacheSvc = Substitute.For<IConfigurationCacheService>();
        var currentUser = Substitute.For<ICurrentUser>();
        var uow = Substitute.For<IUnitOfWork>();

        var flagDef = FeatureFlagDefinition.Create(
            "feature.test", "Test", [ConfigurationScope.System], defaultEnabled: false);

        repository.GetDefinitionByKeyAsync("feature.test", Arg.Any<CancellationToken>()).Returns(flagDef);
        repository.GetEntryByKeyAndScopeAsync("feature.test", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns((FeatureFlagEntry?)null);
        currentUser.Id.Returns("user-1");
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(ThrowConcurrencyException("FeatureFlagEntry"));

        var handler = new SetFeatureFlagOverride.Handler(repository, cacheSvc, currentUser, uow);
        var result = await handler.Handle(
            new SetFeatureFlagOverride.Command("feature.test", ConfigurationScope.System, null, IsEnabled: true, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FEATURE_FLAG_CONCURRENCY_CONFLICT");
        result.Error.Type.Should().Be(NexTraceOne.BuildingBlocks.Core.Results.ErrorType.Conflict);
    }

    // ── RemoveFeatureFlagOverride ──────────────────────────────────────

    [Fact]
    public async Task RemoveFeatureFlagOverride_WhenCommitThrowsConcurrencyException_ReturnsConflictError()
    {
        var repository = Substitute.For<IFeatureFlagRepository>();
        var cacheSvc = Substitute.For<IConfigurationCacheService>();
        var currentUser = Substitute.For<ICurrentUser>();
        var uow = Substitute.For<IUnitOfWork>();

        var flagDef = FeatureFlagDefinition.Create(
            "feature.test", "Test", [ConfigurationScope.System], defaultEnabled: false);

        var entry = FeatureFlagEntry.Create(
            flagDef.Id, "feature.test", ConfigurationScope.System, isEnabled: true, createdBy: "user-1");

        repository.GetEntryByKeyAndScopeAsync("feature.test", ConfigurationScope.System, null, Arg.Any<CancellationToken>())
            .Returns(entry);
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(ThrowConcurrencyException("FeatureFlagEntry"));

        var handler = new RemoveFeatureFlagOverride.Handler(repository, cacheSvc, currentUser, uow);
        var result = await handler.Handle(
            new RemoveFeatureFlagOverride.Command("feature.test", ConfigurationScope.System, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FEATURE_FLAG_CONCURRENCY_CONFLICT");
        result.Error.Type.Should().Be(NexTraceOne.BuildingBlocks.Core.Results.ErrorType.Conflict);
    }

    // ── RowVersion in DTOs ─────────────────────────────────────────────

    [Fact]
    public void ConfigurationEntryDto_ShouldExposeRowVersion()
    {
        var dto = new ConfigurationEntryDto(
            Id: Guid.NewGuid(), DefinitionKey: "platform.test", Scope: "System",
            ScopeReferenceId: null, Value: "true", IsActive: true, Version: 1,
            ChangeReason: null, UpdatedAt: DateTimeOffset.UtcNow, UpdatedBy: "user-1", RowVersion: 42u);

        dto.RowVersion.Should().Be(42u);
    }

    [Fact]
    public void FeatureFlagEntryDto_ShouldExposeRowVersion()
    {
        var dto = new FeatureFlagEntryDto(
            Id: Guid.NewGuid(), Key: "feature.test", Scope: "System",
            ScopeReferenceId: null, IsEnabled: true, IsActive: true,
            ChangeReason: null, UpdatedAt: DateTimeOffset.UtcNow, UpdatedBy: "user-1", RowVersion: 99u);

        dto.RowVersion.Should().Be(99u);
    }
}
