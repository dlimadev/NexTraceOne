using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cg_external_change_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ChangeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ScheduledStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScheduledEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LinkedReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cg_external_change_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_approval_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalType = table.Column<int>(type: "integer", nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalRequestId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CallbackTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CallbackTokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TargetEnvironment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OutboundWebhookUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RespondedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_approval_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_benchmark_consents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConsentedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsentedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LgpdLawfulBasis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_benchmark_consents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_benchmark_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeploymentFrequencyPerWeek = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    LeadTimeForChangesHours = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    ChangeFailureRatePercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    MeanTimeToRestoreHours = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    MaturityScore = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    CostPerRequestUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    ServiceCount = table.Column<int>(type: "integer", nullable: false),
                    IsAnonymizedForBenchmarks = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_benchmark_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_blast_radius_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAffectedConsumers = table.Column<int>(type: "integer", nullable: false),
                    DirectConsumers = table.Column<string>(type: "text", nullable: false),
                    TransitiveConsumers = table.Column<string>(type: "text", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_blast_radius_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_canary_rollouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolloutPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ActiveInstances = table.Column<int>(type: "integer", nullable: false),
                    TotalInstances = table.Column<int>(type: "integer", nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsPromoted = table.Column<bool>(type: "boolean", nullable: false),
                    IsAborted = table.Column<bool>(type: "boolean", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_canary_rollouts", x => x.Id);
                    table.CheckConstraint("CK_chg_canary_rollouts_active_instances", "\"ActiveInstances\" >= 0");
                    table.CheckConstraint("CK_chg_canary_rollouts_rollout_percentage", "\"RolloutPercentage\" >= 0 AND \"RolloutPercentage\" <= 100");
                    table.CheckConstraint("CK_chg_canary_rollouts_total_instances", "\"TotalInstances\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "chg_change_confidence_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfidenceBefore = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceAfter = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Details = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_change_confidence_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_change_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_change_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_change_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    BreakingChangeWeight = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    BlastRadiusWeight = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    EnvironmentWeight = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScoreSource = table.Column<string>(type: "text", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_change_scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_commit_associations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitSha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CommitMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CommitAuthor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CommittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BranchName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignmentStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AssignmentSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExtractedWorkItemRefs = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_commit_associations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_confidence_breakdowns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregatedScore = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScoreVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_confidence_breakdowns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_external_markers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_external_markers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_feature_flag_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActiveFlagCount = table.Column<int>(type: "integer", nullable: false),
                    CriticalFlagCount = table.Column<int>(type: "integer", nullable: false),
                    NewFeatureFlagCount = table.Column<int>(type: "integer", nullable: false),
                    FlagProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FlagsJson = table.Column<string>(type: "jsonb", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_feature_flag_states", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_freeze_windows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_freeze_windows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_observation_windows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestsPerMinute = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ErrorRate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    AvgLatencyMs = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    P95LatencyMs = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    P99LatencyMs = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Throughput = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    IsCollected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_observation_windows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_outbox_messages",
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
                    table.PrimaryKey("PK_chg_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_post_release_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPhase = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_post_release_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_promotion_gate_evaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RuleResults = table.Column<string>(type: "jsonb", nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EvaluatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_promotion_gate_evaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_promotion_gates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EnvironmentFrom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnvironmentTo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rules = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    BlockOnFailure = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_promotion_gates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_release_approval_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    EnvironmentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceTag = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ApprovalType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalWebhookUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MinApprovers = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ApproverGroupsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: "[]"),
                    BypassRolesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: "[]"),
                    ExpirationHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 48),
                    RequireEvidencePack = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RequireChecklistCompletion = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MinRiskScoreForManualApproval = table.Column<int>(type: "integer", nullable: true),
                    BlockedTimeWindowsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: "[]"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_release_approval_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_release_baselines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestsPerMinute = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ErrorRate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    AvgLatencyMs = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    P95LatencyMs = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    P99LatencyMs = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Throughput = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CollectedFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CollectedTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_release_baselines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_release_calendar_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WindowType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EnvironmentFilter = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecurrenceTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClosedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_release_calendar_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_release_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalSummary = table.Column<string>(type: "text", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "text", nullable: true),
                    NewEndpointsSection = table.Column<string>(type: "text", nullable: true),
                    BreakingChangesSection = table.Column<string>(type: "text", nullable: true),
                    AffectedServicesSection = table.Column<string>(type: "text", nullable: true),
                    ConfidenceMetricsSection = table.Column<string>(type: "text", nullable: true),
                    EvidenceLinksSection = table.Column<string>(type: "text", nullable: true),
                    ModelUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_chg_release_notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PipelineSource = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CommitSha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChangeLevel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ChangeScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.0m),
                    WorkItemReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RolledBackFromReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceStatus = table.Column<int>(type: "integer", nullable: false),
                    ValidationStatus = table.Column<int>(type: "integer", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: true),
                    Domain = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    slsa_provenance_uri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    artifact_digest = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sbom_uri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalReleaseId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExternalSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReleaseName = table.Column<string>(type: "text", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "text", nullable: true),
                    HasBreakingChanges = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalValidationPassed = table.Column<bool>(type: "boolean", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_releases", x => x.Id);
                    table.CheckConstraint("CK_chg_releases_change_level", "\"ChangeLevel\" >= 0 AND \"ChangeLevel\" <= 4");
                    table.CheckConstraint("CK_chg_releases_change_score", "\"ChangeScore\" >= 0.0 AND \"ChangeScore\" <= 1.0");
                    table.CheckConstraint("CK_chg_releases_status", "\"Status\" >= 0 AND \"Status\" <= 4");
                });

            migrationBuilder.CreateTable(
                name: "chg_rollback_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsViable = table.Column<bool>(type: "boolean", nullable: false),
                    ReadinessScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    PreviousVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HasReversibleMigrations = table.Column<bool>(type: "boolean", nullable: false),
                    ConsumersAlreadyMigrated = table.Column<int>(type: "integer", nullable: false),
                    TotalConsumersImpacted = table.Column<int>(type: "integer", nullable: false),
                    InviabilityReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Recommendation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AssessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_rollback_assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_service_risk_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
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
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_service_risk_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_workitem_associations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalWorkItemId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalSystem = table.Column<int>(type: "integer", nullable: false, defaultValue: 99),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    WorkItemType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AddedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RemovedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_workitem_associations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_confidence_sub_scores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubScoreType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    Confidence = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Citations = table.Column<string>(type: "jsonb", nullable: false),
                    SimulatedNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BreakdownId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_confidence_sub_scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chg_confidence_sub_scores_chg_confidence_breakdowns_Breakdo~",
                        column: x => x.BreakdownId,
                        principalTable: "chg_confidence_breakdowns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cg_external_change_requests_external_key",
                table: "cg_external_change_requests",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cg_external_change_requests_service_id",
                table: "cg_external_change_requests",
                column: "ServiceId",
                filter: "\"ServiceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cg_external_change_requests_status",
                table: "cg_external_change_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_chg_approval_requests_release_id",
                table: "chg_approval_requests",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_approval_requests_tenant_id",
                table: "chg_approval_requests",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uix_chg_approval_requests_token_hash",
                table: "chg_approval_requests",
                column: "CallbackTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chg_benchmark_consents_tenant_id",
                table: "chg_benchmark_consents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_benchmark_snapshots_anonymized",
                table: "chg_benchmark_snapshots",
                column: "IsAnonymizedForBenchmarks",
                filter: "\"IsAnonymizedForBenchmarks\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_chg_benchmark_snapshots_period",
                table: "chg_benchmark_snapshots",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_benchmark_snapshots_tenant_id",
                table: "chg_benchmark_snapshots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_blast_radius_reports_ReleaseId",
                table: "chg_blast_radius_reports",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_canary_rollouts_ReleaseId",
                table: "chg_canary_rollouts",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_change_confidence_events_OccurredAt",
                table: "chg_change_confidence_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_change_confidence_events_ReleaseId",
                table: "chg_change_confidence_events",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_change_events_ReleaseId",
                table: "chg_change_events",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_change_scores_ReleaseId",
                table: "chg_change_scores",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_commit_assoc_release_id",
                table: "chg_commit_associations",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_commit_assoc_service_status",
                table: "chg_commit_associations",
                columns: new[] { "ServiceName", "AssignmentStatus" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_commit_assoc_tenant_id",
                table: "chg_commit_associations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uix_chg_commit_assoc_sha_service_tenant",
                table: "chg_commit_associations",
                columns: new[] { "CommitSha", "ServiceName", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chg_confidence_breakdowns_ReleaseId",
                table: "chg_confidence_breakdowns",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_confidence_sub_scores_BreakdownId",
                table: "chg_confidence_sub_scores",
                column: "BreakdownId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_external_markers_ReleaseId",
                table: "chg_external_markers",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_feature_flag_states_ReleaseId",
                table: "chg_feature_flag_states",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_freeze_windows_StartsAt_EndsAt_IsActive",
                table: "chg_freeze_windows",
                columns: new[] { "StartsAt", "EndsAt", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_chg_observation_windows_ReleaseId_Phase",
                table: "chg_observation_windows",
                columns: new[] { "ReleaseId", "Phase" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chg_outbox_messages_CreatedAt",
                table: "chg_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_outbox_messages_IdempotencyKey",
                table: "chg_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chg_outbox_messages_ProcessedAt",
                table: "chg_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_post_release_reviews_ReleaseId",
                table: "chg_post_release_reviews",
                column: "ReleaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gate_evaluations_ChangeId",
                table: "chg_promotion_gate_evaluations",
                column: "ChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gate_evaluations_EvaluatedAt",
                table: "chg_promotion_gate_evaluations",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gate_evaluations_GateId",
                table: "chg_promotion_gate_evaluations",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gate_evaluations_TenantId",
                table: "chg_promotion_gate_evaluations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gates_EnvironmentFrom_EnvironmentTo",
                table: "chg_promotion_gates",
                columns: new[] { "EnvironmentFrom", "EnvironmentTo" });

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gates_IsActive",
                table: "chg_promotion_gates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gates_TenantId",
                table: "chg_promotion_gates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_approval_policies_env_service_tenant",
                table: "chg_release_approval_policies",
                columns: new[] { "EnvironmentId", "ServiceId", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_approval_policies_tenant_active",
                table: "chg_release_approval_policies",
                columns: new[] { "tenant_id", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_approval_policies_tenant_id",
                table: "chg_release_approval_policies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_chg_release_baselines_ReleaseId",
                table: "chg_release_baselines",
                column: "ReleaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chg_release_calendar_tenant_id",
                table: "chg_release_calendar_entries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_release_calendar_tenant_period",
                table: "chg_release_calendar_entries",
                columns: new[] { "TenantId", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_release_calendar_tenant_type_status",
                table: "chg_release_calendar_entries",
                columns: new[] { "TenantId", "WindowType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_chg_release_notes_GeneratedAt",
                table: "chg_release_notes",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_release_notes_ReleaseId",
                table: "chg_release_notes",
                column: "ReleaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chg_releases_ApiAssetId",
                table: "chg_releases",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_releases_artifact_digest",
                table: "chg_releases",
                column: "artifact_digest",
                filter: "\"artifact_digest\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chg_releases_external_key",
                table: "chg_releases",
                columns: new[] { "ExternalReleaseId", "ExternalSystem" },
                filter: "\"ExternalReleaseId\" IS NOT NULL AND \"ExternalSystem\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chg_releases_tenant_environment",
                table: "chg_releases",
                columns: new[] { "tenant_id", "environment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_releases_tenant_id",
                table: "chg_releases",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_chg_rollback_assessments_ReleaseId",
                table: "chg_rollback_assessments",
                column: "ReleaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chg_risk_profiles_tenant_risk_level",
                table: "chg_service_risk_profiles",
                columns: new[] { "TenantId", "OverallRiskLevel" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_risk_profiles_tenant_score",
                table: "chg_service_risk_profiles",
                columns: new[] { "TenantId", "OverallScore" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_risk_profiles_tenant_service_computed",
                table: "chg_service_risk_profiles",
                columns: new[] { "TenantId", "ServiceAssetId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_workitem_assoc_release_id",
                table: "chg_workitem_associations",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_workitem_assoc_release_item_active",
                table: "chg_workitem_associations",
                columns: new[] { "ReleaseId", "ExternalWorkItemId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_workitem_assoc_tenant_id",
                table: "chg_workitem_associations",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cg_external_change_requests");

            migrationBuilder.DropTable(
                name: "chg_approval_requests");

            migrationBuilder.DropTable(
                name: "chg_benchmark_consents");

            migrationBuilder.DropTable(
                name: "chg_benchmark_snapshots");

            migrationBuilder.DropTable(
                name: "chg_blast_radius_reports");

            migrationBuilder.DropTable(
                name: "chg_canary_rollouts");

            migrationBuilder.DropTable(
                name: "chg_change_confidence_events");

            migrationBuilder.DropTable(
                name: "chg_change_events");

            migrationBuilder.DropTable(
                name: "chg_change_scores");

            migrationBuilder.DropTable(
                name: "chg_commit_associations");

            migrationBuilder.DropTable(
                name: "chg_confidence_sub_scores");

            migrationBuilder.DropTable(
                name: "chg_external_markers");

            migrationBuilder.DropTable(
                name: "chg_feature_flag_states");

            migrationBuilder.DropTable(
                name: "chg_freeze_windows");

            migrationBuilder.DropTable(
                name: "chg_observation_windows");

            migrationBuilder.DropTable(
                name: "chg_outbox_messages");

            migrationBuilder.DropTable(
                name: "chg_post_release_reviews");

            migrationBuilder.DropTable(
                name: "chg_promotion_gate_evaluations");

            migrationBuilder.DropTable(
                name: "chg_promotion_gates");

            migrationBuilder.DropTable(
                name: "chg_release_approval_policies");

            migrationBuilder.DropTable(
                name: "chg_release_baselines");

            migrationBuilder.DropTable(
                name: "chg_release_calendar_entries");

            migrationBuilder.DropTable(
                name: "chg_release_notes");

            migrationBuilder.DropTable(
                name: "chg_releases");

            migrationBuilder.DropTable(
                name: "chg_rollback_assessments");

            migrationBuilder.DropTable(
                name: "chg_service_risk_profiles");

            migrationBuilder.DropTable(
                name: "chg_workitem_associations");

            migrationBuilder.DropTable(
                name: "chg_confidence_breakdowns");
        }
    }
}
