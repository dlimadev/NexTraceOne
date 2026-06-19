using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    KeyHash = table.Column<string>(type: "text", nullable: false),
                    KeyPrefix = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<string>(type: "text", nullable: true),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetDeploymentStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    ImageTag = table.Column<string>(type: "text", nullable: false),
                    ReleaseName = table.Column<string>(type: "text", nullable: false),
                    RuntimeStatus = table.Column<int>(type: "integer", nullable: false),
                    LastHeartbeatAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeployedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDeploymentStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundServiceContractDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScheduleExpression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TriggerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InputsJson = table.Column<string>(type: "text", nullable: false),
                    OutputsJson = table.Column<string>(type: "text", nullable: false),
                    SideEffectsJson = table.Column<string>(type: "text", nullable: false),
                    TimeoutExpression = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AllowsConcurrency = table.Column<bool>(type: "boolean", nullable: false),
                    MessagingRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConsumedTopicsJson = table.Column<string>(type: "text", nullable: false),
                    ProducedTopicsJson = table.Column<string>(type: "text", nullable: false),
                    ConsumedServicesJson = table.Column<string>(type: "text", nullable: false),
                    ProducedEventsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundServiceContractDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundServiceDraftMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    TriggerType = table.Column<string>(type: "text", nullable: false),
                    ScheduleExpression = table.Column<string>(type: "text", nullable: true),
                    InputsJson = table.Column<string>(type: "text", nullable: false),
                    OutputsJson = table.Column<string>(type: "text", nullable: false),
                    SideEffectsJson = table.Column<string>(type: "text", nullable: false),
                    MessagingRole = table.Column<string>(type: "text", nullable: false),
                    ConsumedTopicsJson = table.Column<string>(type: "text", nullable: false),
                    ProducedTopicsJson = table.Column<string>(type: "text", nullable: false),
                    ConsumedServicesJson = table.Column<string>(type: "text", nullable: false),
                    ProducedEventsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundServiceDraftMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BreakingChangeProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProposedBreakingChangesJson = table.Column<string>(type: "text", nullable: false),
                    MigrationWindowDays = table.Column<int>(type: "integer", nullable: false),
                    DeprecationPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedBy = table.Column<string>(type: "text", nullable: false),
                    ConsultationOpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecisionNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakingChangeProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanonicalEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Owner = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    SchemaFormat = table.Column<string>(type: "text", nullable: false),
                    Criticality = table.Column<string>(type: "text", nullable: false),
                    ReusePolicy = table.Column<string>(type: "text", nullable: false),
                    OrganizationId = table.Column<string>(type: "text", nullable: true),
                    KnownUsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanonicalEntityVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    SchemaFormat = table.Column<string>(type: "text", nullable: false),
                    ChangeDescription = table.Column<string>(type: "text", nullable: false),
                    PublishedBy = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalEntityVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_hub_outbox_messages",
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
                    table.PrimaryKey("PK_cat_hub_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CicsTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ProgramName = table.Column<string>(type: "text", nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    CommareaLength = table.Column<int>(type: "integer", nullable: true),
                    Criticality = table.Column<int>(type: "integer", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CicsTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CobolPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    CompilerVersion = table.Column<string>(type: "text", nullable: false),
                    LastCompiled = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SourceLibrary = table.Column<string>(type: "text", nullable: false),
                    LoadModule = table.Column<string>(type: "text", nullable: false),
                    Criticality = table.Column<int>(type: "integer", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CobolPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeGenerationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiName = table.Column<string>(type: "text", nullable: false),
                    ContractVersion = table.Column<string>(type: "text", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    GenerationType = table.Column<string>(type: "text", nullable: false),
                    GeneratedCode = table.Column<string>(type: "text", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    TemplateId = table.Column<string>(type: "text", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeGenerationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeQualityRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    ProjectKey = table.Column<string>(type: "text", nullable: false),
                    QualityGateStatus = table.Column<string>(type: "text", nullable: false),
                    Coverage = table.Column<double>(type: "double precision", nullable: false),
                    Bugs = table.Column<int>(type: "integer", nullable: false),
                    Vulnerabilities = table.Column<int>(type: "integer", nullable: false),
                    CodeSmells = table.Column<int>(type: "integer", nullable: false),
                    DuplicatedLinesDensity = table.Column<double>(type: "double precision", nullable: false),
                    Branch = table.Column<string>(type: "text", nullable: true),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeQualityRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsumerAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsumerExpectations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerServiceName = table.Column<string>(type: "text", nullable: false),
                    ConsumerDomain = table.Column<string>(type: "text", nullable: false),
                    ExpectedSubsetJson = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerExpectations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BindingEnvironment = table.Column<string>(type: "text", nullable: false),
                    IsDefaultVersion = table.Column<bool>(type: "boolean", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActivatedBy = table.Column<string>(type: "text", nullable: true),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeactivatedBy = table.Column<string>(type: "text", nullable: true),
                    MigrationNotes = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractBindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractChangelogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ApiAssetId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    FromVersion = table.Column<string>(type: "text", nullable: true),
                    ToVersion = table.Column<string>(type: "text", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Entries = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    MarkdownContent = table.Column<string>(type: "text", nullable: true),
                    JsonContent = table.Column<string>(type: "text", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CommitSha = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractChangelogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractComplianceGates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Rules = table.Column<string>(type: "text", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    ScopeId = table.Column<string>(type: "text", nullable: false),
                    BlockOnViolation = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractComplianceGates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractComplianceResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<string>(type: "text", nullable: false),
                    ChangeId = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    Violations = table.Column<string>(type: "text", nullable: true),
                    EvidencePackId = table.Column<string>(type: "text", nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractComplianceResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractConsumerInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerService = table.Column<string>(type: "text", nullable: false),
                    ConsumerEnvironment = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: true),
                    FrequencyPerDay = table.Column<double>(type: "double precision", nullable: false),
                    LastCalledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FirstCalledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractConsumerInventories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    SemVer = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DeployedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeployedBy = table.Column<string>(type: "text", nullable: false),
                    SourceSystem = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractDeployments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractEvidencePacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<int>(type: "integer", nullable: false),
                    SemVer = table.Column<string>(type: "text", nullable: false),
                    ChangeLevel = table.Column<int>(type: "integer", nullable: false),
                    BreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    AdditiveChangeCount = table.Column<int>(type: "integer", nullable: false),
                    NonBreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    RecommendedVersion = table.Column<string>(type: "text", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric", nullable: false),
                    RuleViolationCount = table.Column<int>(type: "integer", nullable: false),
                    RequiresWorkflowApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresChangeNotification = table.Column<bool>(type: "boolean", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "text", nullable: false),
                    TechnicalSummary = table.Column<string>(type: "text", nullable: false),
                    ImpactedConsumers = table.Column<string[]>(type: "text[]", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractEvidencePacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractHealthScores",
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
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractHealthScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractLintRulesets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Origin = table.Column<int>(type: "integer", nullable: false),
                    DefaultExecutionMode = table.Column<int>(type: "integer", nullable: false),
                    EnforcementBehavior = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<string>(type: "text", nullable: true),
                    Owner = table.Column<string>(type: "text", nullable: true),
                    Domain = table.Column<string>(type: "text", nullable: true),
                    ApplicableServiceType = table.Column<string>(type: "text", nullable: true),
                    ApplicableProtocols = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractLintRulesets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    ConsumerCount = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalReviews = table.Column<int>(type: "integer", nullable: false),
                    IsPromoted = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PublishedBy = table.Column<string>(type: "text", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractListings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractNegotiations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedByTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedByTeamName = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Deadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Participants = table.Column<string>(type: "text", nullable: false),
                    ParticipantCount = table.Column<int>(type: "integer", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false),
                    ProposedContractSpec = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "text", nullable: true),
                    InitiatedByUserId = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractNegotiations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractPublications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractTitle = table.Column<string>(type: "text", nullable: false),
                    SemVer = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    PublishedBy = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseNotes = table.Column<string>(type: "text", nullable: true),
                    WithdrawnBy = table.Column<string>(type: "text", nullable: true),
                    WithdrawnAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractPublications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractScorecards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<int>(type: "integer", nullable: false),
                    QualityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CompletenessScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CompatibilityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric", nullable: false),
                    OperationCount = table.Column<int>(type: "integer", nullable: false),
                    SchemaCount = table.Column<int>(type: "integer", nullable: false),
                    HasSecurityDefinitions = table.Column<bool>(type: "boolean", nullable: false),
                    HasExamples = table.Column<bool>(type: "boolean", nullable: false),
                    HasDescriptions = table.Column<bool>(type: "boolean", nullable: false),
                    QualityJustification = table.Column<string>(type: "text", nullable: false),
                    CompletenessJustification = table.Column<string>(type: "text", nullable: false),
                    CompatibilityJustification = table.Column<string>(type: "text", nullable: false),
                    RiskJustification = table.Column<string>(type: "text", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractScorecards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ApiAssetId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpecContentHash = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BreakingChangesCount = table.Column<int>(type: "integer", nullable: false),
                    NonBreakingChangesCount = table.Column<int>(type: "integer", nullable: false),
                    AdditiveChangesCount = table.Column<int>(type: "integer", nullable: false),
                    DiffDetails = table.Column<string>(type: "text", nullable: false),
                    ComplianceViolations = table.Column<string>(type: "text", nullable: false),
                    SourceSystem = table.Column<string>(type: "text", nullable: false),
                    SourceBranch = table.Column<string>(type: "text", nullable: true),
                    CommitSha = table.Column<string>(type: "text", nullable: true),
                    PipelineId = table.Column<string>(type: "text", nullable: true),
                    EnvironmentName = table.Column<string>(type: "text", nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemVer = table.Column<string>(type: "text", nullable: false),
                    SpecContent = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    Protocol = table.Column<int>(type: "integer", nullable: false),
                    LifecycleState = table.Column<int>(type: "integer", nullable: false),
                    ImportedFrom = table.Column<string>(type: "text", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "text", nullable: true),
                    DeprecationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SunsetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecationNotice = table.Column<string>(type: "text", nullable: true),
                    LastOverallScore = table.Column<decimal>(type: "numeric", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopybookContractMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MappingType = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopybookContractMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopybookDiffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeLevel = table.Column<int>(type: "integer", nullable: false),
                    BreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    AdditiveChangeCount = table.Column<int>(type: "integer", nullable: false),
                    NonBreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopybookDiffs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopybookFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    PicClause = table.Column<string>(type: "text", nullable: false),
                    Offset = table.Column<int>(type: "integer", nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: false),
                    DataType = table.Column<string>(type: "text", nullable: false),
                    IsRedefines = table.Column<bool>(type: "boolean", nullable: false),
                    RedefinesField = table.Column<string>(type: "text", nullable: true),
                    OccursCount = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopybookFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopybookProgramUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageType = table.Column<string>(type: "text", nullable: false),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopybookProgramUsages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Copybooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    SourceLibrary = table.Column<string>(type: "text", nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: false),
                    Criticality = table.Column<int>(type: "integer", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Copybooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopybookVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionLabel = table.Column<string>(type: "text", nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: false),
                    FieldCount = table.Column<int>(type: "integer", nullable: false),
                    TotalLength = table.Column<int>(type: "integer", nullable: false),
                    RecordFormat = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopybookVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataContractRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    DatasetName = table.Column<string>(type: "text", nullable: false),
                    ContractVersion = table.Column<string>(type: "text", nullable: false),
                    FreshnessRequirementHours = table.Column<int>(type: "integer", nullable: true),
                    FieldDefinitionsJson = table.Column<string>(type: "text", nullable: true),
                    OwnerTeamId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataContractRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataContractSchemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Owner = table.Column<string>(type: "text", nullable: false),
                    SlaFreshnessHours = table.Column<int>(type: "integer", nullable: false),
                    SchemaJson = table.Column<string>(type: "text", nullable: false),
                    PiiClassification = table.Column<int>(type: "integer", nullable: false),
                    SourceSystem = table.Column<string>(type: "text", nullable: false),
                    ColumnCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataContractSchemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Db2Artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ArtifactType = table.Column<int>(type: "integer", nullable: false),
                    SchemaName = table.Column<string>(type: "text", nullable: false),
                    TablespaceName = table.Column<string>(type: "text", nullable: false),
                    DatabaseName = table.Column<string>(type: "text", nullable: false),
                    Criticality = table.Column<int>(type: "integer", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Db2Artifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeprecationSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    PlannedDeprecationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlannedSunsetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MigrationGuideUrl = table.Column<string>(type: "text", nullable: true),
                    SuccessorVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotificationDraftMessage = table.Column<string>(type: "text", nullable: true),
                    ScheduledByUserId = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeprecationSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeveloperSurveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<string>(type: "text", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: true),
                    RespondentId = table.Column<string>(type: "text", nullable: false),
                    Period = table.Column<string>(type: "text", nullable: false),
                    NpsScore = table.Column<int>(type: "integer", nullable: false),
                    ToolSatisfaction = table.Column<decimal>(type: "numeric", nullable: false),
                    ProcessSatisfaction = table.Column<decimal>(type: "numeric", nullable: false),
                    PlatformSatisfaction = table.Column<decimal>(type: "numeric", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NpsCategory = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeveloperSurveys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscoveredServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    ServiceNamespace = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TraceCount = table.Column<long>(type: "bigint", nullable: false),
                    EndpointCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MatchedServiceAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscoveryRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    IgnoreReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveredServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscoveryMatchRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Pattern = table.Column<string>(type: "text", nullable: false),
                    TargetServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryMatchRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscoveryRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    ServicesFound = table.Column<int>(type: "integer", nullable: false),
                    NewServicesFound = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractType = table.Column<int>(type: "integer", nullable: false),
                    Protocol = table.Column<int>(type: "integer", nullable: false),
                    SpecContent = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    ProposedVersion = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: false),
                    BaseContractVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    AiGenerationPrompt = table.Column<string>(type: "text", nullable: true),
                    LastEditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastEditedBy = table.Column<string>(type: "text", nullable: true),
                    ServiceInterfaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DxScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<string>(type: "text", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: true),
                    Period = table.Column<string>(type: "text", nullable: false),
                    CycleTimeHours = table.Column<decimal>(type: "numeric", nullable: false),
                    DeploymentFrequencyPerWeek = table.Column<decimal>(type: "numeric", nullable: false),
                    CognitiveLoadScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ToilPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ScoreLevel = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DxScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventContractDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    AsyncApiVersion = table.Column<string>(type: "text", nullable: false),
                    DefaultContentType = table.Column<string>(type: "text", nullable: false),
                    ChannelsJson = table.Column<string>(type: "text", nullable: false),
                    MessagesJson = table.Column<string>(type: "text", nullable: false),
                    ServersJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventContractDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventDraftMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    AsyncApiVersion = table.Column<string>(type: "text", nullable: false),
                    DefaultContentType = table.Column<string>(type: "text", nullable: false),
                    ChannelsJson = table.Column<string>(type: "text", nullable: false),
                    MessagesJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventDraftMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlagRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    FlagKey = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EnabledEnvironmentsJson = table.Column<string>(type: "text", nullable: true),
                    OwnerId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastToggledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScheduledRemovalDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FrameworkAssetDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageName = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    PackageManager = table.Column<string>(type: "text", nullable: false),
                    ArtifactRegistryUrl = table.Column<string>(type: "text", nullable: false),
                    LatestVersion = table.Column<string>(type: "text", nullable: false),
                    MinSupportedVersion = table.Column<string>(type: "text", nullable: false),
                    TargetPlatform = table.Column<string>(type: "text", nullable: false),
                    LicenseType = table.Column<string>(type: "text", nullable: false),
                    BuildPipelineUrl = table.Column<string>(type: "text", nullable: false),
                    ChangelogUrl = table.Column<string>(type: "text", nullable: false),
                    KnownConsumerCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkAssetDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GraphQlSchemaSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersion = table.Column<string>(type: "text", nullable: false),
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
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphQlSchemaSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GraphSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NodesJson = table.Column<string>(type: "text", nullable: false),
                    EdgesJson = table.Column<string>(type: "text", nullable: false),
                    NodeCount = table.Column<int>(type: "integer", nullable: false),
                    EdgeCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdeUsageRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    ResourceName = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeUsageRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImpactSimulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Scenario = table.Column<int>(type: "integer", nullable: false),
                    ScenarioDescription = table.Column<string>(type: "text", nullable: false),
                    AffectedServices = table.Column<string>(type: "text", nullable: true),
                    BrokenConsumers = table.Column<string>(type: "text", nullable: true),
                    TransitiveCascadeDepth = table.Column<int>(type: "integer", nullable: false),
                    RiskPercent = table.Column<int>(type: "integer", nullable: false),
                    MitigationRecommendations = table.Column<string>(type: "text", nullable: true),
                    SimulatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpactSimulations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImsTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionCode = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    PsbName = table.Column<string>(type: "text", nullable: false),
                    DbdName = table.Column<string>(type: "text", nullable: false),
                    Criticality = table.Column<int>(type: "integer", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImsTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "knw_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Slug = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastEditorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    freshness_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    last_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_documents", x => x.Id);
                    table.CheckConstraint("CK_knw_documents_category", "\"Category\" IN ('General','Runbook','Troubleshooting','Architecture','Procedure','PostMortem','Reference')");
                    table.CheckConstraint("CK_knw_documents_status", "\"Status\" IN ('Draft','Published','Archived','Deprecated')");
                });

            migrationBuilder.CreateTable(
                name: "knw_knowledge_graph_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalNodes = table.Column<int>(type: "integer", nullable: false),
                    TotalEdges = table.Column<int>(type: "integer", nullable: false),
                    ConnectedComponents = table.Column<int>(type: "integer", nullable: false),
                    IsolatedNodes = table.Column<int>(type: "integer", nullable: false),
                    CoverageScore = table.Column<int>(type: "integer", nullable: false),
                    NodeTypeDistribution = table.Column<string>(type: "jsonb", nullable: false),
                    EdgeTypeDistribution = table.Column<string>(type: "jsonb", nullable: false),
                    TopConnectedEntities = table.Column<string>(type: "jsonb", nullable: true),
                    OrphanEntities = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_knowledge_graph_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "knw_operational_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NoteType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContextType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "jsonb", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_operational_notes", x => x.Id);
                    table.CheckConstraint("CK_knw_operational_notes_note_type", "\"NoteType\" IN ('Observation','Mitigation','Decision','Hypothesis','FollowUp')");
                    table.CheckConstraint("CK_knw_operational_notes_severity", "\"Severity\" IN ('Info','Warning','Critical')");
                });

            migrationBuilder.CreateTable(
                name: "knw_proposed_runbooks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content_markdown = table.Column<string>(type: "text", nullable: false),
                    source_incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    team_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    proposed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_proposed_runbooks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knw_relations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Context = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_relations", x => x.Id);
                    table.CheckConstraint("CK_knw_relations_source_entity_type", "\"SourceEntityType\" IN ('KnowledgeDocument','OperationalNote')");
                    table.CheckConstraint("CK_knw_relations_target_type", "\"TargetType\" IN ('Service','Contract','Change','Incident','KnowledgeDocument','Runbook','Other')");
                });

            migrationBuilder.CreateTable(
                name: "LegacyDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAssetType = table.Column<int>(type: "integer", nullable: false),
                    TargetAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetAssetType = table.Column<int>(type: "integer", nullable: false),
                    DependencyType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyDependencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LinkedReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetType = table.Column<int>(type: "integer", nullable: false),
                    ReferenceType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedReferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MainframeSystems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    TechnicalOwner = table.Column<string>(type: "text", nullable: false),
                    BusinessOwner = table.Column<string>(type: "text", nullable: false),
                    Criticality = table.Column<int>(type: "integer", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "integer", nullable: false),
                    OperatingSystem = table.Column<string>(type: "text", nullable: false),
                    MipsCapacity = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainframeSystems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MqMessageContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QueueName = table.Column<string>(type: "text", nullable: false),
                    MessageFormat = table.Column<string>(type: "text", nullable: false),
                    PayloadSchema = table.Column<string>(type: "text", nullable: true),
                    MaxMessageLength = table.Column<int>(type: "integer", nullable: true),
                    HeaderFormat = table.Column<string>(type: "text", nullable: true),
                    EncodingScheme = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MqMessageContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NegotiationComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NegotiationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    AuthorDisplayName = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    LineReference = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NegotiationComments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NodeHealthRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeType = table.Column<int>(type: "integer", nullable: false),
                    OverlayMode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    FactorsJson = table.Column<string>(type: "text", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceSystem = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeHealthRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pan_analytics_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Feature = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Route = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DomainId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClientType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pan_analytics_events", x => x.Id);
                    table.CheckConstraint("CK_pan_analytics_events_event_type", "\"EventType\" IN ('ModuleViewed','EntityViewed','SearchExecuted','SearchResultClicked','ZeroResultSearch','QuickActionTriggered','AssistantPromptSubmitted','AssistantResponseUsed','ContractDraftCreated','ContractPublished','ChangeViewed','IncidentInvestigated','MitigationWorkflowStarted','MitigationWorkflowCompleted','EvidencePackageExported','PolicyViewed','ExecutiveOverviewViewed','RunbookViewed','SourceOfTruthQueried','ReportGenerated','OnboardingStepCompleted','JourneyAbandoned','EmptyStateEncountered','ReliabilityDashboardViewed','AutomationWorkflowManaged','ServiceCreated')");
                    table.CheckConstraint("CK_pan_analytics_events_module", "\"Module\" IN ('Dashboard','ServiceCatalog','SourceOfTruth','ContractStudio','ChangeIntelligence','Incidents','Reliability','Runbooks','AiAssistant','Governance','ExecutiveViews','FinOps','IntegrationHub','DeveloperPortal','Admin','Automation','Search')");
                });

            migrationBuilder.CreateTable(
                name: "pan_journey_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StepsJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pan_journey_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PipelineExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractName = table.Column<string>(type: "text", nullable: false),
                    ContractVersion = table.Column<string>(type: "text", nullable: false),
                    RequestedStages = table.Column<string>(type: "text", nullable: false),
                    StageResults = table.Column<string>(type: "text", nullable: true),
                    GeneratedArtifacts = table.Column<string>(type: "text", nullable: true),
                    TargetLanguage = table.Column<string>(type: "text", nullable: false),
                    TargetFramework = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalStages = table.Column<int>(type: "integer", nullable: false),
                    CompletedStages = table.Column<int>(type: "integer", nullable: false),
                    FailedStages = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    InitiatedByUserId = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlaygroundSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiName = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HttpMethod = table.Column<string>(type: "text", nullable: false),
                    RequestPath = table.Column<string>(type: "text", nullable: false),
                    RequestBody = table.Column<string>(type: "text", nullable: true),
                    RequestHeaders = table.Column<string>(type: "text", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaygroundSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortalAnalyticsEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    EntityType = table.Column<string>(type: "text", nullable: true),
                    SearchQuery = table.Column<string>(type: "text", nullable: true),
                    ZeroResults = table.Column<bool>(type: "boolean", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalAnalyticsEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductivitySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: true),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeploymentCount = table.Column<int>(type: "integer", nullable: false),
                    AverageCycleTimeHours = table.Column<decimal>(type: "numeric", nullable: false),
                    IncidentCount = table.Column<int>(type: "integer", nullable: false),
                    ManualStepsCount = table.Column<int>(type: "integer", nullable: false),
                    SnapshotSource = table.Column<string>(type: "text", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductivitySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProtobufSchemaSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersion = table.Column<string>(type: "text", nullable: false),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    FieldCount = table.Column<int>(type: "integer", nullable: false),
                    ServiceCount = table.Column<int>(type: "integer", nullable: false),
                    RpcCount = table.Column<int>(type: "integer", nullable: false),
                    MessageNamesJson = table.Column<string>(type: "text", nullable: false),
                    FieldsByMessageJson = table.Column<string>(type: "text", nullable: false),
                    RpcsByServiceJson = table.Column<string>(type: "text", nullable: false),
                    Syntax = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtobufSchemaSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateLimitPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestsPerMinute = table.Column<int>(type: "integer", nullable: false),
                    RequestsPerHour = table.Column<int>(type: "integer", nullable: false),
                    RequestsPerDay = table.Column<int>(type: "integer", nullable: false),
                    BurstLimit = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedBy = table.Column<string>(type: "text", nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedGraphViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    FiltersJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedGraphViews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SearchQuery = table.Column<string>(type: "text", nullable: false),
                    Filters = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SbomRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    components_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SbomRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchemaEvolutionAdvices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractName = table.Column<string>(type: "text", nullable: false),
                    SourceVersion = table.Column<string>(type: "text", nullable: false),
                    TargetVersion = table.Column<string>(type: "text", nullable: false),
                    CompatibilityLevel = table.Column<int>(type: "integer", nullable: false),
                    CompatibilityScore = table.Column<int>(type: "integer", nullable: false),
                    FieldsAdded = table.Column<string>(type: "text", nullable: true),
                    FieldsRemoved = table.Column<string>(type: "text", nullable: true),
                    FieldsModified = table.Column<string>(type: "text", nullable: true),
                    FieldsInUseByConsumers = table.Column<string>(type: "text", nullable: true),
                    AffectedConsumers = table.Column<string>(type: "text", nullable: true),
                    AffectedConsumerCount = table.Column<int>(type: "integer", nullable: false),
                    RecommendedStrategy = table.Column<int>(type: "integer", nullable: false),
                    StrategyDetails = table.Column<string>(type: "text", nullable: true),
                    Recommendations = table.Column<string>(type: "text", nullable: true),
                    Warnings = table.Column<string>(type: "text", nullable: true),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AnalyzedByAgentName = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaEvolutionAdvices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SemanticDiffResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionFromId = table.Column<string>(type: "text", nullable: false),
                    ContractVersionToId = table.Column<string>(type: "text", nullable: false),
                    NaturalLanguageSummary = table.Column<string>(type: "text", nullable: false),
                    Classification = table.Column<int>(type: "integer", nullable: false),
                    AffectedConsumers = table.Column<string>(type: "text", nullable: true),
                    MitigationSuggestions = table.Column<string>(type: "text", nullable: true),
                    CompatibilityScore = table.Column<int>(type: "integer", nullable: false),
                    GeneratedByModel = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanticDiffResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ServiceType = table.Column<int>(type: "integer", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    SystemArea = table.Column<string>(type: "text", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalOwner = table.Column<string>(type: "text", nullable: false),
                    BusinessOwner = table.Column<string>(type: "text", nullable: false),
                    Criticality = table.Column<int>(type: "integer", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "integer", nullable: false),
                    ExposureType = table.Column<int>(type: "integer", nullable: false),
                    SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false),
                    DocumentationUrl = table.Column<string>(type: "text", nullable: false),
                    RepositoryUrl = table.Column<string>(type: "text", nullable: false),
                    SubDomain = table.Column<string>(type: "text", nullable: true),
                    Capability = table.Column<string>(type: "text", nullable: true),
                    GitRepository = table.Column<string>(type: "text", nullable: false),
                    CiPipelineUrl = table.Column<string>(type: "text", nullable: false),
                    InfrastructureProvider = table.Column<string>(type: "text", nullable: false),
                    HostingPlatform = table.Column<string>(type: "text", nullable: false),
                    RuntimeLanguage = table.Column<string>(type: "text", nullable: false),
                    RuntimeVersion = table.Column<string>(type: "text", nullable: false),
                    SloTarget = table.Column<string>(type: "text", nullable: false),
                    DataClassification = table.Column<string>(type: "text", nullable: false),
                    RegulatoryScope = table.Column<string>(type: "text", nullable: false),
                    ChangeFrequency = table.Column<string>(type: "text", nullable: false),
                    ProductOwner = table.Column<string>(type: "text", nullable: false),
                    ContactChannel = table.Column<string>(type: "text", nullable: false),
                    OnCallRotationId = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    LastOwnershipReviewAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDependencyProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastScanAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SbomFormat = table.Column<int>(type: "integer", nullable: false),
                    SbomContent = table.Column<string>(type: "text", nullable: true),
                    HealthScore = table.Column<int>(type: "integer", nullable: false),
                    TotalDependencies = table.Column<int>(type: "integer", nullable: false),
                    DirectDependencies = table.Column<int>(type: "integer", nullable: false),
                    TransitiveDependencies = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDependencyProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceInterfaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    InterfaceType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExposureScope = table.Column<int>(type: "integer", nullable: false),
                    BasePath = table.Column<string>(type: "text", nullable: false),
                    TopicName = table.Column<string>(type: "text", nullable: false),
                    WsdlNamespace = table.Column<string>(type: "text", nullable: false),
                    GrpcServiceName = table.Column<string>(type: "text", nullable: false),
                    ScheduleCron = table.Column<string>(type: "text", nullable: false),
                    EnvironmentId = table.Column<string>(type: "text", nullable: false),
                    SloTarget = table.Column<string>(type: "text", nullable: false),
                    RequiresContract = table.Column<bool>(type: "boolean", nullable: false),
                    AuthScheme = table.Column<int>(type: "integer", nullable: false),
                    RateLimitPolicy = table.Column<string>(type: "text", nullable: false),
                    DocumentationUrl = table.Column<string>(type: "text", nullable: false),
                    DeprecationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SunsetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecationNotice = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceInterfaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    ServiceType = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    DefaultDomain = table.Column<string>(type: "text", nullable: false),
                    DefaultTeam = table.Column<string>(type: "text", nullable: false),
                    GovernancePolicyIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    BaseContractSpec = table.Column<string>(type: "text", nullable: true),
                    ScaffoldingManifestJson = table.Column<string>(type: "text", nullable: true),
                    RepositoryTemplateUrl = table.Column<string>(type: "text", nullable: true),
                    RepositoryTemplateBranch = table.Column<string>(type: "text", nullable: true),
                    ArchitecturePatternJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SoapContractDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    TargetNamespace = table.Column<string>(type: "text", nullable: false),
                    SoapVersion = table.Column<string>(type: "text", nullable: false),
                    EndpointUrl = table.Column<string>(type: "text", nullable: true),
                    WsdlSourceUrl = table.Column<string>(type: "text", nullable: true),
                    PortTypeName = table.Column<string>(type: "text", nullable: true),
                    BindingName = table.Column<string>(type: "text", nullable: true),
                    ExtractedOperationsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoapContractDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SoapDraftMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    TargetNamespace = table.Column<string>(type: "text", nullable: false),
                    SoapVersion = table.Column<string>(type: "text", nullable: false),
                    EndpointUrl = table.Column<string>(type: "text", nullable: true),
                    PortTypeName = table.Column<string>(type: "text", nullable: true),
                    BindingName = table.Column<string>(type: "text", nullable: true),
                    OperationsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoapDraftMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiName = table.Column<string>(type: "text", nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberEmail = table.Column<string>(type: "text", nullable: false),
                    ConsumerServiceName = table.Column<string>(type: "text", nullable: false),
                    ConsumerServiceVersion = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    WebhookUrl = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastNotifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VulnerabilityAdvisoryRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdvisoryId = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    CvssScore = table.Column<decimal>(type: "numeric", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PackageName = table.Column<string>(type: "text", nullable: true),
                    AffectedVersionRange = table.Column<string>(type: "text", nullable: true),
                    FixedInVersion = table.Column<string>(type: "text", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VulnerabilityAdvisoryRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZosConnectBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    OperationName = table.Column<string>(type: "text", nullable: false),
                    HttpMethod = table.Column<string>(type: "text", nullable: false),
                    BasePath = table.Column<string>(type: "text", nullable: false),
                    TargetTransaction = table.Column<string>(type: "text", nullable: false),
                    RequestSchema = table.Column<string>(type: "text", nullable: false),
                    ResponseSchema = table.Column<string>(type: "text", nullable: false),
                    Criticality = table.Column<int>(type: "integer", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZosConnectBindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentFormat = table.Column<string>(type: "text", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedBy = table.Column<string>(type: "text", nullable: false),
                    ContractVersionId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractArtifacts_ContractVersions_ContractVersionId1",
                        column: x => x.ContractVersionId1,
                        principalTable: "ContractVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContractDiffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    ChangeLevel = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    SuggestedSemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    additive_changes_json = table.Column<string>(type: "jsonb", nullable: true),
                    breaking_changes_json = table.Column<string>(type: "jsonb", nullable: true),
                    non_breaking_changes_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractDiffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractDiffs_ContractVersions_ContractVersionId",
                        column: x => x.ContractVersionId,
                        principalTable: "ContractVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractRuleViolations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RulesetId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuleName = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    SuggestedFix = table.Column<string>(type: "text", nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ContractVersionId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractRuleViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractRuleViolations_ContractVersions_ContractVersionId1",
                        column: x => x.ContractVersionId1,
                        principalTable: "ContractVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Examples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentFormat = table.Column<string>(type: "text", nullable: false),
                    ExampleType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ContractDraftId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Examples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Examples_Drafts_ContractDraftId",
                        column: x => x.ContractDraftId,
                        principalTable: "Drafts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApiAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RoutePattern = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Visibility = table.Column<string>(type: "text", nullable: false),
                    OwnerServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDecommissioned = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiAssets_ServiceAssets_OwnerServiceId",
                        column: x => x.OwnerServiceId,
                        principalTable: "ServiceAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IconHint = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ServiceAssetId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceLinks_ServiceAssets_ServiceAssetId1",
                        column: x => x.ServiceAssetId1,
                        principalTable: "ServiceAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PackageDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageName = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Ecosystem = table.Column<int>(type: "integer", nullable: false),
                    IsDirect = table.Column<bool>(type: "boolean", nullable: false),
                    License = table.Column<string>(type: "text", nullable: true),
                    LicenseRisk = table.Column<int>(type: "integer", nullable: false),
                    LatestStableVersion = table.Column<string>(type: "text", nullable: true),
                    IsOutdated = table.Column<bool>(type: "boolean", nullable: false),
                    DeprecationNotice = table.Column<string>(type: "text", nullable: true),
                    ServiceDependencyProfileId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageDependencies_ServiceDependencyProfiles_ServiceDepend~",
                        column: x => x.ServiceDependencyProfileId,
                        principalTable: "ServiceDependencyProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConsumerRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    FirstObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumerRelationships_ApiAssets_ApiAssetId",
                        column: x => x.ApiAssetId,
                        principalTable: "ApiAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DiscoverySources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    ExternalReference = table.Column<string>(type: "text", nullable: false),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoverySources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscoverySources_ApiAssets_ApiAssetId",
                        column: x => x.ApiAssetId,
                        principalTable: "ApiAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiAssets_OwnerServiceId",
                table: "ApiAssets",
                column: "OwnerServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_hub_outbox_messages_CreatedAt",
                table: "cat_hub_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_hub_outbox_messages_IdempotencyKey",
                table: "cat_hub_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_hub_outbox_messages_ProcessedAt",
                table: "cat_hub_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerRelationships_ApiAssetId",
                table: "ConsumerRelationships",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractArtifacts_ContractVersionId1",
                table: "ContractArtifacts",
                column: "ContractVersionId1");

            migrationBuilder.CreateIndex(
                name: "IX_ContractDiffs_ApiAssetId",
                table: "ContractDiffs",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractDiffs_ContractVersionId",
                table: "ContractDiffs",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractRuleViolations_ContractVersionId1",
                table: "ContractRuleViolations",
                column: "ContractVersionId1");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoverySources_ApiAssetId",
                table: "DiscoverySources",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Examples_ContractDraftId",
                table: "Examples",
                column: "ContractDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_documents_AuthorId",
                table: "knw_documents",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_documents_Category",
                table: "knw_documents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_knw_documents_CreatedAt",
                table: "knw_documents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_knw_documents_Slug",
                table: "knw_documents",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knw_documents_Status",
                table: "knw_documents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_knw_knowledge_graph_snapshots_GeneratedAt",
                table: "knw_knowledge_graph_snapshots",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_knw_knowledge_graph_snapshots_Status",
                table: "knw_knowledge_graph_snapshots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_knw_knowledge_graph_snapshots_tenant_id",
                table: "knw_knowledge_graph_snapshots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_AuthorId",
                table: "knw_operational_notes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_ContextEntityId",
                table: "knw_operational_notes",
                column: "ContextEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_ContextType",
                table: "knw_operational_notes",
                column: "ContextType");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_ContextType_ContextEntityId",
                table: "knw_operational_notes",
                columns: new[] { "ContextType", "ContextEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_CreatedAt",
                table: "knw_operational_notes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_IsResolved",
                table: "knw_operational_notes",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_NoteType",
                table: "knw_operational_notes",
                column: "NoteType");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_Origin",
                table: "knw_operational_notes",
                column: "Origin");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_Severity",
                table: "knw_operational_notes",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "uix_knw_proposed_runbooks_incident",
                table: "knw_proposed_runbooks",
                column: "source_incident_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knw_relations_SourceEntityId",
                table: "knw_relations",
                column: "SourceEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_relations_SourceEntityId_TargetEntityId",
                table: "knw_relations",
                columns: new[] { "SourceEntityId", "TargetEntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knw_relations_TargetEntityId",
                table: "knw_relations",
                column: "TargetEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_relations_TargetType",
                table: "knw_relations",
                column: "TargetType");

            migrationBuilder.CreateIndex(
                name: "IX_knw_relations_TargetType_TargetEntityId",
                table: "knw_relations",
                columns: new[] { "TargetType", "TargetEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageDependencies_ServiceDependencyProfileId",
                table: "PackageDependencies",
                column: "ServiceDependencyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_CreatedAt",
                table: "pan_analytics_events",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_EventType",
                table: "pan_analytics_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_Module",
                table: "pan_analytics_events",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_Module_EventType",
                table: "pan_analytics_events",
                columns: new[] { "Module", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_OccurredAt",
                table: "pan_analytics_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_Persona",
                table: "pan_analytics_events",
                column: "Persona");

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_SessionId_OccurredAt",
                table: "pan_analytics_events",
                columns: new[] { "SessionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_TenantId_Module_OccurredAt",
                table: "pan_analytics_events",
                columns: new[] { "TenantId", "Module", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_TenantId_OccurredAt",
                table: "pan_analytics_events",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_TenantId_UserId_OccurredAt",
                table: "pan_analytics_events",
                columns: new[] { "TenantId", "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_UserId",
                table: "pan_analytics_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_pan_journey_definitions_TenantId_IsActive",
                table: "pan_journey_definitions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_pan_journey_definitions_TenantId_Key",
                table: "pan_journey_definitions",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLinks_ServiceAssetId1",
                table: "ServiceLinks",
                column: "ServiceAssetId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AssetDeploymentStates");

            migrationBuilder.DropTable(
                name: "BackgroundServiceContractDetails");

            migrationBuilder.DropTable(
                name: "BackgroundServiceDraftMetadata");

            migrationBuilder.DropTable(
                name: "BreakingChangeProposals");

            migrationBuilder.DropTable(
                name: "CanonicalEntities");

            migrationBuilder.DropTable(
                name: "CanonicalEntityVersions");

            migrationBuilder.DropTable(
                name: "cat_hub_outbox_messages");

            migrationBuilder.DropTable(
                name: "CicsTransactions");

            migrationBuilder.DropTable(
                name: "CobolPrograms");

            migrationBuilder.DropTable(
                name: "CodeGenerationRecords");

            migrationBuilder.DropTable(
                name: "CodeQualityRecords");

            migrationBuilder.DropTable(
                name: "ConsumerAssets");

            migrationBuilder.DropTable(
                name: "ConsumerExpectations");

            migrationBuilder.DropTable(
                name: "ConsumerRelationships");

            migrationBuilder.DropTable(
                name: "ContractArtifacts");

            migrationBuilder.DropTable(
                name: "ContractBindings");

            migrationBuilder.DropTable(
                name: "ContractChangelogs");

            migrationBuilder.DropTable(
                name: "ContractComplianceGates");

            migrationBuilder.DropTable(
                name: "ContractComplianceResults");

            migrationBuilder.DropTable(
                name: "ContractConsumerInventories");

            migrationBuilder.DropTable(
                name: "ContractDeployments");

            migrationBuilder.DropTable(
                name: "ContractDiffs");

            migrationBuilder.DropTable(
                name: "ContractEvidencePacks");

            migrationBuilder.DropTable(
                name: "ContractHealthScores");

            migrationBuilder.DropTable(
                name: "ContractLintRulesets");

            migrationBuilder.DropTable(
                name: "ContractListings");

            migrationBuilder.DropTable(
                name: "ContractNegotiations");

            migrationBuilder.DropTable(
                name: "ContractPublications");

            migrationBuilder.DropTable(
                name: "ContractRuleViolations");

            migrationBuilder.DropTable(
                name: "ContractScorecards");

            migrationBuilder.DropTable(
                name: "ContractVerifications");

            migrationBuilder.DropTable(
                name: "CopybookContractMappings");

            migrationBuilder.DropTable(
                name: "CopybookDiffs");

            migrationBuilder.DropTable(
                name: "CopybookFields");

            migrationBuilder.DropTable(
                name: "CopybookProgramUsages");

            migrationBuilder.DropTable(
                name: "Copybooks");

            migrationBuilder.DropTable(
                name: "CopybookVersions");

            migrationBuilder.DropTable(
                name: "DataContractRecords");

            migrationBuilder.DropTable(
                name: "DataContractSchemas");

            migrationBuilder.DropTable(
                name: "Db2Artifacts");

            migrationBuilder.DropTable(
                name: "DeprecationSchedules");

            migrationBuilder.DropTable(
                name: "DeveloperSurveys");

            migrationBuilder.DropTable(
                name: "DiscoveredServices");

            migrationBuilder.DropTable(
                name: "DiscoveryMatchRules");

            migrationBuilder.DropTable(
                name: "DiscoveryRuns");

            migrationBuilder.DropTable(
                name: "DiscoverySources");

            migrationBuilder.DropTable(
                name: "DxScores");

            migrationBuilder.DropTable(
                name: "EventContractDetails");

            migrationBuilder.DropTable(
                name: "EventDraftMetadata");

            migrationBuilder.DropTable(
                name: "Examples");

            migrationBuilder.DropTable(
                name: "FeatureFlagRecords");

            migrationBuilder.DropTable(
                name: "FrameworkAssetDetails");

            migrationBuilder.DropTable(
                name: "GraphQlSchemaSnapshots");

            migrationBuilder.DropTable(
                name: "GraphSnapshots");

            migrationBuilder.DropTable(
                name: "IdeUsageRecords");

            migrationBuilder.DropTable(
                name: "ImpactSimulations");

            migrationBuilder.DropTable(
                name: "ImsTransactions");

            migrationBuilder.DropTable(
                name: "knw_documents");

            migrationBuilder.DropTable(
                name: "knw_knowledge_graph_snapshots");

            migrationBuilder.DropTable(
                name: "knw_operational_notes");

            migrationBuilder.DropTable(
                name: "knw_proposed_runbooks");

            migrationBuilder.DropTable(
                name: "knw_relations");

            migrationBuilder.DropTable(
                name: "LegacyDependencies");

            migrationBuilder.DropTable(
                name: "LinkedReferences");

            migrationBuilder.DropTable(
                name: "MainframeSystems");

            migrationBuilder.DropTable(
                name: "MarketplaceReviews");

            migrationBuilder.DropTable(
                name: "MqMessageContracts");

            migrationBuilder.DropTable(
                name: "NegotiationComments");

            migrationBuilder.DropTable(
                name: "NodeHealthRecords");

            migrationBuilder.DropTable(
                name: "PackageDependencies");

            migrationBuilder.DropTable(
                name: "pan_analytics_events");

            migrationBuilder.DropTable(
                name: "pan_journey_definitions");

            migrationBuilder.DropTable(
                name: "PipelineExecutions");

            migrationBuilder.DropTable(
                name: "PlaygroundSessions");

            migrationBuilder.DropTable(
                name: "PortalAnalyticsEvents");

            migrationBuilder.DropTable(
                name: "ProductivitySnapshots");

            migrationBuilder.DropTable(
                name: "ProtobufSchemaSnapshots");

            migrationBuilder.DropTable(
                name: "RateLimitPolicies");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "SavedGraphViews");

            migrationBuilder.DropTable(
                name: "SavedSearches");

            migrationBuilder.DropTable(
                name: "SbomRecords");

            migrationBuilder.DropTable(
                name: "SchemaEvolutionAdvices");

            migrationBuilder.DropTable(
                name: "SemanticDiffResults");

            migrationBuilder.DropTable(
                name: "ServiceInterfaces");

            migrationBuilder.DropTable(
                name: "ServiceLinks");

            migrationBuilder.DropTable(
                name: "ServiceTemplates");

            migrationBuilder.DropTable(
                name: "SoapContractDetails");

            migrationBuilder.DropTable(
                name: "SoapDraftMetadata");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "VulnerabilityAdvisoryRecords");

            migrationBuilder.DropTable(
                name: "ZosConnectBindings");

            migrationBuilder.DropTable(
                name: "ContractVersions");

            migrationBuilder.DropTable(
                name: "ApiAssets");

            migrationBuilder.DropTable(
                name: "Drafts");

            migrationBuilder.DropTable(
                name: "ServiceDependencyProfiles");

            migrationBuilder.DropTable(
                name: "ServiceAssets");
        }
    }
}
