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
                name: "ai_ide_query_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IdeClient = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IdeClientVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QueryType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    QueryText = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    QueryContext = table.Column<string>(type: "jsonb", nullable: true),
                    ResponseText = table.Column<string>(type: "text", nullable: true),
                    ModelUsed = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GovernanceCheckResult = table.Column<string>(type: "jsonb", nullable: true),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_ai_ide_query_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_onboarding_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ExperienceLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChecklistItems = table.Column<string>(type: "jsonb", nullable: false),
                    CompletedItems = table.Column<int>(type: "integer", nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    ServicesExplored = table.Column<string>(type: "jsonb", nullable: true),
                    ContractsReviewed = table.Column<string>(type: "jsonb", nullable: true),
                    RunbooksRead = table.Column<string>(type: "jsonb", nullable: true),
                    AiInteractionCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ai_onboarding_sessions", x => x.Id);
                });

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
                    DataRetentionDays = table.Column<int>(type: "integer", nullable: true),
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
                name: "aik_agent_execution_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PlanStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxTokenBudget = table.Column<int>(type: "integer", nullable: false),
                    ConsumedTokens = table.Column<int>(type: "integer", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    BlastRadiusThreshold = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    steps = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_agent_execution_plans", x => x.Id);
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
                name: "aik_agent_performance_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalExecutions = table.Column<long>(type: "bigint", nullable: false),
                    ExecutionsWithFeedback = table.Column<long>(type: "bigint", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    AccuracyRate = table.Column<double>(type: "double precision", nullable: false),
                    RlCyclesCompleted = table.Column<int>(type: "integer", nullable: false),
                    TrajectoriesExported = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_agent_performance_metrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_agent_trajectory_feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActualOutcome = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WasCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    TimeToResolveMinutes = table.Column<int>(type: "integer", nullable: true),
                    SubmittedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExportedForTraining = table.Column<bool>(type: "boolean", nullable: false),
                    ExportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_agent_trajectory_feedbacks", x => x.Id);
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
                    UsePlanningMode = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                name: "aik_change_confidence_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Verdict = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BlastRadiusScore = table.Column<double>(type: "double precision", nullable: false),
                    TestCoverageScore = table.Column<double>(type: "double precision", nullable: false),
                    IncidentHistoryScore = table.Column<double>(type: "double precision", nullable: false),
                    TimeOfDayScore = table.Column<double>(type: "double precision", nullable: false),
                    DeployerExperienceScore = table.Column<double>(type: "double precision", nullable: false),
                    ChangeSizeScore = table.Column<double>(type: "double precision", nullable: false),
                    DependencyStabilityScore = table.Column<double>(type: "double precision", nullable: false),
                    ScoreBreakdownJson = table.Column<string>(type: "text", nullable: false),
                    RecommendationText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CalculatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_change_confidence_scores", x => x.Id);
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
                name: "aik_eval_datasets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UseCase = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TestCasesJson = table.Column<string>(type: "jsonb", nullable: false),
                    TestCaseCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_eval_datasets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_eval_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DatasetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CasesProcessed = table.Column<int>(type: "integer", nullable: false),
                    ExactMatchCount = table.Column<int>(type: "integer", nullable: false),
                    AverageSemanticSimilarity = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ToolCallAccuracy = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    LatencyP50Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    TotalTokenCost = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_eval_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_evaluation_cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SuiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    InputPrompt = table.Column<string>(type: "text", nullable: false),
                    GroundingContext = table.Column<string>(type: "text", nullable: false),
                    ExpectedOutputPattern = table.Column<string>(type: "text", nullable: false),
                    EvaluationCriteria = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_evaluation_cases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_evaluation_datasets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UseCase = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CaseCount = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_evaluation_datasets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_evaluation_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SuiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TotalCases = table.Column<int>(type: "integer", nullable: false),
                    PassedCases = table.Column<int>(type: "integer", nullable: false),
                    FailedCases = table.Column<int>(type: "integer", nullable: false),
                    AverageLatencyMs = table.Column<double>(type: "double precision", nullable: false),
                    TotalTokenCost = table.Column<decimal>(type: "numeric(14,6)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_evaluation_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_evaluation_suites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UseCase = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_evaluation_suites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_evaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PromptTemplateName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RelevanceScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    AccuracyScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    UsefulnessScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    SafetyScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Feedback = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_evaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_execution_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InputQuery = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Persona = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UseCaseType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SelectedModel = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SelectedProvider = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    RoutingPath = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SelectedSources = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SourceWeightingSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PolicyDecision = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EstimatedCostClass = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RationaleSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ConfidenceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EscalationReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_execution_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_external_data_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConnectorType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConnectorConfigJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    SyncIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSyncStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastSyncError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastSyncDocumentCount = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_external_data_sources", x => x.Id);
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
                name: "aik_gov_feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rating = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Comment = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    AgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    QueryCategory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_gov_feedbacks", x => x.Id);
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
                name: "aik_guardian_alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PatternDetected = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Recommendation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AcknowledgedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DismissReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WasActualIssue = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_guardian_alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_guardrails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GuardType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Pattern = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    PatternType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_guardrails", x => x.Id);
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
                    EmbeddingJson = table.Column<string>(type: "text", nullable: true),
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
                name: "aik_memory_nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Context = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TagsJson = table.Column<string>(type: "text", nullable: false),
                    LinkedNodeIdsJson = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RelevanceScore = table.Column<double>(type: "double precision", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_memory_nodes", x => x.Id);
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
                name: "aik_model_prediction_samples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PredictedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InputFeatureStatsJson = table.Column<string>(type: "jsonb", nullable: true),
                    PredictedClass = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: true),
                    InferenceLatencyMs = table.Column<int>(type: "integer", nullable: true),
                    ActualClass = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsFallback = table.Column<bool>(type: "boolean", nullable: false),
                    DriftAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_model_prediction_samples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_model_routing_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Intent = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreferredModelName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FallbackModelName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    MaxCostPerRequestUsd = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_model_routing_policies", x => x.Id);
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
                name: "aik_prompt_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Variables = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_prompt_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_prompt_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Variables = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetPersonas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ScopeHint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Relevance = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreferredModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecommendedTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxOutputTokens = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_prompt_templates", x => x.Id);
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
                    AutoAdjustedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AutoAdjustmentReason = table.Column<string>(type: "text", nullable: true),
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
                name: "aik_self_healing_actions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Result = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AuditTrailJson = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_self_healing_actions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_skill_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecutedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InputJson = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    OutputJson = table.Column<string>(type: "character varying(64000)", maxLength: 64000, nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_skill_executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_skill_feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActualOutcome = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WasCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_skill_feedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SkillContent = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnershipType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequiredTools = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PreferredModels = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    InputSchema = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    OutputSchema = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    ExecutionCount = table.Column<long>(type: "bigint", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    ParentAgentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsComposable = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerTeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_aik_skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_source_weights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UseCaseType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Relevance = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    TrustLevel = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ConfiguredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_source_weights", x => x.Id);
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
                    CostPerInputToken = table.Column<decimal>(type: "numeric(18,12)", nullable: true),
                    CostPerOutputToken = table.Column<decimal>(type: "numeric(18,12)", nullable: true),
                    EstimatedCostUsd = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    CostCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
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
                name: "aik_tool_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ParametersSchema = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    TimeoutMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_tool_definitions", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "aik_war_rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IncidentTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceAffected = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedByAgentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParticipantsJson = table.Column<string>(type: "text", nullable: false),
                    TimelineJson = table.Column<string>(type: "text", nullable: false),
                    SuggestedActionsJson = table.Column<string>(type: "text", nullable: false),
                    PostMortemDraft = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SkillUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_war_rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_prompt_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ChangeNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EvalScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_prompt_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_aik_prompt_versions_aik_prompt_assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "aik_prompt_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_IdeClient",
                table: "ai_ide_query_sessions",
                column: "IdeClient");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_ModelUsed",
                table: "ai_ide_query_sessions",
                column: "ModelUsed");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_QueryType",
                table: "ai_ide_query_sessions",
                column: "QueryType");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_Status",
                table: "ai_ide_query_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_SubmittedAt",
                table: "ai_ide_query_sessions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_TenantId",
                table: "ai_ide_query_sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_UserId",
                table: "ai_ide_query_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_onboarding_sessions_StartedAt",
                table: "ai_onboarding_sessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_onboarding_sessions_Status",
                table: "ai_onboarding_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ai_onboarding_sessions_TeamId",
                table: "ai_onboarding_sessions",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_onboarding_sessions_TenantId",
                table: "ai_onboarding_sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_onboarding_sessions_UserId",
                table: "ai_onboarding_sessions",
                column: "UserId");

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
                name: "IX_aik_agent_execution_plans_CorrelationId",
                table: "aik_agent_execution_plans",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_execution_plans_PlanStatus",
                table: "aik_agent_execution_plans",
                column: "PlanStatus");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_execution_plans_TenantId",
                table: "aik_agent_execution_plans",
                column: "TenantId");

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
                name: "IX_aik_agent_performance_metrics_AgentId",
                table: "aik_agent_performance_metrics",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_performance_metrics_PeriodStart",
                table: "aik_agent_performance_metrics",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_performance_metrics_TenantId",
                table: "aik_agent_performance_metrics",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_trajectory_feedbacks_ExecutionId",
                table: "aik_agent_trajectory_feedbacks",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_trajectory_feedbacks_ExportedForTraining",
                table: "aik_agent_trajectory_feedbacks",
                column: "ExportedForTraining");

            migrationBuilder.CreateIndex(
                name: "IX_aik_agent_trajectory_feedbacks_TenantId",
                table: "aik_agent_trajectory_feedbacks",
                column: "TenantId");

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
                name: "IX_aik_change_confidence_scores_ChangeId_TenantId",
                table: "aik_change_confidence_scores",
                columns: new[] { "ChangeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_aik_change_confidence_scores_ServiceName_TenantId",
                table: "aik_change_confidence_scores",
                columns: new[] { "ServiceName", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_aik_change_confidence_scores_TenantId",
                table: "aik_change_confidence_scores",
                column: "TenantId");

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
                name: "ix_aik_eval_datasets_tenant_usecase",
                table: "aik_eval_datasets",
                columns: new[] { "tenant_id", "UseCase" });

            migrationBuilder.CreateIndex(
                name: "ix_aik_eval_runs_tenant_dataset",
                table: "aik_eval_runs",
                columns: new[] { "tenant_id", "DatasetId" });

            migrationBuilder.CreateIndex(
                name: "ix_aik_eval_runs_tenant_model",
                table: "aik_eval_runs",
                columns: new[] { "tenant_id", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "idx_aik_eval_cases_suite",
                table: "aik_evaluation_cases",
                column: "SuiteId");

            migrationBuilder.CreateIndex(
                name: "idx_aik_eval_datasets_tenant",
                table: "aik_evaluation_datasets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "idx_aik_eval_runs_suite",
                table: "aik_evaluation_runs",
                column: "SuiteId");

            migrationBuilder.CreateIndex(
                name: "idx_aik_eval_runs_tenant",
                table: "aik_evaluation_runs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "idx_aik_eval_suites_tenant",
                table: "aik_evaluation_suites",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "idx_aik_eval_suites_tenant_usecase",
                table: "aik_evaluation_suites",
                columns: new[] { "TenantId", "UseCase" });

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_AgentExecutionId",
                table: "aik_evaluations",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_ConversationId",
                table: "aik_evaluations",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_EvaluatedAt",
                table: "aik_evaluations",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_OverallScore",
                table: "aik_evaluations",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_TenantId",
                table: "aik_evaluations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_UserId",
                table: "aik_evaluations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_execution_plans_CorrelationId",
                table: "aik_execution_plans",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_execution_plans_ExecutionId",
                table: "aik_execution_plans",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_execution_plans_PlannedAt",
                table: "aik_execution_plans",
                column: "PlannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_external_data_sources_ConnectorType",
                table: "aik_external_data_sources",
                column: "ConnectorType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_external_data_sources_IsActive",
                table: "aik_external_data_sources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_external_data_sources_Name",
                table: "aik_external_data_sources",
                column: "Name",
                unique: true);

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
                name: "IX_aik_gov_feedbacks_AgentExecutionId",
                table: "aik_gov_feedbacks",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_AgentName",
                table: "aik_gov_feedbacks",
                column: "AgentName");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_ConversationId",
                table: "aik_gov_feedbacks",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_CreatedByUserId",
                table: "aik_gov_feedbacks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_Rating",
                table: "aik_gov_feedbacks",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_SubmittedAt",
                table: "aik_gov_feedbacks",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_TenantId",
                table: "aik_gov_feedbacks",
                column: "TenantId");

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
                name: "IX_aik_guardian_alerts_ServiceName_TenantId",
                table: "aik_guardian_alerts",
                columns: new[] { "ServiceName", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_aik_guardian_alerts_Status",
                table: "aik_guardian_alerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aik_guardian_alerts_TenantId",
                table: "aik_guardian_alerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_guardrails_Category",
                table: "aik_guardrails",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_aik_guardrails_GuardType",
                table: "aik_guardrails",
                column: "GuardType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_guardrails_IsActive",
                table: "aik_guardrails",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_guardrails_Name",
                table: "aik_guardrails",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_guardrails_Priority",
                table: "aik_guardrails",
                column: "Priority");

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
                name: "IX_aik_memory_nodes_NodeType",
                table: "aik_memory_nodes",
                column: "NodeType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_memory_nodes_Subject_TenantId",
                table: "aik_memory_nodes",
                columns: new[] { "Subject", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_aik_memory_nodes_TenantId",
                table: "aik_memory_nodes",
                column: "TenantId");

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
                name: "IX_aik_model_prediction_samples_ModelId_TenantId",
                table: "aik_model_prediction_samples",
                columns: new[] { "ModelId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_aik_model_prediction_samples_TenantId_PredictedAt",
                table: "aik_model_prediction_samples",
                columns: new[] { "TenantId", "PredictedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_aik_model_routing_policies_TenantId_Intent_IsActive",
                table: "aik_model_routing_policies",
                columns: new[] { "TenantId", "Intent", "IsActive" });

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
                name: "IX_aik_prompt_assets_Slug_TenantId",
                table: "aik_prompt_assets",
                columns: new[] { "Slug", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_prompt_templates_Category",
                table: "aik_prompt_templates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_aik_prompt_templates_IsActive",
                table: "aik_prompt_templates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_prompt_templates_IsOfficial",
                table: "aik_prompt_templates",
                column: "IsOfficial");

            migrationBuilder.CreateIndex(
                name: "IX_aik_prompt_templates_Name",
                table: "aik_prompt_templates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_aik_prompt_templates_Name_Version",
                table: "aik_prompt_templates",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_prompt_versions_AssetId_VersionNumber",
                table: "aik_prompt_versions",
                columns: new[] { "AssetId", "VersionNumber" },
                unique: true);

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
                name: "IX_aik_self_healing_actions_IncidentId_TenantId",
                table: "aik_self_healing_actions",
                columns: new[] { "IncidentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_aik_self_healing_actions_Status",
                table: "aik_self_healing_actions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aik_self_healing_actions_TenantId",
                table: "aik_self_healing_actions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_skill_executions_ExecutedAt",
                table: "aik_skill_executions",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_skill_executions_ExecutedBy",
                table: "aik_skill_executions",
                column: "ExecutedBy");

            migrationBuilder.CreateIndex(
                name: "IX_aik_skill_executions_SkillId",
                table: "aik_skill_executions",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_skill_feedbacks_SkillExecutionId",
                table: "aik_skill_feedbacks",
                column: "SkillExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_skill_feedbacks_TenantId",
                table: "aik_skill_feedbacks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_skills_Name_TenantId",
                table: "aik_skills",
                columns: new[] { "Name", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_aik_skills_OwnershipType",
                table: "aik_skills",
                column: "OwnershipType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_skills_Status",
                table: "aik_skills",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aik_skills_TenantId",
                table: "aik_skills",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_source_weights_IsActive",
                table: "aik_source_weights",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_source_weights_UseCaseType_SourceType",
                table: "aik_source_weights",
                columns: new[] { "UseCaseType", "SourceType" });

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
                name: "IX_aik_tool_definitions_Category",
                table: "aik_tool_definitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_aik_tool_definitions_IsActive",
                table: "aik_tool_definitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_tool_definitions_Name",
                table: "aik_tool_definitions",
                column: "Name",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_aik_war_rooms_IncidentId",
                table: "aik_war_rooms",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_war_rooms_Status",
                table: "aik_war_rooms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aik_war_rooms_TenantId",
                table: "aik_war_rooms",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_ide_query_sessions");

            migrationBuilder.DropTable(
                name: "ai_onboarding_sessions");

            migrationBuilder.DropTable(
                name: "aik_access_policies");

            migrationBuilder.DropTable(
                name: "aik_agent_artifacts");

            migrationBuilder.DropTable(
                name: "aik_agent_execution_plans");

            migrationBuilder.DropTable(
                name: "aik_agent_executions");

            migrationBuilder.DropTable(
                name: "aik_agent_performance_metrics");

            migrationBuilder.DropTable(
                name: "aik_agent_trajectory_feedbacks");

            migrationBuilder.DropTable(
                name: "aik_agents");

            migrationBuilder.DropTable(
                name: "aik_budgets");

            migrationBuilder.DropTable(
                name: "aik_change_confidence_scores");

            migrationBuilder.DropTable(
                name: "aik_conversations");

            migrationBuilder.DropTable(
                name: "aik_eval_datasets");

            migrationBuilder.DropTable(
                name: "aik_eval_runs");

            migrationBuilder.DropTable(
                name: "aik_evaluation_cases");

            migrationBuilder.DropTable(
                name: "aik_evaluation_datasets");

            migrationBuilder.DropTable(
                name: "aik_evaluation_runs");

            migrationBuilder.DropTable(
                name: "aik_evaluation_suites");

            migrationBuilder.DropTable(
                name: "aik_evaluations");

            migrationBuilder.DropTable(
                name: "aik_execution_plans");

            migrationBuilder.DropTable(
                name: "aik_external_data_sources");

            migrationBuilder.DropTable(
                name: "aik_external_inference_records");

            migrationBuilder.DropTable(
                name: "aik_gov_feedbacks");

            migrationBuilder.DropTable(
                name: "aik_gov_outbox_messages");

            migrationBuilder.DropTable(
                name: "aik_guardian_alerts");

            migrationBuilder.DropTable(
                name: "aik_guardrails");

            migrationBuilder.DropTable(
                name: "aik_ide_capability_policies");

            migrationBuilder.DropTable(
                name: "aik_ide_client_registrations");

            migrationBuilder.DropTable(
                name: "aik_knowledge_sources");

            migrationBuilder.DropTable(
                name: "aik_memory_nodes");

            migrationBuilder.DropTable(
                name: "aik_messages");

            migrationBuilder.DropTable(
                name: "aik_model_prediction_samples");

            migrationBuilder.DropTable(
                name: "aik_model_routing_policies");

            migrationBuilder.DropTable(
                name: "aik_models");

            migrationBuilder.DropTable(
                name: "aik_prompt_templates");

            migrationBuilder.DropTable(
                name: "aik_prompt_versions");

            migrationBuilder.DropTable(
                name: "aik_providers");

            migrationBuilder.DropTable(
                name: "aik_routing_decisions");

            migrationBuilder.DropTable(
                name: "aik_routing_strategies");

            migrationBuilder.DropTable(
                name: "aik_self_healing_actions");

            migrationBuilder.DropTable(
                name: "aik_skill_executions");

            migrationBuilder.DropTable(
                name: "aik_skill_feedbacks");

            migrationBuilder.DropTable(
                name: "aik_skills");

            migrationBuilder.DropTable(
                name: "aik_source_weights");

            migrationBuilder.DropTable(
                name: "aik_sources");

            migrationBuilder.DropTable(
                name: "aik_token_quota_policies");

            migrationBuilder.DropTable(
                name: "aik_token_usage_ledger");

            migrationBuilder.DropTable(
                name: "aik_tool_definitions");

            migrationBuilder.DropTable(
                name: "aik_usage_entries");

            migrationBuilder.DropTable(
                name: "aik_war_rooms");

            migrationBuilder.DropTable(
                name: "aik_prompt_assets");
        }
    }
}
