using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAiGovernanceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_gov_access_policies",
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
                    table.PrimaryKey("PK_ai_gov_access_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_budgets",
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
                    table.PrimaryKey("PK_ai_gov_budgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_conversations",
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
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_gov_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_ide_capability_policies",
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
                    table.PrimaryKey("PK_ai_gov_ide_capability_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_ide_client_registrations",
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
                    table.PrimaryKey("PK_ai_gov_ide_client_registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_knowledge_sources",
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
                    table.PrimaryKey("PK_ai_gov_knowledge_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_messages",
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
                    table.PrimaryKey("PK_ai_gov_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_models",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Capabilities = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DefaultUseCases = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SensitivityLevel = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_gov_models", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_routing_decisions",
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
                    table.PrimaryKey("PK_ai_gov_routing_decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_routing_strategies",
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
                    table.PrimaryKey("PK_ai_gov_routing_strategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_usage_entries",
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
                    table.PrimaryKey("PK_ai_gov_usage_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiExternalInferenceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_AiExternalInferenceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ProviderType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsLocal = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SupportedCapabilities = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiSources",
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
                    table.PrimaryKey("PK_AiSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiTokenQuotaPolicies",
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
                    table.PrimaryKey("PK_AiTokenQuotaPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiTokenUsageLedger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_AiTokenUsageLedger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_access_policies_IsActive",
                table: "ai_gov_access_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_access_policies_Scope",
                table: "ai_gov_access_policies",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_budgets_IsActive",
                table: "ai_gov_budgets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_budgets_Scope",
                table: "ai_gov_budgets",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_conversations_CreatedBy",
                table: "ai_gov_conversations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_conversations_IsActive",
                table: "ai_gov_conversations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_conversations_LastMessageAt",
                table: "ai_gov_conversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_ide_capability_policies_ClientType",
                table: "ai_gov_ide_capability_policies",
                column: "ClientType");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_ide_capability_policies_IsActive",
                table: "ai_gov_ide_capability_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_ide_client_registrations_IsActive",
                table: "ai_gov_ide_client_registrations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_ide_client_registrations_UserId",
                table: "ai_gov_ide_client_registrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_knowledge_sources_IsActive",
                table: "ai_gov_knowledge_sources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_knowledge_sources_SourceType",
                table: "ai_gov_knowledge_sources",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_messages_ConversationId",
                table: "ai_gov_messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_messages_CorrelationId",
                table: "ai_gov_messages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_messages_Timestamp",
                table: "ai_gov_messages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_models_Name",
                table: "ai_gov_models",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_models_Provider",
                table: "ai_gov_models",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_models_Status",
                table: "ai_gov_models",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_routing_decisions_CorrelationId",
                table: "ai_gov_routing_decisions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_routing_decisions_DecidedAt",
                table: "ai_gov_routing_decisions",
                column: "DecidedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_routing_decisions_SelectedPath",
                table: "ai_gov_routing_decisions",
                column: "SelectedPath");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_routing_strategies_IsActive",
                table: "ai_gov_routing_strategies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_routing_strategies_Name",
                table: "ai_gov_routing_strategies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_routing_strategies_Priority",
                table: "ai_gov_routing_strategies",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_usage_entries_CorrelationId",
                table: "ai_gov_usage_entries",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_usage_entries_ModelId",
                table: "ai_gov_usage_entries",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_usage_entries_Timestamp",
                table: "ai_gov_usage_entries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_usage_entries_UserId",
                table: "ai_gov_usage_entries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiExternalInferenceRecords_PromotionStatus",
                table: "AiExternalInferenceRecords",
                column: "PromotionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AiExternalInferenceRecords_TenantId",
                table: "AiExternalInferenceRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AiExternalInferenceRecords_UserId",
                table: "AiExternalInferenceRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiProviders_IsEnabled",
                table: "AiProviders",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AiProviders_Name",
                table: "AiProviders",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AiProviders_ProviderType",
                table: "AiProviders",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_AiSources_IsEnabled",
                table: "AiSources",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AiSources_Name",
                table: "AiSources",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AiSources_OwnerTeam",
                table: "AiSources",
                column: "OwnerTeam");

            migrationBuilder.CreateIndex(
                name: "IX_AiSources_SourceType",
                table: "AiSources",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_AiTokenQuotaPolicies_IsEnabled",
                table: "AiTokenQuotaPolicies",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AiTokenQuotaPolicies_Scope",
                table: "AiTokenQuotaPolicies",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_AiTokenQuotaPolicies_ScopeValue",
                table: "AiTokenQuotaPolicies",
                column: "ScopeValue");

            migrationBuilder.CreateIndex(
                name: "IX_AiTokenUsageLedger_Status",
                table: "AiTokenUsageLedger",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AiTokenUsageLedger_TenantId",
                table: "AiTokenUsageLedger",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTokenUsageLedger_Timestamp",
                table: "AiTokenUsageLedger",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AiTokenUsageLedger_UserId",
                table: "AiTokenUsageLedger",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_CreatedAt",
                table: "outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                table: "outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_gov_access_policies");

            migrationBuilder.DropTable(
                name: "ai_gov_budgets");

            migrationBuilder.DropTable(
                name: "ai_gov_conversations");

            migrationBuilder.DropTable(
                name: "ai_gov_ide_capability_policies");

            migrationBuilder.DropTable(
                name: "ai_gov_ide_client_registrations");

            migrationBuilder.DropTable(
                name: "ai_gov_knowledge_sources");

            migrationBuilder.DropTable(
                name: "ai_gov_messages");

            migrationBuilder.DropTable(
                name: "ai_gov_models");

            migrationBuilder.DropTable(
                name: "ai_gov_routing_decisions");

            migrationBuilder.DropTable(
                name: "ai_gov_routing_strategies");

            migrationBuilder.DropTable(
                name: "ai_gov_usage_entries");

            migrationBuilder.DropTable(
                name: "AiExternalInferenceRecords");

            migrationBuilder.DropTable(
                name: "AiProviders");

            migrationBuilder.DropTable(
                name: "AiSources");

            migrationBuilder.DropTable(
                name: "AiTokenQuotaPolicies");

            migrationBuilder.DropTable(
                name: "AiTokenUsageLedger");

            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
