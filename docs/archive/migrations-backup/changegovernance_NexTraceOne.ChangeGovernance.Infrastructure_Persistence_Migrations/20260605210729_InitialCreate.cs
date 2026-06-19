using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecidedBy = table.Column<string>(type: "text", nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    EnvironmentId = table.Column<string>(type: "text", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceTag = table.Column<string>(type: "text", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalType = table.Column<string>(type: "text", nullable: false),
                    ExternalWebhookUrl = table.Column<string>(type: "text", nullable: true),
                    MinApprovers = table.Column<int>(type: "integer", nullable: false),
                    ApproverGroupsJson = table.Column<string>(type: "text", nullable: false),
                    BypassRolesJson = table.Column<string>(type: "text", nullable: false),
                    ExpirationHours = table.Column<int>(type: "integer", nullable: false),
                    RequireEvidencePack = table.Column<bool>(type: "boolean", nullable: false),
                    RequireChecklistCompletion = table.Column<bool>(type: "boolean", nullable: false),
                    MinRiskScoreForManualApproval = table.Column<int>(type: "integer", nullable: true),
                    BlockedTimeWindowsJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalType = table.Column<int>(type: "integer", nullable: false),
                    ExternalSystem = table.Column<string>(type: "text", nullable: true),
                    ExternalRequestId = table.Column<string>(type: "text", nullable: true),
                    CallbackTokenHash = table.Column<string>(type: "text", nullable: false),
                    CallbackTokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TargetEnvironment = table.Column<string>(type: "text", nullable: false),
                    OutboundWebhookUrl = table.Column<string>(type: "text", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RespondedBy = table.Column<string>(type: "text", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConsentedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsentedByUserId = table.Column<string>(type: "text", nullable: true),
                    LgpdLawfulBasis = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkConsents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeploymentFrequencyPerWeek = table.Column<decimal>(type: "numeric", nullable: false),
                    LeadTimeForChangesHours = table.Column<decimal>(type: "numeric", nullable: false),
                    ChangeFailureRatePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    MeanTimeToRestoreHours = table.Column<decimal>(type: "numeric", nullable: false),
                    MaturityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CostPerRequestUsd = table.Column<decimal>(type: "numeric", nullable: true),
                    ServiceCount = table.Column<int>(type: "integer", nullable: false),
                    IsAnonymizedForBenchmarks = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlastRadiusReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAffectedConsumers = table.Column<int>(type: "integer", nullable: false),
                    DirectConsumers = table.Column<string[]>(type: "text[]", nullable: false),
                    TransitiveConsumers = table.Column<string[]>(type: "text[]", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlastRadiusReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanaryRollouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RolloutPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    ActiveInstances = table.Column<int>(type: "integer", nullable: false),
                    TotalInstances = table.Column<int>(type: "integer", nullable: false),
                    SourceSystem = table.Column<string>(type: "text", nullable: false),
                    IsPromoted = table.Column<bool>(type: "boolean", nullable: false),
                    IsAborted = table.Column<bool>(type: "boolean", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanaryRollouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeConfidenceEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceBefore = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceAfter = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeConfidenceEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    BreakingChangeWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    BlastRadiusWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    EnvironmentWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScoreSource = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_hub_outbox_messages",
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
                    table.PrimaryKey("PK_chg_hub_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CiPromotionGates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EnvironmentFrom = table.Column<string>(type: "text", nullable: false),
                    EnvironmentTo = table.Column<string>(type: "text", nullable: false),
                    Rules = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    BlockOnFailure = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CiPromotionGates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommitAssociations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitSha = table.Column<string>(type: "text", nullable: false),
                    CommitMessage = table.Column<string>(type: "text", nullable: false),
                    CommitAuthor = table.Column<string>(type: "text", nullable: false),
                    CommittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BranchName = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentStatus = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignedBy = table.Column<string>(type: "text", nullable: true),
                    AssignmentSource = table.Column<string>(type: "text", nullable: true),
                    ExtractedWorkItemRefs = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitAssociations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfidenceBreakdowns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregatedScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScoreVersion = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfidenceBreakdowns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentEnvironments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresEvidencePack = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentEnvironments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidencePacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractDiffSummary = table.Column<string>(type: "text", nullable: true),
                    BlastRadiusScore = table.Column<decimal>(type: "numeric", nullable: true),
                    SpectralScore = table.Column<decimal>(type: "numeric", nullable: true),
                    ChangeIntelligenceScore = table.Column<decimal>(type: "numeric", nullable: true),
                    ApprovalHistory = table.Column<string>(type: "text", nullable: true),
                    ContractHash = table.Column<string>(type: "text", nullable: true),
                    PipelineSource = table.Column<string>(type: "text", nullable: true),
                    BuildId = table.Column<string>(type: "text", nullable: true),
                    CommitSha = table.Column<string>(type: "text", nullable: true),
                    CiChecksResult = table.Column<string>(type: "text", nullable: true),
                    CompletenessPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IntegrityHash = table.Column<string>(type: "text", nullable: true),
                    IntegrityManifest = table.Column<string>(type: "text", nullable: true),
                    IntegritySignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IntegritySignedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidencePacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalChangeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSystem = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ChangeType = table.Column<string>(type: "text", nullable: false),
                    RequestedBy = table.Column<string>(type: "text", nullable: false),
                    ScheduledStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScheduledEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Environment = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LinkedReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalChangeRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalMarkers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkerType = table.Column<int>(type: "integer", nullable: false),
                    SourceSystem = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalMarkers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlagStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActiveFlagCount = table.Column<int>(type: "integer", nullable: false),
                    CriticalFlagCount = table.Column<int>(type: "integer", nullable: false),
                    NewFeatureFlagCount = table.Column<int>(type: "integer", nullable: false),
                    FlagProvider = table.Column<string>(type: "text", nullable: false),
                    FlagsJson = table.Column<string>(type: "text", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FreezeWindows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    ScopeValue = table.Column<string>(type: "text", nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreezeWindows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GateEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    EvaluatedBy = table.Column<string>(type: "text", nullable: false),
                    EvaluationDetails = table.Column<string>(type: "text", nullable: true),
                    OverrideJustification = table.Column<string>(type: "text", nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LintResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalFindings = table.Column<int>(type: "integer", nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LintResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObservationWindows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestsPerMinute = table.Column<decimal>(type: "numeric", nullable: true),
                    ErrorRate = table.Column<decimal>(type: "numeric", nullable: true),
                    AvgLatencyMs = table.Column<decimal>(type: "numeric", nullable: true),
                    P95LatencyMs = table.Column<decimal>(type: "numeric", nullable: true),
                    P99LatencyMs = table.Column<decimal>(type: "numeric", nullable: true),
                    Throughput = table.Column<decimal>(type: "numeric", nullable: true),
                    IsCollected = table.Column<bool>(type: "boolean", nullable: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservationWindows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostReleaseReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPhase = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostReleaseReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionGateEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeId = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    RuleResults = table.Column<string>(type: "text", nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EvaluatedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionGateEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionGates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GateName = table.Column<string>(type: "text", nullable: false),
                    GateType = table.Column<string>(type: "text", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionGates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Justification = table.Column<string>(type: "text", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseBaselines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestsPerMinute = table.Column<decimal>(type: "numeric", nullable: false),
                    ErrorRate = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgLatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    P95LatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    P99LatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    Throughput = table.Column<decimal>(type: "numeric", nullable: false),
                    CollectedFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CollectedTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseBaselines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseCalendarEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    WindowType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EnvironmentFilter = table.Column<string>(type: "text", nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecurrenceTag = table.Column<string>(type: "text", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "text", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseCalendarEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalSummary = table.Column<string>(type: "text", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "text", nullable: true),
                    NewEndpointsSection = table.Column<string>(type: "text", nullable: true),
                    BreakingChangesSection = table.Column<string>(type: "text", nullable: true),
                    AffectedServicesSection = table.Column<string>(type: "text", nullable: true),
                    ConfidenceMetricsSection = table.Column<string>(type: "text", nullable: true),
                    EvidenceLinksSection = table.Column<string>(type: "text", nullable: true),
                    ModelUsed = table.Column<string>(type: "text", nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRegeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RegenerationCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    PipelineSource = table.Column<string>(type: "text", nullable: false),
                    CommitSha = table.Column<string>(type: "text", nullable: false),
                    ChangeLevel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChangeScore = table.Column<decimal>(type: "numeric", nullable: false),
                    WorkItemReference = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceStatus = table.Column<int>(type: "integer", nullable: false),
                    ValidationStatus = table.Column<int>(type: "integer", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: true),
                    Domain = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SlsaProvenanceUri = table.Column<string>(type: "text", nullable: true),
                    ArtifactDigest = table.Column<string>(type: "text", nullable: true),
                    SbomUri = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalReleaseId = table.Column<string>(type: "text", nullable: true),
                    ExternalSystem = table.Column<string>(type: "text", nullable: true),
                    ReleaseName = table.Column<string>(type: "text", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "text", nullable: true),
                    HasBreakingChanges = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalValidationPassed = table.Column<bool>(type: "boolean", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Releases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RollbackAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsViable = table.Column<bool>(type: "boolean", nullable: false),
                    ReadinessScore = table.Column<decimal>(type: "numeric", nullable: false),
                    PreviousVersion = table.Column<string>(type: "text", nullable: true),
                    HasReversibleMigrations = table.Column<bool>(type: "boolean", nullable: false),
                    ConsumersAlreadyMigrated = table.Column<int>(type: "integer", nullable: false),
                    TotalConsumersImpacted = table.Column<int>(type: "integer", nullable: false),
                    InviabilityReason = table.Column<string>(type: "text", nullable: true),
                    Recommendation = table.Column<string>(type: "text", nullable: false),
                    AssessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RollbackAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RulesetBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetType = table.Column<string>(type: "text", nullable: false),
                    BindingCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulesetBindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rulesets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    RulesetType = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RulesetCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rulesets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRiskProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    OverallRiskLevel = table.Column<int>(type: "integer", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    VulnerabilityScore = table.Column<int>(type: "integer", nullable: false),
                    ChangeFailureScore = table.Column<int>(type: "integer", nullable: false),
                    BlastRadiusScore = table.Column<int>(type: "integer", nullable: false),
                    PolicyViolationScore = table.Column<int>(type: "integer", nullable: false),
                    ActiveSignalsJson = table.Column<string>(type: "text", nullable: false),
                    ActiveSignalCount = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRiskProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StageName = table.Column<string>(type: "text", nullable: false),
                    MaxDurationHours = table.Column<int>(type: "integer", nullable: false),
                    EscalationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EscalationTargetRole = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedBy = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentStageIndex = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StageOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequiredApprovers = table.Column<int>(type: "integer", nullable: false),
                    CurrentApprovals = table.Column<int>(type: "integer", nullable: false),
                    CommentRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SlaDurationHours = table.Column<int>(type: "integer", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ChangeType = table.Column<string>(type: "text", nullable: false),
                    ApiCriticality = table.Column<string>(type: "text", nullable: false),
                    TargetEnvironment = table.Column<string>(type: "text", nullable: false),
                    MinimumApprovers = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkItemAssociations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalWorkItemId = table.Column<string>(type: "text", nullable: false),
                    ExternalSystem = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    WorkItemType = table.Column<string>(type: "text", nullable: false),
                    ExternalStatus = table.Column<string>(type: "text", nullable: true),
                    ExternalUrl = table.Column<string>(type: "text", nullable: true),
                    AddedBy = table.Column<string>(type: "text", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RemovedBy = table.Column<string>(type: "text", nullable: true),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemAssociations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chg_hub_outbox_messages_CreatedAt",
                table: "chg_hub_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_hub_outbox_messages_IdempotencyKey",
                table: "chg_hub_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chg_hub_outbox_messages_ProcessedAt",
                table: "chg_hub_outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalDecisions");

            migrationBuilder.DropTable(
                name: "ApprovalPolicies");

            migrationBuilder.DropTable(
                name: "ApprovalRequests");

            migrationBuilder.DropTable(
                name: "BenchmarkConsents");

            migrationBuilder.DropTable(
                name: "BenchmarkSnapshots");

            migrationBuilder.DropTable(
                name: "BlastRadiusReports");

            migrationBuilder.DropTable(
                name: "CanaryRollouts");

            migrationBuilder.DropTable(
                name: "ChangeConfidenceEvents");

            migrationBuilder.DropTable(
                name: "ChangeEvents");

            migrationBuilder.DropTable(
                name: "ChangeScores");

            migrationBuilder.DropTable(
                name: "chg_hub_outbox_messages");

            migrationBuilder.DropTable(
                name: "CiPromotionGates");

            migrationBuilder.DropTable(
                name: "CommitAssociations");

            migrationBuilder.DropTable(
                name: "ConfidenceBreakdowns");

            migrationBuilder.DropTable(
                name: "DeploymentEnvironments");

            migrationBuilder.DropTable(
                name: "EvidencePacks");

            migrationBuilder.DropTable(
                name: "ExternalChangeRequests");

            migrationBuilder.DropTable(
                name: "ExternalMarkers");

            migrationBuilder.DropTable(
                name: "FeatureFlagStates");

            migrationBuilder.DropTable(
                name: "FreezeWindows");

            migrationBuilder.DropTable(
                name: "GateEvaluations");

            migrationBuilder.DropTable(
                name: "LintResults");

            migrationBuilder.DropTable(
                name: "ObservationWindows");

            migrationBuilder.DropTable(
                name: "PostReleaseReviews");

            migrationBuilder.DropTable(
                name: "PromotionGateEvaluations");

            migrationBuilder.DropTable(
                name: "PromotionGates");

            migrationBuilder.DropTable(
                name: "PromotionRequests");

            migrationBuilder.DropTable(
                name: "ReleaseBaselines");

            migrationBuilder.DropTable(
                name: "ReleaseCalendarEntries");

            migrationBuilder.DropTable(
                name: "ReleaseNotes");

            migrationBuilder.DropTable(
                name: "Releases");

            migrationBuilder.DropTable(
                name: "RollbackAssessments");

            migrationBuilder.DropTable(
                name: "RulesetBindings");

            migrationBuilder.DropTable(
                name: "Rulesets");

            migrationBuilder.DropTable(
                name: "ServiceRiskProfiles");

            migrationBuilder.DropTable(
                name: "SlaPolicies");

            migrationBuilder.DropTable(
                name: "WorkflowInstances");

            migrationBuilder.DropTable(
                name: "WorkflowStages");

            migrationBuilder.DropTable(
                name: "WorkflowTemplates");

            migrationBuilder.DropTable(
                name: "WorkItemAssociations");
        }
    }
}
