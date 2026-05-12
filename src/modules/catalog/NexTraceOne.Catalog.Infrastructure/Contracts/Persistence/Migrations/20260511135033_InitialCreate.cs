using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_contract_compliance_gates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Rules = table.Column<string>(type: "jsonb", nullable: true),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BlockOnViolation = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_compliance_gates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_compliance_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChangeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Violations = table.Column<string>(type: "jsonb", nullable: true),
                    EvidencePackId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_compliance_results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_health_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    BreakingChangeFrequencyScore = table.Column<int>(type: "integer", nullable: false),
                    ConsumerImpactScore = table.Column<int>(type: "integer", nullable: false),
                    ReviewRecencyScore = table.Column<int>(type: "integer", nullable: false),
                    ExampleCoverageScore = table.Column<int>(type: "integer", nullable: false),
                    PolicyComplianceScore = table.Column<int>(type: "integer", nullable: false),
                    DocumentationScore = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDegraded = table.Column<bool>(type: "boolean", nullable: false),
                    DegradationThreshold = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_health_scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: true),
                    ConsumerCount = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    TotalReviews = table.Column<int>(type: "integer", nullable: false),
                    IsPromoted = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PublishedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_listings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_negotiations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedByTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedByTeamName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Deadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Participants = table.Column<string>(type: "jsonb", nullable: false),
                    ParticipantCount = table.Column<int>(type: "integer", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false),
                    ProposedContractSpec = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InitiatedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_negotiations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_impact_simulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scenario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScenarioDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AffectedServices = table.Column<string>(type: "jsonb", nullable: true),
                    BrokenConsumers = table.Column<string>(type: "jsonb", nullable: true),
                    TransitiveCascadeDepth = table.Column<int>(type: "integer", nullable: false),
                    RiskPercent = table.Column<int>(type: "integer", nullable: false),
                    MitigationRecommendations = table.Column<string>(type: "jsonb", nullable: true),
                    SimulatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_impact_simulations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_negotiation_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NegotiationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuthorDisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    LineReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_negotiation_comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_pipeline_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContractVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestedStages = table.Column<string>(type: "jsonb", nullable: false),
                    StageResults = table.Column<string>(type: "jsonb", nullable: true),
                    GeneratedArtifacts = table.Column<string>(type: "jsonb", nullable: true),
                    TargetLanguage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetFramework = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalStages = table.Column<int>(type: "integer", nullable: false),
                    CompletedStages = table.Column<int>(type: "integer", nullable: false),
                    FailedStages = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    InitiatedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_pipeline_executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_schema_evolution_advices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompatibilityLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompatibilityScore = table.Column<int>(type: "integer", nullable: false),
                    FieldsAdded = table.Column<string>(type: "jsonb", nullable: true),
                    FieldsRemoved = table.Column<string>(type: "jsonb", nullable: true),
                    FieldsModified = table.Column<string>(type: "jsonb", nullable: true),
                    FieldsInUseByConsumers = table.Column<string>(type: "jsonb", nullable: true),
                    AffectedConsumers = table.Column<string>(type: "jsonb", nullable: true),
                    AffectedConsumerCount = table.Column<int>(type: "integer", nullable: false),
                    RecommendedStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StrategyDetails = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    Warnings = table.Column<string>(type: "jsonb", nullable: true),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AnalyzedByAgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_schema_evolution_advices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_semantic_diff_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionFromId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContractVersionToId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NaturalLanguageSummary = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Classification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AffectedConsumers = table.Column<string>(type: "jsonb", nullable: true),
                    MitigationSuggestions = table.Column<string>(type: "jsonb", nullable: true),
                    CompatibilityScore = table.Column<int>(type: "integer", nullable: false),
                    GeneratedByModel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_semantic_diff_results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_background_service_contract_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScheduleExpression = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TriggerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InputsJson = table.Column<string>(type: "text", nullable: false),
                    OutputsJson = table.Column<string>(type: "text", nullable: false),
                    SideEffectsJson = table.Column<string>(type: "text", nullable: false),
                    TimeoutExpression = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AllowsConcurrency = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MessagingRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "None"),
                    ConsumedTopicsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    ProducedTopicsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    ConsumedServicesJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    ProducedEventsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_background_service_contract_details", x => x.Id);
                    table.CheckConstraint("CK_ctr_bg_service_details_messaging_role", "\"MessagingRole\" IN ('None', 'Producer', 'Consumer', 'Both')");
                    table.CheckConstraint("CK_ctr_bg_service_details_trigger_type", "\"TriggerType\" IN ('Cron', 'Interval', 'EventTriggered', 'OnDemand', 'Continuous')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_background_service_draft_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduleExpression = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InputsJson = table.Column<string>(type: "text", nullable: false),
                    OutputsJson = table.Column<string>(type: "text", nullable: false),
                    SideEffectsJson = table.Column<string>(type: "text", nullable: false),
                    MessagingRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "None"),
                    ConsumedTopicsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    ProducedTopicsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    ConsumedServicesJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    ProducedEventsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_background_service_draft_metadata", x => x.Id);
                    table.CheckConstraint("CK_ctr_bg_service_draft_messaging_role", "\"MessagingRole\" IN ('None', 'Producer', 'Consumer', 'Both')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_breaking_change_proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProposedBreakingChangesJson = table.Column<string>(type: "jsonb", nullable: false),
                    MigrationWindowDays = table.Column<int>(type: "integer", nullable: false),
                    DeprecationPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConsultationOpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecisionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_breaking_change_proposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_canonical_entities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Domain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    SchemaFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Aliases = table.Column<string[]>(type: "text[]", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReusePolicy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    KnownUsageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_canonical_entities", x => x.Id);
                    table.CheckConstraint("CK_ctr_canonical_entities_state", "\"State\" IN ('Draft', 'Published', 'Deprecated', 'Retired')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_canonical_entity_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    SchemaFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PublishedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_canonical_entity_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_consumer_expectations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConsumerDomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpectedSubsetJson = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_consumer_expectations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_changelogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiAssetId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FromVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ToVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Entries = table.Column<string>(type: "jsonb", nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MarkdownContent = table.Column<string>(type: "text", nullable: true),
                    JsonContent = table.Column<string>(type: "text", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ApprovedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CommitSha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_changelogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_consumer_inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerService = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConsumerEnvironment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FrequencyPerDay = table.Column<double>(type: "double precision", nullable: false),
                    LastCalledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FirstCalledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_consumer_inventory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_deployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    DeployedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeployedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_deployments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_drafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "OpenApi"),
                    SpecContent = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ProposedVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Editing"),
                    Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseContractVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AiGenerationPrompt = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    LastEditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastEditedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ServiceInterfaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_drafts", x => x.Id);
                    table.CheckConstraint("CK_ctr_contract_drafts_status", "\"Status\" IN ('Editing', 'InReview', 'Approved', 'Rejected', 'Published', 'Discarded')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiAssetId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpecContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BreakingChangesCount = table.Column<int>(type: "integer", nullable: false),
                    NonBreakingChangesCount = table.Column<int>(type: "integer", nullable: false),
                    AdditiveChangesCount = table.Column<int>(type: "integer", nullable: false),
                    DiffDetails = table.Column<string>(type: "jsonb", nullable: false),
                    ComplianceViolations = table.Column<string>(type: "jsonb", nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceBranch = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CommitSha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PipelineId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EnvironmentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_verifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SpecContent = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "OpenApi"),
                    LifecycleState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    ImportedFrom = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignatureFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SignatureAlgorithm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SignedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProvenanceOrigin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProvenanceOriginalFormat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProvenanceParserUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProvenanceStandardVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProvenanceImportedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProvenanceIsAiGenerated = table.Column<bool>(type: "boolean", nullable: true),
                    ProvenanceAiModelVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeprecationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SunsetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecationNotice = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SlaAvailabilityTarget = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    SlaLatencyP99Ms = table.Column<int>(type: "integer", nullable: true),
                    SlaLatencyP95Ms = table.Column<int>(type: "integer", nullable: true),
                    SlaMaxErrorRatePercent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    SlaMinThroughputRps = table.Column<int>(type: "integer", nullable: true),
                    SlaMaintenanceWindowMinutes = table.Column<int>(type: "integer", nullable: true),
                    SlaTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SlaDocumentReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastOverallScore = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_versions", x => x.Id);
                    table.CheckConstraint("CK_ctr_contract_versions_lifecycle_state", "\"LifecycleState\" IN ('Draft', 'InReview', 'Approved', 'Locked', 'Deprecated', 'Sunset', 'Retired')");
                    table.CheckConstraint("CK_ctr_contract_versions_protocol", "\"Protocol\" IN ('OpenApi', 'Swagger', 'Wsdl', 'AsyncApi', 'Protobuf', 'GraphQL')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_data_contract_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DatasetName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContractVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FreshnessRequirementHours = table.Column<int>(type: "integer", nullable: true),
                    FieldDefinitionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    OwnerTeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_data_contract_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_data_contract_schemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SlaFreshnessHours = table.Column<int>(type: "integer", nullable: false),
                    SchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                    PiiClassification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColumnCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_data_contract_schemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_deprecation_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PlannedDeprecationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlannedSunsetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MigrationGuideUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SuccessorVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotificationDraftMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ScheduledByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_deprecation_schedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_event_contract_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AsyncApiVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DefaultContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChannelsJson = table.Column<string>(type: "text", nullable: false),
                    MessagesJson = table.Column<string>(type: "text", nullable: false),
                    ServersJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_event_contract_details", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_event_draft_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AsyncApiVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DefaultContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChannelsJson = table.Column<string>(type: "text", nullable: false),
                    MessagesJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_event_draft_metadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_feature_flag_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FlagKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EnabledEnvironmentsJson = table.Column<string>(type: "jsonb", nullable: true),
                    OwnerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastToggledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScheduledRemovalDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_feature_flag_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_graphql_schema_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    TypeCount = table.Column<int>(type: "integer", nullable: false),
                    FieldCount = table.Column<int>(type: "integer", nullable: false),
                    OperationCount = table.Column<int>(type: "integer", nullable: false),
                    TypeNamesJson = table.Column<string>(type: "text", nullable: false),
                    OperationsJson = table.Column<string>(type: "text", nullable: false),
                    FieldsByTypeJson = table.Column<string>(type: "text", nullable: false),
                    HasQueryType = table.Column<bool>(type: "boolean", nullable: false),
                    HasMutationType = table.Column<bool>(type: "boolean", nullable: false),
                    HasSubscriptionType = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_graphql_schema_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_protobuf_schema_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    FieldCount = table.Column<int>(type: "integer", nullable: false),
                    ServiceCount = table.Column<int>(type: "integer", nullable: false),
                    RpcCount = table.Column<int>(type: "integer", nullable: false),
                    MessageNamesJson = table.Column<string>(type: "text", nullable: false),
                    FieldsByMessageJson = table.Column<string>(type: "text", nullable: false),
                    RpcsByServiceJson = table.Column<string>(type: "text", nullable: false),
                    Syntax = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_protobuf_schema_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_sbom_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Components = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_sbom_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_soap_contract_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetNamespace = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SoapVersion = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    EndpointUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WsdlSourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PortTypeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BindingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExtractedOperationsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_soap_contract_details", x => x.Id);
                    table.CheckConstraint("CK_ctr_soap_contract_details_soap_version", "\"SoapVersion\" IN ('1.1', '1.2')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_soap_draft_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetNamespace = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SoapVersion = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    EndpointUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PortTypeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BindingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OperationsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_soap_draft_metadata", x => x.Id);
                    table.CheckConstraint("CK_ctr_soap_draft_metadata_soap_version", "\"SoapVersion\" IN ('1.1', '1.2')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_spectral_rulesets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Origin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultExecutionMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnforcementBehavior = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Domain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApplicableServiceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApplicableProtocols = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_spectral_rulesets", x => x.Id);
                    table.CheckConstraint("CK_ctr_spectral_rulesets_origin", "\"Origin\" IN ('Platform', 'Organization', 'Team', 'Imported')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_examples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExampleType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_examples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_examples_ctr_contract_drafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "ctr_contract_drafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_artifacts_ctr_contract_versions_ContractVersio~",
                        column: x => x.ContractVersionId,
                        principalTable: "ctr_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_diffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "OpenApi"),
                    ChangeLevel = table.Column<int>(type: "integer", nullable: false),
                    BreakingChanges = table.Column<string>(type: "text", nullable: false),
                    NonBreakingChanges = table.Column<string>(type: "text", nullable: false),
                    AdditiveChanges = table.Column<string>(type: "text", nullable: false),
                    SuggestedSemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.8m),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_diffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_diffs_ctr_contract_versions_ContractVersionId",
                        column: x => x.ContractVersionId,
                        principalTable: "ctr_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_evidence_packs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangeLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    AdditiveChangeCount = table.Column<int>(type: "integer", nullable: false),
                    NonBreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    RecommendedVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RuleViolationCount = table.Column<int>(type: "integer", nullable: false),
                    RequiresWorkflowApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresChangeNotification = table.Column<bool>(type: "boolean", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TechnicalSummary = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ImpactedConsumers = table.Column<string[]>(type: "text[]", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_evidence_packs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_evidence_packs_ctr_contract_versions_ContractV~",
                        column: x => x.ContractVersionId,
                        principalTable: "ctr_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_rule_violations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RulesetId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SuggestedFix = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_rule_violations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_rule_violations_ctr_contract_versions_Contract~",
                        column: x => x.ContractVersionId,
                        principalTable: "ctr_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_scorecards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QualityScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CompletenessScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CompatibilityScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    OperationCount = table.Column<int>(type: "integer", nullable: false),
                    SchemaCount = table.Column<int>(type: "integer", nullable: false),
                    HasSecurityDefinitions = table.Column<bool>(type: "boolean", nullable: false),
                    HasExamples = table.Column<bool>(type: "boolean", nullable: false),
                    HasDescriptions = table.Column<bool>(type: "boolean", nullable: false),
                    QualityJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CompletenessJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CompatibilityJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RiskJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_scorecards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ctr_contract_scorecards_ctr_contract_versions_ContractVersi~",
                        column: x => x.ContractVersionId,
                        principalTable: "ctr_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_gates_IsActive",
                table: "cat_contract_compliance_gates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_gates_Scope",
                table: "cat_contract_compliance_gates",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_gates_Scope_ScopeId_IsActive",
                table: "cat_contract_compliance_gates",
                columns: new[] { "Scope", "ScopeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_gates_ScopeId",
                table: "cat_contract_compliance_gates",
                column: "ScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_gates_TenantId",
                table: "cat_contract_compliance_gates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_ChangeId",
                table: "cat_contract_compliance_results",
                column: "ChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_ContractVersionId",
                table: "cat_contract_compliance_results",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_EvaluatedAt",
                table: "cat_contract_compliance_results",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_GateId",
                table: "cat_contract_compliance_results",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_Result",
                table: "cat_contract_compliance_results",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_compliance_results_TenantId",
                table: "cat_contract_compliance_results",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_health_scores_ApiAssetId",
                table: "cat_contract_health_scores",
                column: "ApiAssetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_health_scores_IsDegraded",
                table: "cat_contract_health_scores",
                column: "IsDegraded");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_health_scores_OverallScore",
                table: "cat_contract_health_scores",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_listings_Category",
                table: "cat_contract_listings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_listings_ContractId",
                table: "cat_contract_listings",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_listings_IsPromoted",
                table: "cat_contract_listings",
                column: "IsPromoted");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_listings_PublishedAt",
                table: "cat_contract_listings",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_listings_Status",
                table: "cat_contract_listings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_negotiations_ContractId",
                table: "cat_contract_negotiations",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_negotiations_CreatedAt",
                table: "cat_contract_negotiations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_negotiations_ProposedByTeamId",
                table: "cat_contract_negotiations",
                column: "ProposedByTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_negotiations_Status",
                table: "cat_contract_negotiations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_reviews_AuthorId",
                table: "cat_contract_reviews",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_reviews_ListingId",
                table: "cat_contract_reviews",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_reviews_ReviewedAt",
                table: "cat_contract_reviews",
                column: "ReviewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_impact_simulations_RiskPercent",
                table: "cat_impact_simulations",
                column: "RiskPercent");

            migrationBuilder.CreateIndex(
                name: "IX_cat_impact_simulations_Scenario",
                table: "cat_impact_simulations",
                column: "Scenario");

            migrationBuilder.CreateIndex(
                name: "IX_cat_impact_simulations_ServiceName",
                table: "cat_impact_simulations",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_cat_impact_simulations_SimulatedAt",
                table: "cat_impact_simulations",
                column: "SimulatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_negotiation_comments_AuthorId",
                table: "cat_negotiation_comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_negotiation_comments_NegotiationId",
                table: "cat_negotiation_comments",
                column: "NegotiationId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_pipeline_executions_ApiAssetId",
                table: "cat_pipeline_executions",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_pipeline_executions_StartedAt",
                table: "cat_pipeline_executions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_pipeline_executions_Status",
                table: "cat_pipeline_executions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_schema_evolution_advices_AnalyzedAt",
                table: "cat_schema_evolution_advices",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_schema_evolution_advices_ApiAssetId",
                table: "cat_schema_evolution_advices",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_schema_evolution_advices_CompatibilityLevel",
                table: "cat_schema_evolution_advices",
                column: "CompatibilityLevel");

            migrationBuilder.CreateIndex(
                name: "IX_cat_semantic_diff_results_Classification",
                table: "cat_semantic_diff_results",
                column: "Classification");

            migrationBuilder.CreateIndex(
                name: "IX_cat_semantic_diff_results_ContractVersionFromId",
                table: "cat_semantic_diff_results",
                column: "ContractVersionFromId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_semantic_diff_results_ContractVersionToId",
                table: "cat_semantic_diff_results",
                column: "ContractVersionToId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_semantic_diff_results_GeneratedAt",
                table: "cat_semantic_diff_results",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_background_service_contract_details_ContractVersionId",
                table: "ctr_background_service_contract_details",
                column: "ContractVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_background_service_contract_details_IsDeleted",
                table: "ctr_background_service_contract_details",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_background_service_draft_metadata_ContractDraftId",
                table: "ctr_background_service_draft_metadata",
                column: "ContractDraftId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_breaking_change_proposals_tenant_id_ContractId",
                table: "ctr_breaking_change_proposals",
                columns: new[] { "tenant_id", "ContractId" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_breaking_change_proposals_tenant_id_Status",
                table: "ctr_breaking_change_proposals",
                columns: new[] { "tenant_id", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_Domain",
                table: "ctr_canonical_entities",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_IsDeleted",
                table: "ctr_canonical_entities",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_Name",
                table: "ctr_canonical_entities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_OrganizationId",
                table: "ctr_canonical_entities",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_State",
                table: "ctr_canonical_entities",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entity_versions_CanonicalEntityId",
                table: "ctr_canonical_entity_versions",
                column: "CanonicalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entity_versions_CanonicalEntityId_Version",
                table: "ctr_canonical_entity_versions",
                columns: new[] { "CanonicalEntityId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_consumer_expectations_ApiAssetId",
                table: "ctr_consumer_expectations",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_consumer_expectations_ApiAssetId_ConsumerServiceName",
                table: "ctr_consumer_expectations",
                columns: new[] { "ApiAssetId", "ConsumerServiceName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_consumer_expectations_IsActive",
                table: "ctr_consumer_expectations",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_artifacts_ArtifactType",
                table: "ctr_contract_artifacts",
                column: "ArtifactType");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_artifacts_ContractVersionId",
                table: "ctr_contract_artifacts",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_changelogs_ApiAssetId",
                table: "ctr_contract_changelogs",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_changelogs_ApiAssetId_IsApproved",
                table: "ctr_contract_changelogs",
                columns: new[] { "ApiAssetId", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_changelogs_IsApproved",
                table: "ctr_contract_changelogs",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_changelogs_TenantId",
                table: "ctr_contract_changelogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ctr_consumer_inventory_tenant_contract",
                table: "ctr_contract_consumer_inventory",
                columns: new[] { "tenant_id", "ContractId" });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_consumer_inventory_unique_consumer",
                table: "ctr_contract_consumer_inventory",
                columns: new[] { "tenant_id", "ContractId", "ConsumerService", "ConsumerEnvironment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_deployments_ContractVersionId",
                table: "ctr_contract_deployments",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_deployments_ContractVersionId_Environment",
                table: "ctr_contract_deployments",
                columns: new[] { "ContractVersionId", "Environment" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_deployments_DeployedAt",
                table: "ctr_contract_deployments",
                column: "DeployedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_diffs_ContractVersionId",
                table: "ctr_contract_diffs",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_Author",
                table: "ctr_contract_drafts",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_IsDeleted",
                table: "ctr_contract_drafts",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_Protocol",
                table: "ctr_contract_drafts",
                column: "Protocol");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_ServiceId",
                table: "ctr_contract_drafts",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_ServiceInterfaceId",
                table: "ctr_contract_drafts",
                column: "ServiceInterfaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_Status",
                table: "ctr_contract_drafts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_evidence_packs_ApiAssetId",
                table: "ctr_contract_evidence_packs",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_evidence_packs_ChangeLevel",
                table: "ctr_contract_evidence_packs",
                column: "ChangeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_evidence_packs_ContractVersionId",
                table: "ctr_contract_evidence_packs",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_examples_ContractVersionId",
                table: "ctr_contract_examples",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_examples_DraftId",
                table: "ctr_contract_examples",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_reviews_DraftId",
                table: "ctr_contract_reviews",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_rule_violations_ContractVersionId",
                table: "ctr_contract_rule_violations",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_rule_violations_RulesetId",
                table: "ctr_contract_rule_violations",
                column: "RulesetId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_scorecards_ContractVersionId",
                table: "ctr_contract_scorecards",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_scorecards_OverallScore",
                table: "ctr_contract_scorecards",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_verifications_ApiAssetId",
                table: "ctr_contract_verifications",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_verifications_ApiAssetId_Status",
                table: "ctr_contract_verifications",
                columns: new[] { "ApiAssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_verifications_ServiceName",
                table: "ctr_contract_verifications",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_verifications_Status",
                table: "ctr_contract_verifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_verifications_TenantId",
                table: "ctr_contract_verifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_ApiAssetId_SemVer",
                table: "ctr_contract_versions",
                columns: new[] { "ApiAssetId", "SemVer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_IsDeleted",
                table: "ctr_contract_versions",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_LifecycleState",
                table: "ctr_contract_versions",
                column: "LifecycleState");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_Protocol",
                table: "ctr_contract_versions",
                column: "Protocol");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_data_contract_records_TenantId",
                table: "ctr_data_contract_records",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_data_contract_records_TenantId_ServiceId",
                table: "ctr_data_contract_records",
                columns: new[] { "TenantId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_data_contract_schemas_api_tenant_captured",
                table: "ctr_data_contract_schemas",
                columns: new[] { "tenant_id", "ApiAssetId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_data_contract_schemas_tenant_captured",
                table: "ctr_data_contract_schemas",
                columns: new[] { "tenant_id", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_deprecation_schedules_ContractId",
                table: "ctr_deprecation_schedules",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_deprecation_schedules_TenantId",
                table: "ctr_deprecation_schedules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_event_contract_details_ContractVersionId",
                table: "ctr_event_contract_details",
                column: "ContractVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_event_contract_details_IsDeleted",
                table: "ctr_event_contract_details",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_event_draft_metadata_ContractDraftId",
                table: "ctr_event_draft_metadata",
                column: "ContractDraftId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_feature_flag_records_TenantId",
                table: "ctr_feature_flag_records",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_feature_flag_records_TenantId_ServiceId",
                table: "ctr_feature_flag_records",
                columns: new[] { "TenantId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_feature_flag_records_TenantId_ServiceId_FlagKey",
                table: "ctr_feature_flag_records",
                columns: new[] { "TenantId", "ServiceId", "FlagKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ctr_graphql_snapshots_api_tenant_captured",
                table: "ctr_graphql_schema_snapshots",
                columns: new[] { "ApiAssetId", "TenantId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_graphql_snapshots_tenant_captured",
                table: "ctr_graphql_schema_snapshots",
                columns: new[] { "TenantId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_outbox_messages_CreatedAt",
                table: "ctr_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_outbox_messages_IdempotencyKey",
                table: "ctr_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_outbox_messages_ProcessedAt",
                table: "ctr_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "ix_ctr_protobuf_snapshots_api_tenant_captured",
                table: "ctr_protobuf_schema_snapshots",
                columns: new[] { "ApiAssetId", "TenantId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_protobuf_snapshots_tenant_captured",
                table: "ctr_protobuf_schema_snapshots",
                columns: new[] { "TenantId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_sbom_records_TenantId",
                table: "ctr_sbom_records",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_sbom_records_TenantId_ServiceId",
                table: "ctr_sbom_records",
                columns: new[] { "TenantId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_soap_contract_details_ContractVersionId",
                table: "ctr_soap_contract_details",
                column: "ContractVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_soap_contract_details_IsDeleted",
                table: "ctr_soap_contract_details",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_soap_draft_metadata_ContractDraftId",
                table: "ctr_soap_draft_metadata",
                column: "ContractDraftId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_IsActive",
                table: "ctr_spectral_rulesets",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_IsDeleted",
                table: "ctr_spectral_rulesets",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_Name",
                table: "ctr_spectral_rulesets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_OrganizationId",
                table: "ctr_spectral_rulesets",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_Origin",
                table: "ctr_spectral_rulesets",
                column: "Origin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_contract_compliance_gates");

            migrationBuilder.DropTable(
                name: "cat_contract_compliance_results");

            migrationBuilder.DropTable(
                name: "cat_contract_health_scores");

            migrationBuilder.DropTable(
                name: "cat_contract_listings");

            migrationBuilder.DropTable(
                name: "cat_contract_negotiations");

            migrationBuilder.DropTable(
                name: "cat_contract_reviews");

            migrationBuilder.DropTable(
                name: "cat_impact_simulations");

            migrationBuilder.DropTable(
                name: "cat_negotiation_comments");

            migrationBuilder.DropTable(
                name: "cat_pipeline_executions");

            migrationBuilder.DropTable(
                name: "cat_schema_evolution_advices");

            migrationBuilder.DropTable(
                name: "cat_semantic_diff_results");

            migrationBuilder.DropTable(
                name: "ctr_background_service_contract_details");

            migrationBuilder.DropTable(
                name: "ctr_background_service_draft_metadata");

            migrationBuilder.DropTable(
                name: "ctr_breaking_change_proposals");

            migrationBuilder.DropTable(
                name: "ctr_canonical_entities");

            migrationBuilder.DropTable(
                name: "ctr_canonical_entity_versions");

            migrationBuilder.DropTable(
                name: "ctr_consumer_expectations");

            migrationBuilder.DropTable(
                name: "ctr_contract_artifacts");

            migrationBuilder.DropTable(
                name: "ctr_contract_changelogs");

            migrationBuilder.DropTable(
                name: "ctr_contract_consumer_inventory");

            migrationBuilder.DropTable(
                name: "ctr_contract_deployments");

            migrationBuilder.DropTable(
                name: "ctr_contract_diffs");

            migrationBuilder.DropTable(
                name: "ctr_contract_evidence_packs");

            migrationBuilder.DropTable(
                name: "ctr_contract_examples");

            migrationBuilder.DropTable(
                name: "ctr_contract_reviews");

            migrationBuilder.DropTable(
                name: "ctr_contract_rule_violations");

            migrationBuilder.DropTable(
                name: "ctr_contract_scorecards");

            migrationBuilder.DropTable(
                name: "ctr_contract_verifications");

            migrationBuilder.DropTable(
                name: "ctr_data_contract_records");

            migrationBuilder.DropTable(
                name: "ctr_data_contract_schemas");

            migrationBuilder.DropTable(
                name: "ctr_deprecation_schedules");

            migrationBuilder.DropTable(
                name: "ctr_event_contract_details");

            migrationBuilder.DropTable(
                name: "ctr_event_draft_metadata");

            migrationBuilder.DropTable(
                name: "ctr_feature_flag_records");

            migrationBuilder.DropTable(
                name: "ctr_graphql_schema_snapshots");

            migrationBuilder.DropTable(
                name: "ctr_outbox_messages");

            migrationBuilder.DropTable(
                name: "ctr_protobuf_schema_snapshots");

            migrationBuilder.DropTable(
                name: "ctr_sbom_records");

            migrationBuilder.DropTable(
                name: "ctr_soap_contract_details");

            migrationBuilder.DropTable(
                name: "ctr_soap_draft_metadata");

            migrationBuilder.DropTable(
                name: "ctr_spectral_rulesets");

            migrationBuilder.DropTable(
                name: "ctr_contract_drafts");

            migrationBuilder.DropTable(
                name: "ctr_contract_versions");
        }
    }
}
