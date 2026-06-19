using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Scope = table.Column<string>(type: "text", nullable: false),
                    ScopeValue = table.Column<string>(type: "text", nullable: false),
                    AllowedModelIds = table.Column<string>(type: "text", nullable: false),
                    BlockedModelIds = table.Column<string>(type: "text", nullable: false),
                    AllowExternalAI = table.Column<bool>(type: "boolean", nullable: false),
                    InternalOnly = table.Column<bool>(type: "boolean", nullable: false),
                    MaxTokensPerRequest = table.Column<int>(type: "integer", nullable: false),
                    EnvironmentRestrictions = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_AccessPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    ReviewStatus = table.Column<int>(type: "integer", nullable: false),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentArtifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentExecutionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PlanStatus = table.Column<int>(type: "integer", nullable: false),
                    MaxTokenBudget = table.Column<int>(type: "integer", nullable: false),
                    ConsumedTokens = table.Column<int>(type: "integer", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    BlastRadiusThreshold = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentExecutionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutedBy = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ModelIdUsed = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderUsed = table.Column<string>(type: "text", nullable: false),
                    InputJson = table.Column<string>(type: "text", nullable: false),
                    OutputJson = table.Column<string>(type: "text", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Steps = table.Column<string>(type: "text", nullable: false),
                    ContextJson = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentPerformanceMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentName = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_AgentPerformanceMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    PreferredModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Capabilities = table.Column<string>(type: "text", nullable: false),
                    TargetPersona = table.Column<string>(type: "text", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OwnershipType = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    PublicationStatus = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    OwnerTeamId = table.Column<string>(type: "text", nullable: false),
                    AllowedModelIds = table.Column<string>(type: "text", nullable: false),
                    AllowedTools = table.Column<string>(type: "text", nullable: false),
                    Objective = table.Column<string>(type: "text", nullable: false),
                    InputSchema = table.Column<string>(type: "text", nullable: false),
                    OutputSchema = table.Column<string>(type: "text", nullable: false),
                    AllowModelOverride = table.Column<bool>(type: "boolean", nullable: false),
                    UsePlanningMode = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ExecutionCount = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentTrajectoryFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ActualOutcome = table.Column<string>(type: "text", nullable: true),
                    WasCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    TimeToResolveMinutes = table.Column<int>(type: "integer", nullable: true),
                    SubmittedBy = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_AgentTrajectoryFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_hub_outbox_messages",
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
                    table.PrimaryKey("PK_aik_hub_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Scope = table.Column<string>(type: "text", nullable: false),
                    ScopeValue = table.Column<string>(type: "text", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeConfidenceScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Verdict = table.Column<string>(type: "text", nullable: false),
                    BlastRadiusScore = table.Column<double>(type: "double precision", nullable: false),
                    TestCoverageScore = table.Column<double>(type: "double precision", nullable: false),
                    IncidentHistoryScore = table.Column<double>(type: "double precision", nullable: false),
                    TimeOfDayScore = table.Column<double>(type: "double precision", nullable: false),
                    DeployerExperienceScore = table.Column<double>(type: "double precision", nullable: false),
                    ChangeSizeScore = table.Column<double>(type: "double precision", nullable: false),
                    DependencyStabilityScore = table.Column<double>(type: "double precision", nullable: false),
                    ScoreBreakdownJson = table.Column<string>(type: "text", nullable: false),
                    RecommendationText = table.Column<string>(type: "text", nullable: false),
                    CalculatedBy = table.Column<string>(type: "text", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeConfidenceScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Consultations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Context = table.Column<string>(type: "text", nullable: false),
                    Query = table.Column<string>(type: "text", nullable: false),
                    Response = table.Column<string>(type: "text", nullable: true),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedBy = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    ContextType = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    TokenEstimate = table.Column<int>(type: "integer", nullable: false),
                    AssembledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Persona = table.Column<string>(type: "text", nullable: false),
                    ClientType = table.Column<int>(type: "integer", nullable: false),
                    DefaultContextScope = table.Column<string>(type: "text", nullable: false),
                    LastModelUsed = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvalDatasets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UseCase = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TestCasesJson = table.Column<string>(type: "text", nullable: false),
                    TestCaseCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvalDatasets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvalRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CasesProcessed = table.Column<int>(type: "integer", nullable: false),
                    ExactMatchCount = table.Column<int>(type: "integer", nullable: false),
                    AverageSemanticSimilarity = table.Column<decimal>(type: "numeric", nullable: false),
                    ToolCallAccuracy = table.Column<decimal>(type: "numeric", nullable: false),
                    LatencyP50Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    TotalTokenCost = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvalRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    InputPrompt = table.Column<string>(type: "text", nullable: false),
                    GroundingContext = table.Column<string>(type: "text", nullable: false),
                    ExpectedOutputPattern = table.Column<string>(type: "text", nullable: false),
                    EvaluationCriteria = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationDatasets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UseCase = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    CaseCount = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationDatasets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptVersion = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TotalCases = table.Column<int>(type: "integer", nullable: false),
                    PassedCases = table.Column<int>(type: "integer", nullable: false),
                    FailedCases = table.Column<int>(type: "integer", nullable: false),
                    AverageLatencyMs = table.Column<double>(type: "double precision", nullable: false),
                    TotalTokenCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationType = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    PromptTemplateName = table.Column<string>(type: "text", nullable: true),
                    RelevanceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    AccuracyScore = table.Column<decimal>(type: "numeric", nullable: false),
                    UsefulnessScore = table.Column<decimal>(type: "numeric", nullable: false),
                    SafetyScore = table.Column<decimal>(type: "numeric", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric", nullable: false),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationSuites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UseCase = table.Column<string>(type: "text", nullable: false),
                    TargetModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationSuites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    InputQuery = table.Column<string>(type: "text", nullable: false),
                    Persona = table.Column<string>(type: "text", nullable: false),
                    UseCaseType = table.Column<int>(type: "integer", nullable: false),
                    SelectedModel = table.Column<string>(type: "text", nullable: false),
                    SelectedProvider = table.Column<string>(type: "text", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    RoutingPath = table.Column<int>(type: "integer", nullable: false),
                    SelectedSources = table.Column<string>(type: "text", nullable: false),
                    SourceWeightingSummary = table.Column<string>(type: "text", nullable: false),
                    PolicyDecision = table.Column<string>(type: "text", nullable: false),
                    EstimatedCostClass = table.Column<string>(type: "text", nullable: false),
                    RationaleSummary = table.Column<string>(type: "text", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false),
                    EscalationReason = table.Column<int>(type: "integer", nullable: false),
                    PlannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAiPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    MaxDailyQueries = table.Column<int>(type: "integer", nullable: false),
                    MaxTokensPerDay = table.Column<long>(type: "bigint", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedContexts = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAiPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAiProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxTokensPerRequest = table.Column<int>(type: "integer", nullable: false),
                    CostPerToken = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("PK_ExternalAiProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalDataSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ConnectorType = table.Column<int>(type: "integer", nullable: false),
                    ConnectorConfigJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    SyncIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSyncStatus = table.Column<string>(type: "text", nullable: true),
                    LastSyncError = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_ExternalDataSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalInferenceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    OriginalPrompt = table.Column<string>(type: "text", nullable: false),
                    AdditionalContext = table.Column<string>(type: "text", nullable: true),
                    Response = table.Column<string>(type: "text", nullable: false),
                    SensitivityClassification = table.Column<string>(type: "text", nullable: false),
                    QualityScore = table.Column<int>(type: "integer", nullable: true),
                    PromotionStatus = table.Column<int>(type: "integer", nullable: false),
                    CanPromoteToSharedMemory = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalInferenceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureModelBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureKey = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredModelName = table.Column<string>(type: "text", nullable: false),
                    RequiredProviderId = table.Column<string>(type: "text", nullable: false),
                    FallbackModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    FallbackModelName = table.Column<string>(type: "text", nullable: true),
                    FallbackProviderId = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureModelBindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    AgentName = table.Column<string>(type: "text", nullable: false),
                    ModelUsed = table.Column<string>(type: "text", nullable: false),
                    QueryCategory = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuardianAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    PatternDetected = table.Column<string>(type: "text", nullable: false),
                    Recommendation = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AcknowledgedBy = table.Column<string>(type: "text", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DismissReason = table.Column<string>(type: "text", nullable: true),
                    WasActualIssue = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardianAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guardrails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    GuardType = table.Column<int>(type: "integer", nullable: false),
                    Pattern = table.Column<string>(type: "text", nullable: false),
                    PatternType = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    UserMessage = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_Guardrails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdeCapabilityPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientType = table.Column<int>(type: "integer", nullable: false),
                    Persona = table.Column<string>(type: "text", nullable: true),
                    AllowedCommands = table.Column<string>(type: "text", nullable: false),
                    AllowedContextScopes = table.Column<string>(type: "text", nullable: false),
                    AllowedModelIds = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_IdeCapabilityPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdeClientRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    UserDisplayName = table.Column<string>(type: "text", nullable: false),
                    ClientType = table.Column<int>(type: "integer", nullable: false),
                    ClientVersion = table.Column<string>(type: "text", nullable: true),
                    DeviceIdentifier = table.Column<string>(type: "text", nullable: true),
                    LastAccessAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RevocationReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeClientRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdeQuerySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    IdeClient = table.Column<string>(type: "text", nullable: false),
                    IdeClientVersion = table.Column<string>(type: "text", nullable: false),
                    QueryType = table.Column<int>(type: "integer", nullable: false),
                    QueryText = table.Column<string>(type: "text", nullable: false),
                    QueryContext = table.Column<string>(type: "text", nullable: true),
                    ResponseText = table.Column<string>(type: "text", nullable: true),
                    ModelUsed = table.Column<string>(type: "text", nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GovernanceCheckResult = table.Column<string>(type: "text", nullable: true),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeQuerySessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeCaptureEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Relevance = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ValidatedBy = table.Column<string>(type: "text", nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SuggestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeCaptureEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeCaptures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ReuseCount = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeCaptures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    EndpointOrPath = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_KnowledgeSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemoryNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeType = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Context = table.Column<string>(type: "text", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    TagsJson = table.Column<string>(type: "text", nullable: false),
                    LinkedNodeIdsJson = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    SourceId = table.Column<string>(type: "text", nullable: true),
                    RelevanceScore = table.Column<double>(type: "double precision", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: true),
                    Provider = table.Column<string>(type: "text", nullable: true),
                    IsInternalModel = table.Column<bool>(type: "boolean", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    AppliedPolicyName = table.Column<string>(type: "text", nullable: true),
                    GroundingSources = table.Column<string>(type: "text", nullable: false),
                    ContextReferences = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelPredictionSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    PredictedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InputFeatureStatsJson = table.Column<string>(type: "text", nullable: true),
                    PredictedClass = table.Column<string>(type: "text", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: true),
                    InferenceLatencyMs = table.Column<int>(type: "integer", nullable: true),
                    ActualClass = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_ModelPredictionSamples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelRoutingPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Intent = table.Column<int>(type: "integer", nullable: false),
                    PreferredModelName = table.Column<string>(type: "text", nullable: false),
                    FallbackModelName = table.Column<string>(type: "text", nullable: true),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    MaxCostPerRequestUsd = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelRoutingPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ExternalModelId = table.Column<string>(type: "text", nullable: false),
                    ModelType = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsInstalled = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Capabilities = table.Column<string>(type: "text", nullable: false),
                    DefaultUseCases = table.Column<string>(type: "text", nullable: false),
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
                    RecommendedRamGb = table.Column<decimal>(type: "numeric", nullable: true),
                    LicenseName = table.Column<string>(type: "text", nullable: false),
                    LicenseUrl = table.Column<string>(type: "text", nullable: false),
                    ComplianceStatus = table.Column<string>(type: "text", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    UserDisplayName = table.Column<string>(type: "text", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: false),
                    ExperienceLevel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChecklistItems = table.Column<string>(type: "text", nullable: false),
                    CompletedItems = table.Column<int>(type: "integer", nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    ServicesExplored = table.Column<string>(type: "text", nullable: true),
                    ContractsReviewed = table.Column<string>(type: "text", nullable: true),
                    RunbooksRead = table.Column<string>(type: "text", nullable: true),
                    AiInteractionCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrchestrationConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Topic = table.Column<string>(type: "text", nullable: false),
                    TurnCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedBy = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastTurnAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrchestrationConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    Variables = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CurrentVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Variables = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetPersonas = table.Column<string>(type: "text", nullable: false),
                    ScopeHint = table.Column<string>(type: "text", nullable: true),
                    Relevance = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    ProviderType = table.Column<string>(type: "text", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    IsLocal = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "integer", nullable: false),
                    SupportedCapabilities = table.Column<string>(type: "text", nullable: false),
                    SupportsChat = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsEmbeddings = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsTools = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsVision = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsStructuredOutput = table.Column<bool>(type: "boolean", nullable: false),
                    HealthStatus = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutingDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    Persona = table.Column<string>(type: "text", nullable: false),
                    UseCaseType = table.Column<int>(type: "integer", nullable: false),
                    ClientType = table.Column<string>(type: "text", nullable: false),
                    SelectedPath = table.Column<int>(type: "integer", nullable: false),
                    SelectedModelName = table.Column<string>(type: "text", nullable: false),
                    SelectedProvider = table.Column<string>(type: "text", nullable: false),
                    IsInternalModel = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedStrategyId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliedPolicyName = table.Column<string>(type: "text", nullable: true),
                    EscalationReason = table.Column<int>(type: "integer", nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    EstimatedCostClass = table.Column<string>(type: "text", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false),
                    SelectedSources = table.Column<string>(type: "text", nullable: false),
                    SourceWeightingSummary = table.Column<string>(type: "text", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutingStrategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TargetPersona = table.Column<string>(type: "text", nullable: false),
                    TargetUseCase = table.Column<string>(type: "text", nullable: false),
                    TargetClientType = table.Column<string>(type: "text", nullable: false),
                    PreferredPath = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_RoutingStrategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SelfHealingActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Result = table.Column<string>(type: "text", nullable: true),
                    AuditTrailJson = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfHealingActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutedBy = table.Column<string>(type: "text", nullable: false),
                    ModelUsed = table.Column<string>(type: "text", nullable: false),
                    InputJson = table.Column<string>(type: "text", nullable: false),
                    OutputJson = table.Column<string>(type: "text", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_SkillExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ActualOutcome = table.Column<string>(type: "text", nullable: true),
                    WasCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedBy = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_SkillFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SkillContent = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    OwnershipType = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    RequiredTools = table.Column<string>(type: "text", nullable: false),
                    PreferredModels = table.Column<string>(type: "text", nullable: false),
                    InputSchema = table.Column<string>(type: "text", nullable: false),
                    OutputSchema = table.Column<string>(type: "text", nullable: false),
                    ExecutionCount = table.Column<long>(type: "bigint", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    ParentAgentId = table.Column<string>(type: "text", nullable: true),
                    IsComposable = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    OwnerTeamId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConnectionInfo = table.Column<string>(type: "text", nullable: false),
                    AccessPolicyScope = table.Column<string>(type: "text", nullable: false),
                    Classification = table.Column<string>(type: "text", nullable: false),
                    OwnerTeam = table.Column<string>(type: "text", nullable: false),
                    HealthStatus = table.Column<string>(type: "text", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourceWeights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    UseCaseType = table.Column<int>(type: "integer", nullable: false),
                    Relevance = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_SourceWeights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    TestFramework = table.Column<string>(type: "text", nullable: false),
                    GeneratedCode = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestArtifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenQuotaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Scope = table.Column<string>(type: "text", nullable: false),
                    ScopeValue = table.Column<string>(type: "text", nullable: false),
                    ProviderId = table.Column<string>(type: "text", nullable: true),
                    ModelId = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_TokenQuotaPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenUsageLedger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<string>(type: "text", nullable: false),
                    ModelId = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyName = table.Column<string>(type: "text", nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockReason = table.Column<string>(type: "text", nullable: true),
                    RequestId = table.Column<string>(type: "text", nullable: false),
                    ExecutionId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DurationMs = table.Column<double>(type: "double precision", nullable: false),
                    CostPerInputToken = table.Column<decimal>(type: "numeric", nullable: true),
                    CostPerOutputToken = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedCostUsd = table.Column<decimal>(type: "numeric", nullable: true),
                    CostCurrency = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenUsageLedger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    ParametersSchema = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_ToolDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsageEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    UserDisplayName = table.Column<string>(type: "text", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyName = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContextScope = table.Column<string>(type: "text", nullable: false),
                    ClientType = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "text", nullable: false),
                    IncidentTitle = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ServiceAffected = table.Column<string>(type: "text", nullable: false),
                    CreatedByAgentId = table.Column<string>(type: "text", nullable: false),
                    ParticipantsJson = table.Column<string>(type: "text", nullable: false),
                    TimelineJson = table.Column<string>(type: "text", nullable: false),
                    SuggestedActionsJson = table.Column<string>(type: "text", nullable: false),
                    PostMortemDraft = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SkillUsed = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InitialInput = table.Column<string>(type: "text", nullable: false),
                    FinalOutput = table.Column<string>(type: "text", nullable: false),
                    StepResultsJson = table.Column<string>(type: "text", nullable: false),
                    TotalSteps = table.Column<int>(type: "integer", nullable: false),
                    SuccessfulSteps = table.Column<int>(type: "integer", nullable: false),
                    TotalRetries = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    CallerTeamId = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ChangeNotes = table.Column<string>(type: "text", nullable: false),
                    EvalScore = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    PromptAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptVersions_PromptAssets_PromptAssetId",
                        column: x => x.PromptAssetId,
                        principalTable: "PromptAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_aik_hub_outbox_messages_CreatedAt",
                table: "aik_hub_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_hub_outbox_messages_IdempotencyKey",
                table: "aik_hub_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_hub_outbox_messages_ProcessedAt",
                table: "aik_hub_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PromptVersions_PromptAssetId",
                table: "PromptVersions",
                column: "PromptAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessPolicies");

            migrationBuilder.DropTable(
                name: "AgentArtifacts");

            migrationBuilder.DropTable(
                name: "AgentExecutionPlans");

            migrationBuilder.DropTable(
                name: "AgentExecutions");

            migrationBuilder.DropTable(
                name: "AgentPerformanceMetrics");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "AgentTrajectoryFeedbacks");

            migrationBuilder.DropTable(
                name: "aik_hub_outbox_messages");

            migrationBuilder.DropTable(
                name: "Budgets");

            migrationBuilder.DropTable(
                name: "ChangeConfidenceScores");

            migrationBuilder.DropTable(
                name: "Consultations");

            migrationBuilder.DropTable(
                name: "Contexts");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "EvalDatasets");

            migrationBuilder.DropTable(
                name: "EvalRuns");

            migrationBuilder.DropTable(
                name: "EvaluationCases");

            migrationBuilder.DropTable(
                name: "EvaluationDatasets");

            migrationBuilder.DropTable(
                name: "EvaluationRuns");

            migrationBuilder.DropTable(
                name: "Evaluations");

            migrationBuilder.DropTable(
                name: "EvaluationSuites");

            migrationBuilder.DropTable(
                name: "ExecutionPlans");

            migrationBuilder.DropTable(
                name: "ExternalAiPolicies");

            migrationBuilder.DropTable(
                name: "ExternalAiProviders");

            migrationBuilder.DropTable(
                name: "ExternalDataSources");

            migrationBuilder.DropTable(
                name: "ExternalInferenceRecords");

            migrationBuilder.DropTable(
                name: "FeatureModelBindings");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "GuardianAlerts");

            migrationBuilder.DropTable(
                name: "Guardrails");

            migrationBuilder.DropTable(
                name: "IdeCapabilityPolicies");

            migrationBuilder.DropTable(
                name: "IdeClientRegistrations");

            migrationBuilder.DropTable(
                name: "IdeQuerySessions");

            migrationBuilder.DropTable(
                name: "KnowledgeCaptureEntries");

            migrationBuilder.DropTable(
                name: "KnowledgeCaptures");

            migrationBuilder.DropTable(
                name: "KnowledgeSources");

            migrationBuilder.DropTable(
                name: "MemoryNodes");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "ModelPredictionSamples");

            migrationBuilder.DropTable(
                name: "ModelRoutingPolicies");

            migrationBuilder.DropTable(
                name: "Models");

            migrationBuilder.DropTable(
                name: "OnboardingSessions");

            migrationBuilder.DropTable(
                name: "OrchestrationConversations");

            migrationBuilder.DropTable(
                name: "PromptTemplates");

            migrationBuilder.DropTable(
                name: "PromptVersions");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "RoutingDecisions");

            migrationBuilder.DropTable(
                name: "RoutingStrategies");

            migrationBuilder.DropTable(
                name: "SelfHealingActions");

            migrationBuilder.DropTable(
                name: "SkillExecutions");

            migrationBuilder.DropTable(
                name: "SkillFeedbacks");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropTable(
                name: "SourceWeights");

            migrationBuilder.DropTable(
                name: "TestArtifacts");

            migrationBuilder.DropTable(
                name: "TokenQuotaPolicies");

            migrationBuilder.DropTable(
                name: "TokenUsageLedger");

            migrationBuilder.DropTable(
                name: "ToolDefinitions");

            migrationBuilder.DropTable(
                name: "UsageEntries");

            migrationBuilder.DropTable(
                name: "WarRooms");

            migrationBuilder.DropTable(
                name: "WorkflowExecutions");

            migrationBuilder.DropTable(
                name: "PromptAssets");
        }
    }
}
