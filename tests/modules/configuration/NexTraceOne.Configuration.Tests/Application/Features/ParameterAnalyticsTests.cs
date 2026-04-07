using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

using UsageReportFeature = NexTraceOne.Configuration.Application.Features.GetParameterUsageReport.GetParameterUsageReport;
using ComplianceFeature = NexTraceOne.Configuration.Application.Features.GetParameterComplianceSummary.GetParameterComplianceSummary;
using PersonaActivityFeature = NexTraceOne.Configuration.Application.Features.TrackPersonaActivity.TrackPersonaActivity;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes para as features de analytics de parametrização:
/// - GetParameterUsageReport
/// - GetParameterComplianceSummary
/// - TrackPersonaActivity
/// </summary>
public sealed class ParameterAnalyticsTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    private static ConfigurationDefinition CreateDefinition(
        string key, string displayName = "Test", bool isDeprecated = false,
        bool isSensitive = false, bool isEditable = true, string? validationRules = null)
    {
        return ConfigurationDefinition.Create(
            key: key,
            displayName: displayName,
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: $"Description for {key}",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 100,
            isSensitive: isSensitive,
            isEditable: isEditable,
            isInheritable: true,
            isDeprecated: isDeprecated,
            validationRules: validationRules);
    }

    private static ConfigurationAuditEntry CreateAuditEntry(string key, string changedBy = "user-1")
    {
        return ConfigurationAuditEntry.Create(
            entryId: new ConfigurationEntryId(Guid.NewGuid()),
            key: key,
            scope: ConfigurationScope.Tenant,
            action: "Updated",
            newVersion: 2,
            changedBy: changedBy,
            newValue: "false",
            previousValue: "true",
            previousVersion: 1);
    }

    // ── GetParameterUsageReport ─────────────────────────────────────────

    [Fact]
    public async Task UsageReport_EmptyDefinitions_ReturnsZeroCounts()
    {
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        defRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ConfigurationDefinition>() as IReadOnlyList<ConfigurationDefinition>);

        var handler = new UsageReportFeature.Handler(defRepo, entryRepo, auditRepo);
        var result = await handler.Handle(new UsageReportFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDefinitions.Should().Be(0);
        result.Value.TotalOverrides.Should().Be(0);
        result.Value.OverrideCoveragePercent.Should().Be(0);
    }

    [Fact]
    public async Task UsageReport_WithOverrides_ReturnsCorrectCounts()
    {
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();

        var def1 = CreateDefinition("param.one");
        var def2 = CreateDefinition("param.two");
        defRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { def1, def2 } as IReadOnlyList<ConfigurationDefinition>);

        // param.one has 2 overrides, param.two has 0
        var entry1 = CreateEntryStub("param.one", ConfigurationScope.Tenant);
        var entry2 = CreateEntryStub("param.one", ConfigurationScope.User);
        entryRepo.GetAllByKeyAsync("param.one", Arg.Any<CancellationToken>())
            .Returns(new[] { entry1, entry2 } as IReadOnlyList<ConfigurationEntry>);
        entryRepo.GetAllByKeyAsync("param.two", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ConfigurationEntry>() as IReadOnlyList<ConfigurationEntry>);

        var handler = new UsageReportFeature.Handler(defRepo, entryRepo, auditRepo);
        var result = await handler.Handle(new UsageReportFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.TotalDefinitions.Should().Be(2);
        dto.TotalOverrides.Should().Be(2);
        dto.DefinitionsWithOverrides.Should().Be(1);
        dto.DefinitionsUsingDefault.Should().Be(1);
        dto.OverrideCoveragePercent.Should().Be(50);
        dto.MostOverridden.Should().HaveCount(1);
        dto.MostOverridden[0].Key.Should().Be("param.one");
        dto.OverridesByScope.Should().HaveCount(2);
    }

    [Fact]
    public async Task UsageReport_ScopeDistribution_OrderedByCount()
    {
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var entryRepo = Substitute.For<IConfigurationEntryRepository>();
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();

        var def = CreateDefinition("param.multi");
        defRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { def } as IReadOnlyList<ConfigurationDefinition>);

        var entries = new[]
        {
            CreateEntryStub("param.multi", ConfigurationScope.Tenant),
            CreateEntryStub("param.multi", ConfigurationScope.Tenant),
            CreateEntryStub("param.multi", ConfigurationScope.User),
        };
        entryRepo.GetAllByKeyAsync("param.multi", Arg.Any<CancellationToken>())
            .Returns(entries as IReadOnlyList<ConfigurationEntry>);

        var handler = new UsageReportFeature.Handler(defRepo, entryRepo, auditRepo);
        var result = await handler.Handle(new UsageReportFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var scopes = result.Value.OverridesByScope;
        scopes.First().Scope.Should().Be("Tenant");
        scopes.First().Count.Should().Be(2);
    }

    // ── GetParameterComplianceSummary ───────────────────────────────────

    [Fact]
    public async Task ComplianceSummary_AllI18n_Returns100Percent()
    {
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var defs = new[]
        {
            CreateDefinition("p1", "config.p1.label"),
            CreateDefinition("p2", "config.p2.label"),
        };
        defRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(defs as IReadOnlyList<ConfigurationDefinition>);

        var handler = new ComplianceFeature.Handler(defRepo);
        var result = await handler.Handle(new ComplianceFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.I18nCoveragePercent.Should().Be(100);
        result.Value.WithI18nKeys.Should().Be(2);
        result.Value.WithoutI18nKeys.Should().Be(0);
    }

    [Fact]
    public async Task ComplianceSummary_MixedI18n_ReturnsCorrectPercent()
    {
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var defs = new[]
        {
            CreateDefinition("p1", "config.p1.label"),
            CreateDefinition("p2", "Hardcoded English Text"),
            CreateDefinition("p3", "config.p3.label"),
        };
        defRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(defs as IReadOnlyList<ConfigurationDefinition>);

        var handler = new ComplianceFeature.Handler(defRepo);
        var result = await handler.Handle(new ComplianceFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WithI18nKeys.Should().Be(2);
        result.Value.WithoutI18nKeys.Should().Be(1);
        result.Value.I18nCoveragePercent.Should().BeApproximately(66.67, 0.01);
    }

    [Fact]
    public async Task ComplianceSummary_DeprecatedAndSensitive_CountsCorrectly()
    {
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var defs = new[]
        {
            CreateDefinition("p1", "config.p1.label", isDeprecated: true),
            CreateDefinition("p2", "config.p2.label", isSensitive: true),
            CreateDefinition("p3", "config.p3.label", validationRules: """{"min": 0}"""),
        };
        defRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(defs as IReadOnlyList<ConfigurationDefinition>);

        var handler = new ComplianceFeature.Handler(defRepo);
        var result = await handler.Handle(new ComplianceFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeprecatedCount.Should().Be(1);
        result.Value.SensitiveCount.Should().Be(1);
        result.Value.WithValidationRules.Should().Be(1);
        result.Value.DeprecatedKeys.Should().Contain("p1");
    }

    [Fact]
    public async Task ComplianceSummary_ByCategory_GroupsCorrectly()
    {
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        var defs = new[]
        {
            CreateDefinition("p1", "config.p1.label"),
            CreateDefinition("p2", "config.p2.label"),
        };
        defRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(defs as IReadOnlyList<ConfigurationDefinition>);

        var handler = new ComplianceFeature.Handler(defRepo);
        var result = await handler.Handle(new ComplianceFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByCategory.Should().NotBeEmpty();
        result.Value.ByCategory.First().Category.Should().Be("Functional");
        result.Value.ByCategory.First().Total.Should().Be(2);
    }

    // ── TrackPersonaActivity ────────────────────────────────────────────

    [Fact]
    public async Task PersonaActivity_NoAudit_ReturnsEmptyReport()
    {
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();
        defRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ConfigurationDefinition>() as IReadOnlyList<ConfigurationDefinition>);

        var handler = new PersonaActivityFeature.Handler(auditRepo, defRepo);
        var result = await handler.Handle(new PersonaActivityFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalChanges.Should().Be(0);
        result.Value.ByUser.Should().BeEmpty();
        result.Value.ByParameter.Should().BeEmpty();
        result.Value.RecentActivity.Should().BeEmpty();
    }

    [Fact]
    public async Task PersonaActivity_WithSpecificKey_ReturnsFilteredData()
    {
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();

        var entry1 = CreateAuditEntry("param.specific", "user-a");
        var entry2 = CreateAuditEntry("param.specific", "user-b");
        auditRepo.GetByKeyAsync("param.specific", 100, Arg.Any<CancellationToken>())
            .Returns(new[] { entry1, entry2 } as IReadOnlyList<ConfigurationAuditEntry>);

        var handler = new PersonaActivityFeature.Handler(auditRepo, defRepo);
        var result = await handler.Handle(
            new PersonaActivityFeature.Query(Key: "param.specific"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalChanges.Should().Be(2);
        result.Value.ByUser.Should().HaveCount(2);
        result.Value.ByParameter.Should().HaveCount(1);
        result.Value.ByParameter[0].UniqueUsers.Should().Be(2);
    }

    [Fact]
    public async Task PersonaActivity_AllKeys_AggregatesAcrossDefinitions()
    {
        var auditRepo = Substitute.For<IConfigurationAuditRepository>();
        var defRepo = Substitute.For<IConfigurationDefinitionRepository>();

        var def1 = CreateDefinition("param.a");
        var def2 = CreateDefinition("param.b");
        defRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { def1, def2 } as IReadOnlyList<ConfigurationDefinition>);

        auditRepo.GetByKeyAsync("param.a", 10, Arg.Any<CancellationToken>())
            .Returns(new[] { CreateAuditEntry("param.a", "user-1") } as IReadOnlyList<ConfigurationAuditEntry>);
        auditRepo.GetByKeyAsync("param.b", 10, Arg.Any<CancellationToken>())
            .Returns(new[] { CreateAuditEntry("param.b", "user-1"), CreateAuditEntry("param.b", "user-2") } as IReadOnlyList<ConfigurationAuditEntry>);

        var handler = new PersonaActivityFeature.Handler(auditRepo, defRepo);
        var result = await handler.Handle(new PersonaActivityFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalChanges.Should().Be(3);
        result.Value.ByUser.Should().Contain(u => u.UserId == "user-1" && u.ChangeCount == 2);
        result.Value.ByParameter.Should().HaveCount(2);
    }

    // ── Stub helper ─────────────────────────────────────────────────────

    private static ConfigurationEntry CreateEntryStub(string key, ConfigurationScope scope)
    {
        var defId = new ConfigurationDefinitionId(Guid.NewGuid());
        return ConfigurationEntry.Create(
            definitionId: defId,
            key: key,
            scope: scope,
            value: "custom-value",
            createdBy: "test-user",
            scopeReferenceId: scope == ConfigurationScope.System ? null : Guid.NewGuid().ToString());
    }
}
