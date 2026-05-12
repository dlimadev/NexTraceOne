using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ops_anomaly_narratives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriftFindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    NarrativeText = table.Column<string>(type: "text", nullable: false),
                    SymptomsSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    BaselineComparisonSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    ProbableCauseSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CorrelatedChangesSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    RecommendedActionsSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    SeverityJustificationSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    ModelUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefreshCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_anomaly_narratives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_chaos_experiments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExperimentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    TargetPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Steps = table.Column<string>(type: "jsonb", nullable: false),
                    SafetyChecks = table.Column<string>(type: "jsonb", nullable: false),
                    ExecutionNotes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_chaos_experiments", x => x.Id);
                    table.CheckConstraint("CK_ops_chaos_experiments_duration", "\"DurationSeconds\" >= 10 AND \"DurationSeconds\" <= 3600");
                    table.CheckConstraint("CK_ops_chaos_experiments_status", "\"Status\" >= 0 AND \"Status\" <= 4");
                    table.CheckConstraint("CK_ops_chaos_experiments_target_pct", "\"TargetPercentage\" >= 1 AND \"TargetPercentage\" <= 100");
                });

            migrationBuilder.CreateTable(
                name: "ops_custom_charts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ChartType = table.Column<int>(type: "integer", nullable: false),
                    MetricQuery = table.Column<string>(type: "text", nullable: false),
                    TimeRange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FiltersJson = table.Column<string>(type: "text", nullable: true),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_custom_charts", x => x.Id);
                    table.CheckConstraint("CK_ops_custom_charts_chart_type", "\"ChartType\" >= 0 AND \"ChartType\" <= 6");
                });

            migrationBuilder.CreateTable(
                name: "ops_drift_findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MetricName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpectedValue = table.Column<decimal>(type: "numeric", nullable: false),
                    ActualValue = table.Column<decimal>(type: "numeric", nullable: false),
                    DeviationPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolutionComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_drift_findings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_environment_drift_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEnvironment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetEnvironment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnalyzedDimensions = table.Column<string>(type: "jsonb", nullable: false),
                    ServiceVersionDrifts = table.Column<string>(type: "jsonb", nullable: true),
                    ConfigurationDrifts = table.Column<string>(type: "jsonb", nullable: true),
                    ContractVersionDrifts = table.Column<string>(type: "jsonb", nullable: true),
                    DependencyDrifts = table.Column<string>(type: "jsonb", nullable: true),
                    PolicyDrifts = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    TotalDriftItems = table.Column<int>(type: "integer", nullable: false),
                    CriticalDriftItems = table.Column<int>(type: "integer", nullable: false),
                    OverallSeverity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_ops_environment_drift_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_observability_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HasTracing = table.Column<bool>(type: "boolean", nullable: false),
                    HasMetrics = table.Column<bool>(type: "boolean", nullable: false),
                    HasLogging = table.Column<bool>(type: "boolean", nullable: false),
                    HasAlerting = table.Column<bool>(type: "boolean", nullable: false),
                    HasDashboard = table.Column<bool>(type: "boolean", nullable: false),
                    ObservabilityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    LastAssessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_observability_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_operational_playbooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Steps = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LinkedServiceIds = table.Column<string>(type: "jsonb", nullable: true),
                    LinkedRunbookIds = table.Column<string>(type: "jsonb", nullable: true),
                    Tags = table.Column<string>(type: "jsonb", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_operational_playbooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_playbook_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaybookName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecutedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StepResults = table.Column<string>(type: "jsonb", nullable: true),
                    Evidence = table.Column<string>(type: "jsonb", nullable: true),
                    Notes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_playbook_executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_profiling_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FrameType = table.Column<int>(type: "integer", nullable: false),
                    WindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    TotalCpuSamples = table.Column<long>(type: "bigint", nullable: false),
                    PeakMemoryMb = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TopFramesJson = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                    RawDataUri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RawDataHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReleaseVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CommitSha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HasAnomalies = table.Column<bool>(type: "boolean", nullable: false),
                    PeakThreadCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_profiling_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_resilience_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChaosExperimentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExperimentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResilienceScore = table.Column<int>(type: "integer", nullable: false),
                    TheoreticalBlastRadius = table.Column<string>(type: "jsonb", nullable: true),
                    ActualBlastRadius = table.Column<string>(type: "jsonb", nullable: true),
                    BlastRadiusDeviation = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    TelemetryObservations = table.Column<string>(type: "jsonb", nullable: true),
                    LatencyImpactMs = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    ErrorRateImpact = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    RecoveryTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    Strengths = table.Column<string>(type: "jsonb", nullable: true),
                    Weaknesses = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_resilience_reports", x => x.Id);
                    table.CheckConstraint("CK_ops_resilience_reports_score", "\"ResilienceScore\" >= 0 AND \"ResilienceScore\" <= 100");
                });

            migrationBuilder.CreateTable(
                name: "ops_rt_outbox_messages",
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
                    table.PrimaryKey("PK_ops_rt_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_runtime_baselines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpectedAvgLatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedP99LatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedErrorRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedRequestsPerSecond = table.Column<decimal>(type: "numeric", nullable: false),
                    EstablishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataPointCount = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_runtime_baselines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_runtime_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AvgLatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    P99LatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    ErrorRate = table.Column<decimal>(type: "numeric", nullable: false),
                    RequestsPerSecond = table.Column<decimal>(type: "numeric", nullable: false),
                    CpuUsagePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    MemoryUsageMb = table.Column<decimal>(type: "numeric", nullable: false),
                    ActiveInstances = table.Column<int>(type: "integer", nullable: false),
                    HealthStatus = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_runtime_snapshots", x => x.Id);
                    table.CheckConstraint("CK_ops_runtime_snapshots_health", "\"HealthStatus\" >= 0 AND \"HealthStatus\" <= 3");
                });

            migrationBuilder.CreateTable(
                name: "ops_service_cost_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DomainName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    OriginalAmount = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TagsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_service_cost_allocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_slo_observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MetricName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ObservedValue = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    SloTarget = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_slo_observations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ops_anomaly_narratives_DriftFindingId",
                table: "ops_anomaly_narratives",
                column: "DriftFindingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ops_anomaly_narratives_tenant_id",
                table: "ops_anomaly_narratives",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_chaos_experiments_TenantId",
                table: "ops_chaos_experiments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_chaos_experiments_TenantId_ServiceName",
                table: "ops_chaos_experiments",
                columns: new[] { "TenantId", "ServiceName" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_chaos_experiments_TenantId_Status",
                table: "ops_chaos_experiments",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_custom_charts_IsShared",
                table: "ops_custom_charts",
                column: "IsShared");

            migrationBuilder.CreateIndex(
                name: "IX_ops_custom_charts_TenantId",
                table: "ops_custom_charts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_custom_charts_UserId_TenantId",
                table: "ops_custom_charts",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_drift_findings_IsResolved",
                table: "ops_drift_findings",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_ops_drift_findings_ServiceName_Environment_DetectedAt",
                table: "ops_drift_findings",
                columns: new[] { "ServiceName", "Environment", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_drift_findings_Severity",
                table: "ops_drift_findings",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_ops_environment_drift_reports_SourceEnvironment_TargetEnvir~",
                table: "ops_environment_drift_reports",
                columns: new[] { "SourceEnvironment", "TargetEnvironment", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_environment_drift_reports_Status",
                table: "ops_environment_drift_reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_ops_environment_drift_reports_tenant_id",
                table: "ops_environment_drift_reports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_observability_profiles_LastAssessedAt",
                table: "ops_observability_profiles",
                column: "LastAssessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_observability_profiles_ServiceName_Environment",
                table: "ops_observability_profiles",
                columns: new[] { "ServiceName", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ops_operational_playbooks_tenant_id",
                table: "ops_operational_playbooks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_operational_playbooks_tenant_status",
                table: "ops_operational_playbooks",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_playbook_executions_playbook_id",
                table: "ops_playbook_executions",
                column: "PlaybookId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_playbook_executions_playbook_started",
                table: "ops_playbook_executions",
                columns: new[] { "PlaybookId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_playbook_executions_tenant_id",
                table: "ops_playbook_executions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_profiling_sessions_has_anomalies",
                table: "ops_profiling_sessions",
                column: "HasAnomalies",
                filter: "\"HasAnomalies\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_ops_profiling_sessions_service_env_window",
                table: "ops_profiling_sessions",
                columns: new[] { "ServiceName", "Environment", "WindowStart" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_profiling_sessions_tenant_id",
                table: "ops_profiling_sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_resilience_reports_experiment_id",
                table: "ops_resilience_reports",
                column: "ChaosExperimentId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_resilience_reports_status",
                table: "ops_resilience_reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_ops_resilience_reports_tenant_id",
                table: "ops_resilience_reports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_resilience_reports_tenant_service",
                table: "ops_resilience_reports",
                columns: new[] { "TenantId", "ServiceName" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_rt_outbox_messages_CreatedAt",
                table: "ops_rt_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_rt_outbox_messages_IdempotencyKey",
                table: "ops_rt_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_rt_outbox_messages_ProcessedAt",
                table: "ops_rt_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_runtime_baselines_EstablishedAt",
                table: "ops_runtime_baselines",
                column: "EstablishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_runtime_baselines_ServiceName_Environment",
                table: "ops_runtime_baselines",
                columns: new[] { "ServiceName", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_runtime_snapshots_HealthStatus",
                table: "ops_runtime_snapshots",
                column: "HealthStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ops_runtime_snapshots_ServiceName_Environment_CapturedAt",
                table: "ops_runtime_snapshots",
                columns: new[] { "ServiceName", "Environment", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_cost_alloc_category",
                table: "ops_service_cost_allocations",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "ix_ops_cost_alloc_tenant_environment",
                table: "ops_service_cost_allocations",
                columns: new[] { "TenantId", "Environment" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_cost_alloc_tenant_service_period",
                table: "ops_service_cost_allocations",
                columns: new[] { "TenantId", "ServiceName", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_slo_obs_observed_at",
                table: "ops_slo_observations",
                column: "ObservedAt");

            migrationBuilder.CreateIndex(
                name: "ix_ops_slo_obs_tenant_service_period",
                table: "ops_slo_observations",
                columns: new[] { "TenantId", "ServiceName", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_slo_obs_tenant_status",
                table: "ops_slo_observations",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_anomaly_narratives");

            migrationBuilder.DropTable(
                name: "ops_chaos_experiments");

            migrationBuilder.DropTable(
                name: "ops_custom_charts");

            migrationBuilder.DropTable(
                name: "ops_drift_findings");

            migrationBuilder.DropTable(
                name: "ops_environment_drift_reports");

            migrationBuilder.DropTable(
                name: "ops_observability_profiles");

            migrationBuilder.DropTable(
                name: "ops_operational_playbooks");

            migrationBuilder.DropTable(
                name: "ops_playbook_executions");

            migrationBuilder.DropTable(
                name: "ops_profiling_sessions");

            migrationBuilder.DropTable(
                name: "ops_resilience_reports");

            migrationBuilder.DropTable(
                name: "ops_rt_outbox_messages");

            migrationBuilder.DropTable(
                name: "ops_runtime_baselines");

            migrationBuilder.DropTable(
                name: "ops_runtime_snapshots");

            migrationBuilder.DropTable(
                name: "ops_service_cost_allocations");

            migrationBuilder.DropTable(
                name: "ops_slo_observations");
        }
    }
}
