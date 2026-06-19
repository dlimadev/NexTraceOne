using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCostAndTelemetryEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Cost Intelligence tables (from CostIntelligenceDbContext) ──────────

            migrationBuilder.CreateTable(
                name: "oi_waste_signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    signal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    estimated_monthly_savings = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    team_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    detected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_waste_signals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oi_carbon_score_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CpuHours = table.Column<double>(type: "double precision", nullable: false),
                    MemoryGbHours = table.Column<double>(type: "double precision", nullable: false),
                    NetworkGb = table.Column<double>(type: "double precision", nullable: false),
                    CarbonGrams = table.Column<double>(type: "double precision", nullable: false),
                    IntensityFactor = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oi_carbon_score_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_attributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false),
                    CostPerRequest = table.Column<decimal>(type: "numeric", nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_attributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_budget_forecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ForecastPeriod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProjectedCost = table.Column<decimal>(type: "numeric", nullable: false),
                    BudgetLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    ConfidencePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    IsOverBudgetProjected = table.Column<bool>(type: "boolean", nullable: false),
                    ForecastNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_budget_forecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_efficiency_recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceCost = table.Column<decimal>(type: "numeric", nullable: false),
                    MedianPeerCost = table.Column<decimal>(type: "numeric", nullable: false),
                    DeviationPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    RecommendationText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_efficiency_recommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_import_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_import_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Team = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CpuCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    MemoryCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    NetworkCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    StorageCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_trends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AverageDailyCost = table.Column<decimal>(type: "numeric", nullable: false),
                    PeakDailyCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TrendDirection = table.Column<int>(type: "integer", nullable: false),
                    PercentageChange = table.Column<decimal>(type: "numeric", nullable: false),
                    DataPointCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_trends", x => x.Id);
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
                name: "ops_service_cost_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MonthlyBudget = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentMonthCost = table.Column<decimal>(type: "numeric", nullable: false),
                    AlertThresholdPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_service_cost_profiles", x => x.Id);
                });

            // ── TelemetryStore tables (from TelemetryStoreDbContext) ───────────────

            migrationBuilder.CreateTable(
                name: "ops_ts_anomaly_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnomalyType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MessageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ObservedValue = table.Column<double>(type: "double precision", nullable: false),
                    ExpectedValue = table.Column<double>(type: "double precision", nullable: false),
                    DeviationPercent = table.Column<double>(type: "double precision", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelatedReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_anomaly_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_dependency_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AggregationLevel = table.Column<int>(type: "integer", nullable: false),
                    IntervalStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IntervalEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CallCount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorRatePercent = table.Column<double>(type: "double precision", nullable: false),
                    LatencyAvgMs = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP99Ms = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_dependency_metrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_investigation_contexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TitleMessageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InvestigationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TimeWindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TimeWindowEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AnomalySnapshotIds = table.Column<string>(type: "jsonb", nullable: false),
                    ReleaseCorrelationIds = table.Column<string>(type: "jsonb", nullable: false),
                    TelemetryReferenceIds = table.Column<string>(type: "jsonb", nullable: false),
                    AffectedServiceIds = table.Column<string>(type: "jsonb", nullable: false),
                    AiSummaryJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_investigation_contexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_observed_topology",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CommunicationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalCallCount = table.Column<long>(type: "bigint", nullable: false),
                    IsShadowDependency = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_observed_topology", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_release_correlations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeployedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MarkerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreDeployErrorRate = table.Column<double>(type: "double precision", nullable: false),
                    PreDeployLatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    PreDeployRequestsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    PostDeployErrorRate = table.Column<double>(type: "double precision", nullable: false),
                    PostDeployLatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    PostDeployRequestsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    ImpactScore = table.Column<double>(type: "double precision", nullable: false),
                    ImpactClassification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TelemetryReferenceIds = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_release_correlations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_service_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AggregationLevel = table.Column<int>(type: "integer", nullable: false),
                    IntervalStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IntervalEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false),
                    RequestsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    RequestsPerHour = table.Column<double>(type: "double precision", nullable: false),
                    ErrorCount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorRatePercent = table.Column<double>(type: "double precision", nullable: false),
                    LatencyAvgMs = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP50Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP99Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyMaxMs = table.Column<double>(type: "double precision", nullable: false),
                    CpuAvgPercent = table.Column<double>(type: "double precision", nullable: true),
                    MemoryAvgMb = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_service_metrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_ts_telemetry_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalType = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BackendType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccessUri = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_telemetry_references", x => x.Id);
                });

            // ── Indexes ───────────────────────────────────────────────────────────

            migrationBuilder.CreateIndex(
                name: "ik_oi_waste_signals_service",
                table: "oi_waste_signals",
                column: "service_name");

            migrationBuilder.CreateIndex(
                name: "ik_oi_waste_signals_team_ack",
                table: "oi_waste_signals",
                columns: new[] { "team_name", "is_acknowledged" });

            migrationBuilder.CreateIndex(
                name: "ix_oi_carbon_score_records_tenant_date",
                table: "oi_carbon_score_records",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "ix_oi_carbon_score_records_tenant_service_date",
                table: "oi_carbon_score_records",
                columns: new[] { "TenantId", "ServiceId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_attributions_ApiAssetId_Environment_PeriodStart_Pe~",
                table: "ops_cost_attributions",
                columns: new[] { "ApiAssetId", "Environment", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_attributions_ServiceName",
                table: "ops_cost_attributions",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_budget_forecasts_ComputedAt",
                table: "ops_cost_budget_forecasts",
                column: "ComputedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_budget_forecasts_ServiceId_Environment",
                table: "ops_cost_budget_forecasts",
                columns: new[] { "ServiceId", "Environment" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_efficiency_recommendations_IsAcknowledged",
                table: "ops_cost_efficiency_recommendations",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_efficiency_recommendations_ServiceId",
                table: "ops_cost_efficiency_recommendations",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_import_batches_Source_Period",
                table: "ops_cost_import_batches",
                columns: new[] { "Source", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_import_batches_Status",
                table: "ops_cost_import_batches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_BatchId",
                table: "ops_cost_records",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_Domain",
                table: "ops_cost_records",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_Period",
                table: "ops_cost_records",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_ReleaseId",
                table: "ops_cost_records",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_ServiceId_Period",
                table: "ops_cost_records",
                columns: new[] { "ServiceId", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_Team",
                table: "ops_cost_records",
                column: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_snapshots_Period",
                table: "ops_cost_snapshots",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_snapshots_ServiceName_Environment_CapturedAt",
                table: "ops_cost_snapshots",
                columns: new[] { "ServiceName", "Environment", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_trends_ServiceName_Environment_PeriodStart_PeriodE~",
                table: "ops_cost_trends",
                columns: new[] { "ServiceName", "Environment", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_trends_TrendDirection",
                table: "ops_cost_trends",
                column: "TrendDirection");

            migrationBuilder.CreateIndex(
                name: "IX_ops_service_cost_profiles_ServiceName_Environment",
                table: "ops_service_cost_profiles",
                columns: new[] { "ServiceName", "Environment" },
                unique: true);

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
                name: "IX_ops_ts_anomaly_snapshots_ServiceId_Environment_DetectedAt",
                table: "ops_ts_anomaly_snapshots",
                columns: new[] { "ServiceId", "Environment", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_anomaly_snapshots_Severity_ResolvedAt",
                table: "ops_ts_anomaly_snapshots",
                columns: new[] { "Severity", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_dependency_metrics_SourceServiceId_TargetServiceId_E~",
                table: "ops_ts_dependency_metrics",
                columns: new[] { "SourceServiceId", "TargetServiceId", "Environment", "AggregationLevel", "IntervalStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_investigation_contexts_PrimaryServiceId_Environment_~",
                table: "ops_ts_investigation_contexts",
                columns: new[] { "PrimaryServiceId", "Environment", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_investigation_contexts_Status_CreatedAt",
                table: "ops_ts_investigation_contexts",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_observed_topology_Environment_IsShadowDependency",
                table: "ops_ts_observed_topology",
                columns: new[] { "Environment", "IsShadowDependency" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_observed_topology_SourceServiceId_TargetServiceId_En~",
                table: "ops_ts_observed_topology",
                columns: new[] { "SourceServiceId", "TargetServiceId", "Environment", "CommunicationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_release_correlations_ReleaseId",
                table: "ops_ts_release_correlations",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_release_correlations_ServiceId_Environment_DeployedAt",
                table: "ops_ts_release_correlations",
                columns: new[] { "ServiceId", "Environment", "DeployedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_service_metrics_ServiceId_Environment_AggregationLev~",
                table: "ops_ts_service_metrics",
                columns: new[] { "ServiceId", "Environment", "AggregationLevel", "IntervalStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_service_metrics_ServiceName_Environment_IntervalStart",
                table: "ops_ts_service_metrics",
                columns: new[] { "ServiceName", "Environment", "IntervalStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_telemetry_references_CorrelationId",
                table: "ops_ts_telemetry_references",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_telemetry_references_ServiceId_SignalType_OriginalTi~",
                table: "ops_ts_telemetry_references",
                columns: new[] { "ServiceId", "SignalType", "OriginalTimestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "oi_waste_signals");
            migrationBuilder.DropTable(name: "oi_carbon_score_records");
            migrationBuilder.DropTable(name: "ops_cost_attributions");
            migrationBuilder.DropTable(name: "ops_cost_budget_forecasts");
            migrationBuilder.DropTable(name: "ops_cost_efficiency_recommendations");
            migrationBuilder.DropTable(name: "ops_cost_import_batches");
            migrationBuilder.DropTable(name: "ops_cost_records");
            migrationBuilder.DropTable(name: "ops_cost_snapshots");
            migrationBuilder.DropTable(name: "ops_cost_trends");
            migrationBuilder.DropTable(name: "ops_service_cost_allocations");
            migrationBuilder.DropTable(name: "ops_service_cost_profiles");
            migrationBuilder.DropTable(name: "ops_ts_anomaly_snapshots");
            migrationBuilder.DropTable(name: "ops_ts_dependency_metrics");
            migrationBuilder.DropTable(name: "ops_ts_investigation_contexts");
            migrationBuilder.DropTable(name: "ops_ts_observed_topology");
            migrationBuilder.DropTable(name: "ops_ts_release_correlations");
            migrationBuilder.DropTable(name: "ops_ts_service_metrics");
            migrationBuilder.DropTable(name: "ops_ts_telemetry_references");
        }
    }
}
