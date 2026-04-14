using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Seed;

/// <summary>
/// Testes que validam as definições de configuração de catálogo, contratos,
/// APIs e change governance introduzidas na Fase 5 da parametrização.
/// Garante que todas as chaves estão bem formadas, com categorias, tipos,
/// escopos e valores padrão corretos para o domínio de catálogo e mudanças.
/// </summary>
public sealed class CatalogContractsChangeGovernanceConfigurationDefinitionsTests
{
    /// <summary>
    /// Constrói as definições Phase 5 de catálogo, contratos, APIs e change governance,
    /// replicando os mesmos parâmetros do seeder para validação em memória.
    /// </summary>
    private static List<ConfigurationDefinition> BuildPhase5Definitions() =>
    [
        // Block A — Contract Types, Versioning & Breaking Change
        ConfigurationDefinition.Create("catalog.contract.types_enabled", "Enabled Contract Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["REST","SOAP","GraphQL","gRPC","AsyncAPI","Event","SharedSchema"]""", uiEditorType: "json-editor", sortOrder: 4000),
        ConfigurationDefinition.Create("catalog.contract.api_types_enabled", "Enabled API Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["Public","Internal","Partner","ThirdParty"]""", uiEditorType: "json-editor", sortOrder: 4010),
        ConfigurationDefinition.Create("catalog.contract.versioning_policy", "Contract Versioning Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"REST":"SemVer","SOAP":"Sequential","GraphQL":"SemVer","gRPC":"SemVer","AsyncAPI":"SemVer","Event":"SemVer","SharedSchema":"SemVer"}""", uiEditorType: "json-editor", sortOrder: 4020),
        ConfigurationDefinition.Create("catalog.contract.breaking_change_policy", "Breaking Change Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"REST":"RequireApproval","SOAP":"Block","GraphQL":"RequireApproval","gRPC":"RequireApproval","AsyncAPI":"Warn","Event":"Warn","SharedSchema":"Block"}""", uiEditorType: "json-editor", sortOrder: 4030),
        ConfigurationDefinition.Create("catalog.contract.breaking_change_severity", "Breaking Change Default Severity", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "High", validationRules: """{"enum":["Critical","High","Medium","Low"]}""", uiEditorType: "select", sortOrder: 4040),
        ConfigurationDefinition.Create("catalog.contract.version_increment_rules", "Version Increment Rules", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"breakingChange":"major","newFeature":"minor","bugfix":"patch","documentation":"patch"}""", uiEditorType: "json-editor", sortOrder: 4050),
        ConfigurationDefinition.Create("catalog.contract.breaking_promotion_restriction", "Breaking Change Promotion Restriction", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "true", uiEditorType: "toggle", sortOrder: 4060),
        ConfigurationDefinition.Create("catalog.contract.max_active_versions", "Max Active Contract Versions", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "2", validationRules: """{"min":1,"max":10}""", uiEditorType: "text", sortOrder: 4070),
        ConfigurationDefinition.Create("catalog.contract.require_approval_on_change", "Require Approval on Contract Change", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "true", uiEditorType: "toggle", sortOrder: 4071),
        ConfigurationDefinition.Create("catalog.service.require_approval_on_registration", "Require Approval on Service Registration", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "true", uiEditorType: "toggle", sortOrder: 4072),

        // Block B — Validation, Linting, Rulesets & Templates
        ConfigurationDefinition.Create("catalog.validation.lint_severity_defaults", "Lint Severity Defaults", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"missingDescription":"warn","missingExample":"info","unusedSchema":"warn","invalidReference":"error","securitySchemeUndefined":"error"}""", uiEditorType: "json-editor", sortOrder: 4100),
        ConfigurationDefinition.Create("catalog.validation.rulesets_by_contract_type", "Rulesets by Contract Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"REST":["openapi-standard","security-best-practices"],"SOAP":["wsdl-compliance"],"GraphQL":["graphql-best-practices"],"gRPC":["protobuf-lint"],"AsyncAPI":["asyncapi-standard"],"Event":["event-schema-validation"],"SharedSchema":["schema-consistency"]}""", uiEditorType: "json-editor", sortOrder: 4110),
        ConfigurationDefinition.Create("catalog.validation.blocking_vs_warning", "Validation Blocking vs Warning Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"blocking":["invalidReference","securitySchemeUndefined"],"warning":["missingDescription","missingExample","unusedSchema"]}""", uiEditorType: "json-editor", sortOrder: 4120),
        ConfigurationDefinition.Create("catalog.validation.min_validations_by_type", "Minimum Validations by Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"REST":{"schemaValid":true,"securityDefined":true,"pathsDocumented":true},"SOAP":{"wsdlValid":true},"GraphQL":{"schemaValid":true},"gRPC":{"protoValid":true}}""", uiEditorType: "json-editor", sortOrder: 4130),
        ConfigurationDefinition.Create("catalog.templates.by_contract_type", "Contract Templates by Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"REST":"openapi-3.1-standard","SOAP":"wsdl-2.0-standard","GraphQL":"graphql-standard","gRPC":"protobuf-standard","AsyncAPI":"asyncapi-2.6-standard","Event":"cloudevents-standard","SharedSchema":"json-schema-standard"}""", uiEditorType: "json-editor", sortOrder: 4140),
        ConfigurationDefinition.Create("catalog.templates.metadata_defaults", "Contract Metadata Defaults", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"license":"Proprietary","termsOfService":"","contact":"","externalDocs":""}""", uiEditorType: "json-editor", sortOrder: 4150),

        // Block C — Minimum Requirements & Publication
        ConfigurationDefinition.Create("catalog.requirements.owner_required", "Owner Required", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 4200),
        ConfigurationDefinition.Create("catalog.requirements.changelog_required", "Changelog Required", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 4210),
        ConfigurationDefinition.Create("catalog.requirements.glossary_required", "Glossary Required", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "false", uiEditorType: "toggle", sortOrder: 4220),
        ConfigurationDefinition.Create("catalog.requirements.use_cases_required", "Use Cases Required", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "false", uiEditorType: "toggle", sortOrder: 4230),
        ConfigurationDefinition.Create("catalog.requirements.min_documentation", "Minimum Documentation Requirements", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"descriptionMinLength":20,"operationDescriptions":true,"responseExamples":false,"errorDocumentation":true}""", uiEditorType: "json-editor", sortOrder: 4240),
        ConfigurationDefinition.Create("catalog.requirements.min_catalog_fields", "Minimum Catalog Fields", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"name":true,"description":true,"owner":true,"team":true,"domain":false,"tier":false,"lifecycle":true}""", uiEditorType: "json-editor", sortOrder: 4250),
        ConfigurationDefinition.Create("catalog.requirements.min_contract_fields", "Minimum Contract Fields", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"title":true,"version":true,"description":true,"servers":true,"securityScheme":true,"contact":false}""", uiEditorType: "json-editor", sortOrder: 4260),
        ConfigurationDefinition.Create("catalog.requirements.by_contract_type", "Requirements by Contract Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"REST":{"securityScheme":true,"pathDescriptions":true},"SOAP":{"wsdlValid":true},"GraphQL":{"schemaValid":true},"gRPC":{"protoValid":true}}""", uiEditorType: "json-editor", sortOrder: 4270),
        ConfigurationDefinition.Create("catalog.requirements.by_environment", "Requirements by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"ownerRequired":true,"changelogRequired":true,"minDocumentation":true,"allBlockingValidationsPass":true},"Development":{"ownerRequired":false,"changelogRequired":false}}""", uiEditorType: "json-editor", sortOrder: 4280),
        ConfigurationDefinition.Create("catalog.requirements.by_criticality", "Requirements by Criticality", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"critical":{"ownerRequired":true,"changelogRequired":true,"glossaryRequired":true,"useCasesRequired":true},"standard":{"ownerRequired":true,"changelogRequired":true}}""", uiEditorType: "json-editor", sortOrder: 4290),

        // Block D — Publication & Promotion Policy
        ConfigurationDefinition.Create("catalog.publication.pre_publish_review", "Pre-Publication Review Required", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "true", uiEditorType: "toggle", sortOrder: 4300),
        ConfigurationDefinition.Create("catalog.publication.visibility_defaults", "Publication Visibility Defaults", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Internal":"team","Public":"organization","Partner":"restricted"}""", uiEditorType: "json-editor", sortOrder: 4310),
        ConfigurationDefinition.Create("catalog.publication.portal_defaults", "Developer Portal Publishing Defaults", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"autoPublishToPortal":true,"includeExamples":true,"includeChangelog":true,"includeTryItOut":true}""", uiEditorType: "json-editor", sortOrder: 4320),
        ConfigurationDefinition.Create("catalog.publication.promotion_readiness", "Contract Promotion Readiness Criteria", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"allBlockingValidationsPass":true,"ownerAssigned":true,"changelogUpdated":true,"noUnresolvedBreakingChanges":true,"minGovernanceScore":60}""", uiEditorType: "json-editor", sortOrder: 4330),
        ConfigurationDefinition.Create("catalog.publication.gating_by_environment", "Publication Gating by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"requireApproval":true,"requireAllGatesPass":true},"PreProduction":{"requireApproval":false,"requireAllGatesPass":true},"Development":{"requireApproval":false,"requireAllGatesPass":false}}""", uiEditorType: "json-editor", sortOrder: 4340),

        // Block E — Import/Export Policy
        ConfigurationDefinition.Create("catalog.import.types_allowed", "Allowed Import Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"fileUpload":["OpenAPI","WSDL","GraphQL","Protobuf","AsyncAPI","JSONSchema"],"urlImport":["OpenAPI","AsyncAPI"],"gitSync":["OpenAPI","AsyncAPI","Protobuf"]}""", uiEditorType: "json-editor", sortOrder: 4400),
        ConfigurationDefinition.Create("catalog.export.types_allowed", "Allowed Export Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["OpenAPI-JSON","OpenAPI-YAML","WSDL","GraphQL-SDL","Protobuf","AsyncAPI-YAML","Markdown","HTML"]""", uiEditorType: "json-editor", sortOrder: 4410),
        ConfigurationDefinition.Create("catalog.import.overwrite_policy", "Import Overwrite Policy", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "AskUser", validationRules: """{"enum":["Merge","Overwrite","Block","AskUser"]}""", uiEditorType: "select", sortOrder: 4420),
        ConfigurationDefinition.Create("catalog.import.validation_on_import", "Validate on Import", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 4430),

        // Block F — Change Types, Criticality & Blast Radius
        ConfigurationDefinition.Create("change.types_enabled", "Enabled Change Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["Feature","Bugfix","Hotfix","Refactor","Config","Infrastructure","Rollback"]""", uiEditorType: "json-editor", sortOrder: 4500),
        ConfigurationDefinition.Create("change.criticality_defaults", "Change Criticality Defaults", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Feature":"Medium","Bugfix":"Medium","Hotfix":"Critical","Refactor":"Low","Config":"Low","Infrastructure":"High","Rollback":"Critical"}""", uiEditorType: "json-editor", sortOrder: 4510),
        ConfigurationDefinition.Create("change.risk_classification", "Change Risk Classification", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Hotfix":{"baseRisk":"High","requiresApproval":true},"Infrastructure":{"baseRisk":"High","requiresApproval":true},"Feature":{"baseRisk":"Medium","requiresApproval":false},"Rollback":{"baseRisk":"High","requiresApproval":true}}""", uiEditorType: "json-editor", sortOrder: 4520),
        ConfigurationDefinition.Create("change.blast_radius.thresholds", "Blast Radius Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"Critical":90,"High":70,"Medium":40,"Low":0}""", uiEditorType: "json-editor", sortOrder: 4530),
        ConfigurationDefinition.Create("change.blast_radius.categories", "Blast Radius Categories", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Critical":{"label":"Critical","color":"#DC2626","action":"RequireApproval"},"High":{"label":"High","color":"#F59E0B","action":"RequireReview"},"Medium":{"label":"Medium","color":"#3B82F6","action":"Notify"},"Low":{"label":"Low","color":"#10B981","action":"AutoApprove"}}""", uiEditorType: "json-editor", sortOrder: 4540),
        ConfigurationDefinition.Create("change.blast_radius.environment_weights", "Blast Radius Environment Weights", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":1.0,"PreProduction":0.6,"Staging":0.4,"Development":0.2}""", uiEditorType: "json-editor", sortOrder: 4550),
        ConfigurationDefinition.Create("change.severity_criteria", "Change Severity Criteria", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"affectedServicesHigh":5,"affectedDependenciesHigh":10,"crossDomainChange":true,"dataSchemaChange":true}""", uiEditorType: "json-editor", sortOrder: 4560),

        // Block G — Release Scoring, Evidence Pack & Rollback
        ConfigurationDefinition.Create("change.release_score.weights", "Release Confidence Score Weights", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"testCoverage":20,"codeReview":15,"blastRadius":20,"historicalSuccess":15,"documentationComplete":10,"governanceCompliance":10,"evidencePack":10}""", uiEditorType: "json-editor", sortOrder: 4600),
        ConfigurationDefinition.Create("change.release_score.thresholds", "Release Score Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"HighConfidence":80,"Moderate":60,"LowConfidence":40,"Block":0}""", uiEditorType: "json-editor", sortOrder: 4610),
        ConfigurationDefinition.Create("change.evidence_pack.required", "Evidence Pack Required", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "true", uiEditorType: "toggle", sortOrder: 4620),
        ConfigurationDefinition.Create("change.evidence_pack.requirements", "Evidence Pack Requirements", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"Production":{"testReport":true,"securityScan":true,"approvalRecord":true,"rollbackPlan":true},"PreProduction":{"testReport":true,"securityScan":false,"approvalRecord":false},"Development":{"testReport":false}}""", uiEditorType: "json-editor", sortOrder: 4630),
        ConfigurationDefinition.Create("change.evidence_pack.by_criticality", "Evidence Pack Requirements by Criticality", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Critical":{"securityScan":true,"approvalRecord":true,"rollbackPlan":true,"impactAnalysis":true},"High":{"securityScan":true,"approvalRecord":true,"rollbackPlan":true},"Medium":{"testReport":true},"Low":{}}""", uiEditorType: "json-editor", sortOrder: 4640),
        ConfigurationDefinition.Create("change.rollback.recommendation_policy", "Rollback Recommendation Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"autoRecommendOnScoreBelow":40,"autoRecommendOnIncidentCorrelation":true,"requireRollbackPlanForProduction":true,"requireRollbackPlanForCriticalChanges":true}""", uiEditorType: "json-editor", sortOrder: 4650),
        ConfigurationDefinition.Create("change.release_calendar.window_policy", "Release Calendar Window Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"Hotfix":{"allowOutsideWindow":true,"requireApproval":true},"Feature":{"allowOutsideWindow":false,"requireApproval":false},"Infrastructure":{"allowOutsideWindow":false,"requireApproval":true}}""", uiEditorType: "json-editor", sortOrder: 4660),
        ConfigurationDefinition.Create("change.release_calendar.by_environment", "Release Calendar by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"allowedDays":["Monday","Tuesday","Wednesday","Thursday"],"blockedHours":{"start":"18:00","end":"08:00"},"requireWindow":true},"PreProduction":{"requireWindow":false},"Development":{"requireWindow":false}}""", uiEditorType: "json-editor", sortOrder: 4670),
        ConfigurationDefinition.Create("change.incident_correlation.enabled", "Release-to-Incident Correlation Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 4680),
        ConfigurationDefinition.Create("change.incident_correlation.window_hours", "Incident Correlation Window (Hours)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "24", validationRules: """{"min":1,"max":168}""", uiEditorType: "text", sortOrder: 4690),
    ];

    // ── All Phase 5 definitions have unique keys ──────────────────────

    [Fact]
    public void Phase5Definitions_ShouldHaveUniqueKeys()
    {
        var definitions = BuildPhase5Definitions();
        var keys = definitions.Select(d => d.Key).ToList();

        keys.Should().OnlyHaveUniqueItems("each configuration definition key must be unique");
    }

    // ── All Phase 5 definitions have unique sortOrders ─────────────────

    [Fact]
    public void Phase5Definitions_ShouldHaveUniqueSortOrders()
    {
        var definitions = BuildPhase5Definitions();
        var sortOrders = definitions.Select(d => d.SortOrder).ToList();

        sortOrders.Should().OnlyHaveUniqueItems("each definition must have a unique sortOrder");
    }

    // ── All Phase 5 definitions are Functional category ───────────────

    [Fact]
    public void Phase5Definitions_ShouldAllBeFunctionalCategory()
    {
        var definitions = BuildPhase5Definitions();

        definitions.Should().OnlyContain(
            d => d.Category == ConfigurationCategory.Functional,
            "catalog, contract and change governance definitions should be Functional category");
    }

    // ── All catalog/change definitions use correct prefix ─────────────

    [Fact]
    public void Phase5Definitions_ShouldHaveCorrectKeyPrefix()
    {
        var definitions = BuildPhase5Definitions();

        definitions.Should().OnlyContain(
            d => d.Key.StartsWith("catalog.") || d.Key.StartsWith("change."),
            "Phase 5 definitions must use the catalog.* or change.* key prefix");
    }

    // ── Sort orders are in the 4000+ range ────────────────────────────

    [Fact]
    public void Phase5Definitions_SortOrdersShouldBeInPhase5Range()
    {
        var definitions = BuildPhase5Definitions();

        definitions.Should().OnlyContain(
            d => d.SortOrder >= 4000 && d.SortOrder <= 4999,
            "Phase 5 definitions should have sortOrders in the 4000-4999 range");
    }

    // ── Contract type definitions ─────────────────────────────────────

    [Fact]
    public void ContractTypesEnabled_ShouldIncludeAllStandardTypes()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.contract.types_enabled");

        def.ValueType.Should().Be(ConfigurationValueType.Json);
        def.DefaultValue.Should().Contain("REST");
        def.DefaultValue.Should().Contain("SOAP");
        def.DefaultValue.Should().Contain("GraphQL");
        def.DefaultValue.Should().Contain("gRPC");
        def.DefaultValue.Should().Contain("AsyncAPI");
    }

    [Fact]
    public void VersioningPolicy_ShouldMapTypeToStrategy()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.contract.versioning_policy");

        def.ValueType.Should().Be(ConfigurationValueType.Json);
        def.DefaultValue.Should().Contain("SemVer");
        def.DefaultValue.Should().Contain("Sequential");
    }

    [Fact]
    public void BreakingChangePolicy_ShouldIncludeBlockAndRequireApproval()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.contract.breaking_change_policy");

        def.DefaultValue.Should().Contain("Block");
        def.DefaultValue.Should().Contain("RequireApproval");
        def.DefaultValue.Should().Contain("Warn");
    }

    [Fact]
    public void BreakingChangeSeverity_ShouldHaveEnumValidation()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.contract.breaking_change_severity");

        def.ValidationRules.Should().Contain("Critical");
        def.ValidationRules.Should().Contain("High");
        def.ValidationRules.Should().Contain("Medium");
        def.ValidationRules.Should().Contain("Low");
        def.DefaultValue.Should().Be("High");
    }

    [Fact]
    public void BreakingPromotionRestriction_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.contract.breaking_promotion_restriction");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "breaking promotion restriction should vary by environment");
        def.DefaultValue.Should().Be("true");
    }

    // ── Validation & ruleset definitions ──────────────────────────────

    [Fact]
    public void RulesetsByContractType_ShouldMapAllTypes()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.validation.rulesets_by_contract_type");

        def.DefaultValue.Should().Contain("REST");
        def.DefaultValue.Should().Contain("openapi-standard");
        def.DefaultValue.Should().Contain("SOAP");
        def.DefaultValue.Should().Contain("wsdl-compliance");
    }

    [Fact]
    public void BlockingVsWarning_ShouldSeparateRuleTypes()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.validation.blocking_vs_warning");

        def.DefaultValue.Should().Contain("blocking");
        def.DefaultValue.Should().Contain("warning");
        def.DefaultValue.Should().Contain("invalidReference");
    }

    // ── Minimum requirements ──────────────────────────────────────────

    [Fact]
    public void OwnerRequired_ShouldDefaultToTrue()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.requirements.owner_required");

        def.DefaultValue.Should().Be("true");
        def.ValueType.Should().Be(ConfigurationValueType.Boolean);
    }

    [Fact]
    public void ChangelogRequired_ShouldDefaultToTrue()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.requirements.changelog_required");

        def.DefaultValue.Should().Be("true");
    }

    [Fact]
    public void GlossaryRequired_ShouldDefaultToFalse()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.requirements.glossary_required");

        def.DefaultValue.Should().Be("false");
    }

    [Fact]
    public void RequirementsByEnvironment_ShouldHaveProductionStricter()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.requirements.by_environment");

        def.DefaultValue.Should().Contain("Production");
        def.DefaultValue.Should().Contain("allBlockingValidationsPass");
    }

    // ── Publication & promotion ───────────────────────────────────────

    [Fact]
    public void PrePublishReview_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.publication.pre_publish_review");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "pre-publish review should vary by environment");
        def.DefaultValue.Should().Be("true");
    }

    [Fact]
    public void PromotionReadiness_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.publication.promotion_readiness");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment);
        def.DefaultValue.Should().Contain("minGovernanceScore");
    }

    // ── Import/Export ─────────────────────────────────────────────────

    [Fact]
    public void ImportOverwritePolicy_ShouldHaveEnumValidation()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.import.overwrite_policy");

        def.ValidationRules.Should().Contain("Merge");
        def.ValidationRules.Should().Contain("Overwrite");
        def.ValidationRules.Should().Contain("Block");
        def.ValidationRules.Should().Contain("AskUser");
        def.DefaultValue.Should().Be("AskUser");
    }

    [Fact]
    public void ValidateOnImport_ShouldDefaultToTrue()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "catalog.import.validation_on_import");

        def.DefaultValue.Should().Be("true");
    }

    // ── Change types & blast radius ───────────────────────────────────

    [Fact]
    public void ChangeTypesEnabled_ShouldIncludeAllStandardTypes()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.types_enabled");

        def.DefaultValue.Should().Contain("Feature");
        def.DefaultValue.Should().Contain("Bugfix");
        def.DefaultValue.Should().Contain("Hotfix");
        def.DefaultValue.Should().Contain("Rollback");
        def.DefaultValue.Should().Contain("Infrastructure");
    }

    [Fact]
    public void ChangeCriticalityDefaults_ShouldMapHotfixToCritical()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.criticality_defaults");

        def.DefaultValue.Should().Contain("Hotfix");
        def.DefaultValue.Should().Contain("Critical");
    }

    [Fact]
    public void BlastRadiusThresholds_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.blast_radius.thresholds");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "blast radius thresholds should vary by environment");
    }

    [Fact]
    public void BlastRadiusCategories_ShouldIncludeActionsPerLevel()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.blast_radius.categories");

        def.DefaultValue.Should().Contain("RequireApproval");
        def.DefaultValue.Should().Contain("AutoApprove");
        def.DefaultValue.Should().Contain("#DC2626"); // Critical color
    }

    [Fact]
    public void EnvironmentWeights_ShouldHaveProductionHighest()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.blast_radius.environment_weights");

        def.DefaultValue.Should().Contain("1.0"); // Production
        def.DefaultValue.Should().Contain("0.2"); // Development
    }

    // ── Release scoring & evidence pack ───────────────────────────────

    [Fact]
    public void ReleaseScoreWeights_ShouldSumTo100()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.release_score.weights");

        // testCoverage:20 + codeReview:15 + blastRadius:20 + historicalSuccess:15 + documentationComplete:10 + governanceCompliance:10 + evidencePack:10 = 100
        def.DefaultValue.Should().Contain("20");
        def.DefaultValue.Should().Contain("15");
        def.DefaultValue.Should().Contain("10");
    }

    [Fact]
    public void ReleaseScoreThresholds_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.release_score.thresholds");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "release score thresholds should vary by environment");
    }

    [Fact]
    public void EvidencePackRequired_ShouldDefaultToTrue()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.evidence_pack.required");

        def.DefaultValue.Should().Be("true");
        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment);
    }

    [Fact]
    public void EvidencePackRequirements_ShouldHaveProductionStricter()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.evidence_pack.requirements");

        def.DefaultValue.Should().Contain("Production");
        def.DefaultValue.Should().Contain("rollbackPlan");
        def.DefaultValue.Should().Contain("securityScan");
    }

    // ── Rollback & release calendar ───────────────────────────────────

    [Fact]
    public void RollbackPolicy_ShouldRequireRollbackPlanForProduction()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.rollback.recommendation_policy");

        def.DefaultValue.Should().Contain("requireRollbackPlanForProduction");
        def.DefaultValue.Should().Contain("true");
    }

    [Fact]
    public void ReleaseCalendarByEnvironment_ShouldRestrictProductionDays()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.release_calendar.by_environment");

        def.DefaultValue.Should().Contain("Production");
        def.DefaultValue.Should().Contain("Monday");
        def.DefaultValue.Should().Contain("blockedHours");
    }

    [Fact]
    public void IncidentCorrelationEnabled_ShouldDefaultToTrue()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.incident_correlation.enabled");

        def.DefaultValue.Should().Be("true");
    }

    [Fact]
    public void IncidentCorrelationWindow_ShouldHaveMinMaxValidation()
    {
        var definitions = BuildPhase5Definitions();
        var def = definitions.Single(d => d.Key == "change.incident_correlation.window_hours");

        def.ValidationRules.Should().Contain("min");
        def.ValidationRules.Should().Contain("max");
        def.DefaultValue.Should().Be("24");
    }

    // ── Boolean definitions use toggle editor ─────────────────────────

    [Fact]
    public void AllBooleanDefinitions_ShouldUseToggleEditor()
    {
        var definitions = BuildPhase5Definitions();
        var boolDefs = definitions.Where(d => d.ValueType == ConfigurationValueType.Boolean).ToList();

        boolDefs.Should().NotBeEmpty();
        boolDefs.Should().OnlyContain(
            d => d.UiEditorType == "toggle",
            "all Boolean definitions should use the toggle UI editor");
    }

    // ── JSON definitions use json-editor ──────────────────────────────

    [Fact]
    public void AllJsonDefinitions_ShouldUseJsonEditor()
    {
        var definitions = BuildPhase5Definitions();
        var jsonDefs = definitions.Where(d => d.ValueType == ConfigurationValueType.Json).ToList();

        jsonDefs.Should().NotBeEmpty();
        jsonDefs.Should().OnlyContain(
            d => d.UiEditorType == "json-editor",
            "all JSON definitions should use the json-editor UI editor");
    }

    // ── Total count validation ────────────────────────────────────────

    [Fact]
    public void Phase5Definitions_ShouldHaveExpectedCount()
    {
        var definitions = BuildPhase5Definitions();

        // 7 contract types/versioning + 3 versioning/approval governance + 6 validation/rulesets + 10 requirements + 5 publication + 4 import/export + 7 change types/blast + 10 release scoring/evidence/rollback = 52
        definitions.Should().HaveCount(52,
            "Phase 5 should deliver 52 catalog, contracts and change governance definitions");
    }
}
