using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aik_access_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScopeValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AllowedModelIds = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    BlockedModelIds = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AllowExternalAI = table.Column<bool>(type: "boolean", nullable: false),
                    InternalOnly = table.Column<bool>(type: "boolean", nullable: false),
                    MaxTokensPerRequest = table.Column<int>(type: "integer", nullable: false),
                    EnvironmentRestrictions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_access_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_agent_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_agent_artifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_agent_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelIdUsed = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InputJson = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    OutputJson = table.Column<string>(type: "character varying(64000)", maxLength: 64000, nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Steps = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    ContextJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_agent_executions", x => x.Id);
                    table.CheckConstraint("CK_aik_agent_executions_Status", "\"Status\" IN ('Pending','Running','Completed','Failed','Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "aik_agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SystemPrompt = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    PreferredModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Capabilities = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TargetPersona = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OwnershipType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PublicationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerTeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AllowedModelIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AllowedTools = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Objective = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    InputSchema = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    OutputSchema = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    AllowModelOverride = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ExecutionCount = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_agents", x => x.Id);
                    table.CheckConstraint("CK_aik_agents_Category", "\"Category\" IN ('General','ServiceAnalysis','ContractGovernance','IncidentResponse','ChangeIntelligence','SecurityAudit','FinOps','CodeReview','Documentation','Testing','Compliance','ApiDesign','TestGeneration','EventDesign','DocumentationAssistance','SoapDesign')");
                    table.CheckConstraint("CK_aik_agents_PublicationStatus", "\"PublicationStatus\" IN ('Draft','PendingReview','Active','Published','Archived','Blocked')");
                });

            migrationBuilder.CreateTable(
                name: "aik_budgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScopeValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxTokens = table.Column<long>(type: "bigint", nullable: false),
                    MaxRequests = table.Column<int>(type: "integer", nullable: false),
                    CurrentTokensUsed = table.Column<long>(type: "bigint", nullable: false),
                    CurrentRequestCount = table.Column<int>(type: "integer", nullable: false),
                    PeriodStartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_budgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Persona = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DefaultContextScope = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LastModelUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_external_inference_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    OriginalPrompt = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    AdditionalContext = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Response = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    SensitivityClassification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QualityScore = table.Column<int>(type: "integer", nullable: true),
                    PromotionStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CanPromoteToSharedMemory = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_external_inference_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_gov_outbox_messages",
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
                    table.PrimaryKey("PK_aik_gov_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_ide_capability_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Persona = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AllowedCommands = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AllowedContextScopes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AllowedModelIds = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AllowContractGeneration = table.Column<bool>(type: "boolean", nullable: false),
                    AllowIncidentTroubleshooting = table.Column<bool>(type: "boolean", nullable: false),
                    AllowExternalAI = table.Column<bool>(type: "boolean", nullable: false),
                    MaxTokensPerRequest = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_ide_capability_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_ide_client_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ClientType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceIdentifier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastAccessAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RevocationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_ide_client_registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_knowledge_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EndpointOrPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_knowledge_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsInternalModel = table.Column<bool>(type: "boolean", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    AppliedPolicyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GroundingSources = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ContextReferences = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_models",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalModelId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ModelType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsInstalled = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Capabilities = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DefaultUseCases = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SensitivityLevel = table.Column<int>(type: "integer", nullable: false),
                    IsDefaultForChat = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefaultForReasoning = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefaultForEmbeddings = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsToolCalling = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsEmbeddings = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsVision = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsStructuredOutput = table.Column<bool>(type: "boolean", nullable: false),
                    ContextWindow = table.Column<int>(type: "integer", nullable: true),
                    RequiresGpu = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendedRamGb = table.Column<decimal>(type: "numeric(5,1)", nullable: true),
                    LicenseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LicenseUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ComplianceStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_models", x => x.Id);
                    table.CheckConstraint("CK_aik_models_ModelType", "\"ModelType\" IN ('Chat','Completion','Embedding','CodeGeneration','Analysis')");
                    table.CheckConstraint("CK_aik_models_Status", "\"Status\" IN ('Active','Inactive','Deprecated','Blocked')");
                });

            migrationBuilder.CreateTable(
                name: "aik_providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ProviderType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsLocal = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AuthenticationMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupportedCapabilities = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SupportsChat = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsEmbeddings = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsTools = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsVision = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsStructuredOutput = table.Column<bool>(type: "boolean", nullable: false),
                    HealthStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_providers", x => x.Id);
                    table.CheckConstraint("CK_aik_providers_HealthStatus", "\"HealthStatus\" IN ('Unknown','Healthy','Degraded','Unhealthy','Offline')");
                });

            migrationBuilder.CreateTable(
                name: "aik_routing_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Persona = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UseCaseType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SelectedPath = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SelectedModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SelectedProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsInternalModel = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedStrategyId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliedPolicyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EscalationReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rationale = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    EstimatedCostClass = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfidenceLevel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SelectedSources = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SourceWeightingSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_routing_decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_routing_strategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TargetPersona = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetUseCase = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetClientType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PreferredPath = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxSensitivityLevel = table.Column<int>(type: "integer", nullable: false),
                    AllowExternalEscalation = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_routing_strategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConnectionInfo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AccessPolicyScope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Classification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerTeam = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    HealthStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_token_quota_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScopeValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ModelId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MaxInputTokensPerRequest = table.Column<int>(type: "integer", nullable: false),
                    MaxOutputTokensPerRequest = table.Column<int>(type: "integer", nullable: false),
                    MaxTotalTokensPerRequest = table.Column<int>(type: "integer", nullable: false),
                    MaxTokensPerDay = table.Column<long>(type: "bigint", nullable: false),
                    MaxTokensPerMonth = table.Column<long>(type: "bigint", nullable: false),
                    MaxTokensAccumulated = table.Column<long>(type: "bigint", nullable: false),
                    IsHardLimit = table.Column<bool>(type: "boolean", nullable: false),
                    AllowSensitiveData = table.Column<bool>(type: "boolean", nullable: false),
                    AllowKnowledgePromotion = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_token_quota_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_token_usage_ledger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequestId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExecutionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DurationMs = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_token_usage_ledger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_usage_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Result = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContextScope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ClientType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_usage_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aik_access_policies_IsActive",
                table: "aik_access_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_access_policies_Scope",
                table: "aik_access_policies",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_artifacts_AgentId",
                table: "aik_agent_artifacts",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_artifacts_ArtifactType",
                table: "aik_agent_artifacts",
                column: "ArtifactType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_artifacts_ExecutionId",
                table: "aik_agent_artifacts",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_artifacts_ReviewStatus",
                table: "aik_agent_artifacts",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_executions_AgentId",
                table: "aik_agent_executions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_executions_CorrelationId",
                table: "aik_agent_executions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_executions_ExecutedBy",
                table: "aik_agent_executions",
                column: "ExecutedBy");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_executions_StartedAt",
                table: "aik_agent_executions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_executions_Status",
                table: "aik_agent_executions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agents_Category",
                table: "aik_agents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agents_IsActive",
                table: "aik_agents",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agents_OwnerId",
                table: "aik_agents",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agents_OwnershipType",
                table: "aik_agents",
                column: "OwnershipType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agents_PublicationStatus",
                table: "aik_agents",
                column: "PublicationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agents_Slug",
                table: "aik_agents",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_budgets_IsActive",
                table: "aik_budgets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_budgets_Scope",
                table: "aik_budgets",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_aik_conversations_CreatedBy",
                table: "aik_conversations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_aik_conversations_IsActive",
                table: "aik_conversations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_conversations_LastMessageAt",
                table: "aik_conversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_external_inference_records_PromotionStatus",
                table: "aik_external_inference_records",
                column: "PromotionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_aik_external_inference_records_TenantId",
                table: "aik_external_inference_records",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_external_inference_records_UserId",
                table: "aik_external_inference_records",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_outbox_messages_CreatedAt",
                table: "aik_gov_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_outbox_messages_IdempotencyKey",
                table: "aik_gov_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_outbox_messages_ProcessedAt",
                table: "aik_gov_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_ide_capability_policies_ClientType",
                table: "aik_ide_capability_policies",
                column: "ClientType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_ide_capability_policies_IsActive",
                table: "aik_ide_capability_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_ide_client_registrations_IsActive",
                table: "aik_ide_client_registrations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_ide_client_registrations_UserId",
                table: "aik_ide_client_registrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_knowledge_sources_IsActive",
                table: "aik_knowledge_sources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_knowledge_sources_SourceType",
                table: "aik_knowledge_sources",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_messages_ConversationId",
                table: "aik_messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_messages_CorrelationId",
                table: "aik_messages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_messages_Timestamp",
                table: "aik_messages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_aik_models_IsDefaultForChat",
                table: "aik_models",
                column: "IsDefaultForChat");

            migrationBuilder.CreateIndex(
                name: "IX_aik_models_Name",
                table: "aik_models",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_aik_models_Provider",
                table: "aik_models",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_aik_models_ProviderId",
                table: "aik_models",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_models_Slug",
                table: "aik_models",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_models_Status",
                table: "aik_models",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aik_providers_IsEnabled",
                table: "aik_providers",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_aik_providers_Name",
                table: "aik_providers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_aik_providers_ProviderType",
                table: "aik_providers",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_providers_Slug",
                table: "aik_providers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_routing_decisions_CorrelationId",
                table: "aik_routing_decisions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_routing_decisions_DecidedAt",
                table: "aik_routing_decisions",
                column: "DecidedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_routing_decisions_SelectedPath",
                table: "aik_routing_decisions",
                column: "SelectedPath");

            migrationBuilder.CreateIndex(
                name: "IX_aik_routing_strategies_IsActive",
                table: "aik_routing_strategies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_routing_strategies_Name",
                table: "aik_routing_strategies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_aik_routing_strategies_Priority",
                table: "aik_routing_strategies",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_aik_sources_IsEnabled",
                table: "aik_sources",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_aik_sources_Name",
                table: "aik_sources",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_aik_sources_OwnerTeam",
                table: "aik_sources",
                column: "OwnerTeam");

            migrationBuilder.CreateIndex(
                name: "IX_aik_sources_SourceType",
                table: "aik_sources",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_token_quota_policies_IsEnabled",
                table: "aik_token_quota_policies",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_aik_token_quota_policies_Scope",
                table: "aik_token_quota_policies",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_aik_token_quota_policies_ScopeValue",
                table: "aik_token_quota_policies",
                column: "ScopeValue");

            migrationBuilder.CreateIndex(
                name: "IX_aik_token_usage_ledger_Status",
                table: "aik_token_usage_ledger",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aik_token_usage_ledger_TenantId",
                table: "aik_token_usage_ledger",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_token_usage_ledger_Timestamp",
                table: "aik_token_usage_ledger",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_aik_token_usage_ledger_UserId",
                table: "aik_token_usage_ledger",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_usage_entries_CorrelationId",
                table: "aik_usage_entries",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_usage_entries_ModelId",
                table: "aik_usage_entries",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_usage_entries_Timestamp",
                table: "aik_usage_entries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_aik_usage_entries_UserId",
                table: "aik_usage_entries",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aik_access_policies");

            migrationBuilder.DropTable(
                name: "aik_agent_artifacts");

            migrationBuilder.DropTable(
                name: "aik_agent_executions");

            migrationBuilder.DropTable(
                name: "aik_agents");

            migrationBuilder.DropTable(
                name: "aik_budgets");

            migrationBuilder.DropTable(
                name: "aik_conversations");

            migrationBuilder.DropTable(
                name: "aik_external_inference_records");

            migrationBuilder.DropTable(
                name: "aik_gov_outbox_messages");

            migrationBuilder.DropTable(
                name: "aik_ide_capability_policies");

            migrationBuilder.DropTable(
                name: "aik_ide_client_registrations");

            migrationBuilder.DropTable(
                name: "aik_knowledge_sources");

            migrationBuilder.DropTable(
                name: "aik_messages");

            migrationBuilder.DropTable(
                name: "aik_models");

            migrationBuilder.DropTable(
                name: "aik_providers");

            migrationBuilder.DropTable(
                name: "aik_routing_decisions");

            migrationBuilder.DropTable(
                name: "aik_routing_strategies");

            migrationBuilder.DropTable(
                name: "aik_sources");

            migrationBuilder.DropTable(
                name: "aik_token_quota_policies");

            migrationBuilder.DropTable(
                name: "aik_token_usage_ledger");

            migrationBuilder.DropTable(
                name: "aik_usage_entries");
        }
    }
}
