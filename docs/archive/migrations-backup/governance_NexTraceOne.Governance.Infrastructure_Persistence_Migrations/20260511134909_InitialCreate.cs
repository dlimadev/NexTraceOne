using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gov_change_cost_impacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BaselineCostPerDay = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ActualCostPerDay = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CostDelta = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CostDeltaPercentage = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CostProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CostDetails = table.Column<string>(type: "jsonb", nullable: true),
                    MeasurementWindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MeasurementWindowEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_change_cost_impacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_compliance_gaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Team = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ViolatedPolicyIds = table.Column<string>(type: "jsonb", nullable: false),
                    ViolationCount = table.Column<int>(type: "integer", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_compliance_gaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_cost_attributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DimensionKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DimensionLabel = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ComputeCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    StorageCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NetworkCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    OtherCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CostBreakdown = table.Column<string>(type: "jsonb", nullable: true),
                    AttributionMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DataSources = table.Column<string>(type: "jsonb", nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_cost_attributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_custom_dashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Layout = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Widgets = table.Column<string>(type: "jsonb", nullable: false),
                    SharingPolicyJson = table.Column<string>(type: "jsonb", nullable: false),
                    VariablesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CurrentRevisionNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LifecycleStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    DeprecatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeprecationNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SuccessorDashboardId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TeamId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_custom_dashboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_dashboard_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    WidgetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AuthorUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MentionsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_dashboard_comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_dashboard_monitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    WidgetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NqlQuery = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ConditionField = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConditionOperator = table.Column<int>(type: "integer", nullable: false),
                    ConditionThreshold = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    EvaluationWindowMinutes = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    NotificationChannelsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastFiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FiredCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_dashboard_monitors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_dashboard_revisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Layout = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WidgetsJson = table.Column<string>(type: "jsonb", nullable: false),
                    VariablesJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    AuthorUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChangeNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_dashboard_revisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_dashboard_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TagsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    DashboardSnapshotJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    RequiredVariablesJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    InstallCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "1.0.0"),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_dashboard_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_dashboard_usage_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "view"),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_dashboard_usage_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_delegated_administrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GranteeUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GranteeDisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DomainId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_delegated_administrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_demo_seed_state",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SeededAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EntitiesCount = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_demo_seed_state", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_domains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CapabilityClassification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_domains", x => x.Id);
                    table.CheckConstraint("CK_gov_domains_criticality", "\"Criticality\" IN ('Low', 'Medium', 'High', 'Critical')");
                });

            migrationBuilder.CreateTable(
                name: "gov_evidence_packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SealedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_evidence_packages", x => x.Id);
                    table.CheckConstraint("CK_gov_evidence_packages_status", "\"Status\" IN ('Draft', 'Sealed', 'Exported')");
                });

            migrationBuilder.CreateTable(
                name: "gov_executive_briefings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "text", nullable: true),
                    PlatformStatusSection = table.Column<string>(type: "jsonb", nullable: true),
                    TopIncidentsSection = table.Column<string>(type: "jsonb", nullable: true),
                    TeamPerformanceSection = table.Column<string>(type: "jsonb", nullable: true),
                    HighRiskChangesSection = table.Column<string>(type: "jsonb", nullable: true),
                    ComplianceStatusSection = table.Column<string>(type: "jsonb", nullable: true),
                    CostTrendsSection = table.Column<string>(type: "jsonb", nullable: true),
                    ActiveRisksSection = table.Column<string>(type: "jsonb", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedByAgent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_executive_briefings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_finops_budget_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActualCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BaselineCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CostDeltaPct = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Justification = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ResolvedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_finops_budget_approvals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_greenops_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntensityFactorKgPerKwh = table.Column<double>(type: "double precision", nullable: false),
                    EsgTargetKgCo2PerMonth = table.Column<double>(type: "double precision", nullable: false),
                    DatacenterRegion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_greenops_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_license_compliance_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScopeLabel = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TotalDependencies = table.Column<int>(type: "integer", nullable: false),
                    CompliantCount = table.Column<int>(type: "integer", nullable: false),
                    NonCompliantCount = table.Column<int>(type: "integer", nullable: false),
                    WarningCount = table.Column<int>(type: "integer", nullable: false),
                    OverallRiskLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompliancePercent = table.Column<int>(type: "integer", nullable: false),
                    LicenseDetails = table.Column<string>(type: "jsonb", nullable: true),
                    Conflicts = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    ScannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_license_compliance_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_nonprod_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EnvironmentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveDaysOfWeekJson = table.Column<string>(type: "jsonb", nullable: false),
                    ActiveFromHour = table.Column<int>(type: "integer", nullable: false),
                    ActiveToHour = table.Column<int>(type: "integer", nullable: false),
                    Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EstimatedSavingPct = table.Column<int>(type: "integer", nullable: false),
                    KeepActiveUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OverrideReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_nonprod_schedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_notebooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CellsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SharingPolicyJson = table.Column<string>(type: "jsonb", nullable: false),
                    CurrentRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LinkedDashboardId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_notebooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_outbox_messages",
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
                    table.PrimaryKey("PK_gov_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_pack_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rules = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultEnforcementMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_pack_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_packs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_packs", x => x.Id);
                    table.CheckConstraint("CK_gov_packs_status", "\"Status\" IN ('Draft', 'Published', 'Deprecated', 'Archived')");
                });

            migrationBuilder.CreateTable(
                name: "gov_persona_home_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CardLayoutJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    QuickActionsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    DefaultScopeJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_persona_home_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_policy_as_code",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DefinitionContent = table.Column<string>(type: "text", nullable: false),
                    EnforcementMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SimulatedAffectedServices = table.Column<int>(type: "integer", nullable: true),
                    SimulatedNonCompliantServices = table.Column<int>(type: "integer", nullable: true),
                    LastSimulatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RegisteredBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_policy_as_code", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_presence_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AvatarColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_presence_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_recovery_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestorePointId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SchemasJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InitiatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InitiatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_recovery_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_rollout_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnforcementMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InitiatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InitiatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_rollout_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_saml_sso_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SsoUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SloUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IdpCertificate = table.Column<string>(type: "text", nullable: false),
                    JitProvisioningEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AttributeMappingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_saml_sso_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_scheduled_dashboard_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CronExpression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "pdf"),
                    RecipientsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    WebhookUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 90),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    LastFailureMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_scheduled_dashboard_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_security_scan_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScanProvider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OverallRisk = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PassedGate = table.Column<bool>(type: "boolean", nullable: false),
                    Summary_TotalFindings = table.Column<int>(type: "integer", nullable: false),
                    Summary_CriticalCount = table.Column<int>(type: "integer", nullable: false),
                    Summary_HighCount = table.Column<int>(type: "integer", nullable: false),
                    Summary_MediumCount = table.Column<int>(type: "integer", nullable: false),
                    Summary_LowCount = table.Column<int>(type: "integer", nullable: false),
                    Summary_InfoCount = table.Column<int>(type: "integer", nullable: false),
                    Summary_TopCategories = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_security_scan_results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_service_maturity_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnershipDefined = table.Column<bool>(type: "boolean", nullable: false),
                    ContractsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    DocumentationExists = table.Column<bool>(type: "boolean", nullable: false),
                    PoliciesApplied = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalWorkflowActive = table.Column<bool>(type: "boolean", nullable: false),
                    TelemetryActive = table.Column<bool>(type: "boolean", nullable: false),
                    BaselinesEstablished = table.Column<bool>(type: "boolean", nullable: false),
                    AlertsConfigured = table.Column<bool>(type: "boolean", nullable: false),
                    RunbooksAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    RollbackTested = table.Column<bool>(type: "boolean", nullable: false),
                    ChaosValidated = table.Column<bool>(type: "boolean", nullable: false),
                    AssessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssessedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastReassessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReassessmentCount = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_service_maturity_assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_setup_wizard_steps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StepId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_setup_wizard_steps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_support_bundles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SizeMb = table.Column<double>(type: "double precision", nullable: true),
                    ZipContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    IncludesLogs = table.Column<bool>(type: "boolean", nullable: false),
                    IncludesConfig = table.Column<bool>(type: "boolean", nullable: false),
                    IncludesDb = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_support_bundles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_team_domain_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnershipType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_team_domain_links", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_team_health_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    ServiceCountScore = table.Column<int>(type: "integer", nullable: false),
                    ContractHealthScore = table.Column<int>(type: "integer", nullable: false),
                    IncidentFrequencyScore = table.Column<int>(type: "integer", nullable: false),
                    MttrScore = table.Column<int>(type: "integer", nullable: false),
                    TechDebtScore = table.Column<int>(type: "integer", nullable: false),
                    DocCoverageScore = table.Column<int>(type: "integer", nullable: false),
                    PolicyComplianceScore = table.Column<int>(type: "integer", nullable: false),
                    DimensionDetails = table.Column<string>(type: "jsonb", nullable: true),
                    AssessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_team_health_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ParentOrganizationUnit = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_teams", x => x.Id);
                    table.CheckConstraint("CK_gov_teams_status", "\"Status\" IN ('Active', 'Inactive', 'Archived')");
                });

            migrationBuilder.CreateTable(
                name: "gov_technical_debt_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DebtType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EstimatedEffortDays = table.Column<int>(type: "integer", nullable: false),
                    DebtScore = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_technical_debt_items", x => x.Id);
                    table.CheckConstraint("CK_gov_technical_debt_items_debt_type", "\"DebtType\" IN ('architecture', 'code-quality', 'security', 'dependency', 'documentation', 'testing', 'performance', 'infrastructure')");
                    table.CheckConstraint("CK_gov_technical_debt_items_severity", "\"Severity\" IN ('critical', 'high', 'medium', 'low')");
                });

            migrationBuilder.CreateTable(
                name: "gov_widget_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    WidgetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DataJson = table.Column<string>(type: "jsonb", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_widget_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtelMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricName = table.Column<string>(type: "text", nullable: false),
                    MetricType = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    ServiceVersion = table.Column<string>(type: "text", nullable: true),
                    Environment = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResourceAttributesJson = table.Column<string>(type: "text", nullable: false),
                    MetricAttributesJson = table.Column<string>(type: "text", nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtelMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_evidence_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SourceModule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecordedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_evidence_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gov_evidence_items_gov_evidence_packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "gov_evidence_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gov_waivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Justification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EvidenceLinks = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_waivers", x => x.Id);
                    table.CheckConstraint("CK_gov_waivers_status", "\"Status\" IN ('Pending', 'Approved', 'Rejected', 'Revoked', 'Expired')");
                    table.ForeignKey(
                        name: "FK_gov_waivers_gov_packs_PackId",
                        column: x => x.PackId,
                        principalTable: "gov_packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gov_security_findings",
                columns: table => new
                {
                    FindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScanResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Remediation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CweId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OwaspCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_security_findings", x => x.FindingId);
                    table.ForeignKey(
                        name: "FK_gov_security_findings_gov_security_scan_results_ScanResultId",
                        column: x => x.ScanResultId,
                        principalTable: "gov_security_scan_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gov_change_cost_impacts_CostDelta",
                table: "gov_change_cost_impacts",
                column: "CostDelta");

            migrationBuilder.CreateIndex(
                name: "IX_gov_change_cost_impacts_RecordedAt",
                table: "gov_change_cost_impacts",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_change_cost_impacts_ReleaseId",
                table: "gov_change_cost_impacts",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_change_cost_impacts_ServiceName",
                table: "gov_change_cost_impacts",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_gov_change_cost_impacts_tenant_id",
                table: "gov_change_cost_impacts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_gov_compliance_gaps_DetectedAt",
                table: "gov_compliance_gaps",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_compliance_gaps_Domain",
                table: "gov_compliance_gaps",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_gov_compliance_gaps_ServiceId",
                table: "gov_compliance_gaps",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_compliance_gaps_Severity",
                table: "gov_compliance_gaps",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_gov_compliance_gaps_Team",
                table: "gov_compliance_gaps",
                column: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_gov_cost_attributions_Dimension",
                table: "gov_cost_attributions",
                column: "Dimension");

            migrationBuilder.CreateIndex(
                name: "IX_gov_cost_attributions_Dimension_PeriodStart_PeriodEnd",
                table: "gov_cost_attributions",
                columns: new[] { "Dimension", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_gov_cost_attributions_DimensionKey",
                table: "gov_cost_attributions",
                column: "DimensionKey");

            migrationBuilder.CreateIndex(
                name: "IX_gov_cost_attributions_PeriodEnd",
                table: "gov_cost_attributions",
                column: "PeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_gov_cost_attributions_PeriodStart",
                table: "gov_cost_attributions",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_gov_cost_attributions_tenant_id",
                table: "gov_cost_attributions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_gov_cost_attributions_TotalCost",
                table: "gov_cost_attributions",
                column: "TotalCost");

            migrationBuilder.CreateIndex(
                name: "IX_gov_custom_dashboards_CreatedAt",
                table: "gov_custom_dashboards",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_custom_dashboards_CreatedByUserId",
                table: "gov_custom_dashboards",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_custom_dashboards_Persona",
                table: "gov_custom_dashboards",
                column: "Persona");

            migrationBuilder.CreateIndex(
                name: "IX_gov_custom_dashboards_tenant_id",
                table: "gov_custom_dashboards",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_gov_dash_comments_created_at",
                table: "gov_dashboard_comments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_gov_dash_comments_dashboard_tenant",
                table: "gov_dashboard_comments",
                columns: new[] { "DashboardId", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_dash_comments_widget",
                table: "gov_dashboard_comments",
                columns: new[] { "DashboardId", "WidgetId" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_monitor_dashboard_tenant",
                table: "gov_dashboard_monitors",
                columns: new[] { "DashboardId", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_monitor_tenant_status",
                table: "gov_dashboard_monitors",
                columns: new[] { "tenant_id", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_dashboard_revisions_dashboard_number",
                table: "gov_dashboard_revisions",
                columns: new[] { "DashboardId", "RevisionNumber" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_dashboard_revisions_dashboard_tenant",
                table: "gov_dashboard_revisions",
                columns: new[] { "DashboardId", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_dashboard_revisions_tenant",
                table: "gov_dashboard_revisions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_gov_dash_templates_persona",
                table: "gov_dashboard_templates",
                column: "Persona");

            migrationBuilder.CreateIndex(
                name: "ix_gov_dash_templates_system_category",
                table: "gov_dashboard_templates",
                columns: new[] { "IsSystem", "Category" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_dash_templates_tenant_category",
                table: "gov_dashboard_templates",
                columns: new[] { "tenant_id", "Category" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_dash_usage_dashboard_tenant",
                table: "gov_dashboard_usage_events",
                columns: new[] { "DashboardId", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_dash_usage_occurred_at",
                table: "gov_dashboard_usage_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "ix_gov_dash_usage_tenant_type",
                table: "gov_dashboard_usage_events",
                columns: new[] { "tenant_id", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_DomainId",
                table: "gov_delegated_administrations",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_ExpiresAt",
                table: "gov_delegated_administrations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_GranteeUserId",
                table: "gov_delegated_administrations",
                column: "GranteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_IsActive",
                table: "gov_delegated_administrations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_Scope",
                table: "gov_delegated_administrations",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_TeamId",
                table: "gov_delegated_administrations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_demo_seed_state_TenantId",
                table: "gov_demo_seed_state",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_domains_CapabilityClassification",
                table: "gov_domains",
                column: "CapabilityClassification");

            migrationBuilder.CreateIndex(
                name: "IX_gov_domains_Criticality",
                table: "gov_domains",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_gov_domains_Name",
                table: "gov_domains",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_evidence_items_PackageId",
                table: "gov_evidence_items",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_evidence_items_RecordedAt",
                table: "gov_evidence_items",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_evidence_items_Type",
                table: "gov_evidence_items",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_gov_evidence_packages_Scope",
                table: "gov_evidence_packages",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_evidence_packages_Status",
                table: "gov_evidence_packages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_executive_briefings_Frequency",
                table: "gov_executive_briefings",
                column: "Frequency");

            migrationBuilder.CreateIndex(
                name: "IX_gov_executive_briefings_GeneratedAt",
                table: "gov_executive_briefings",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_executive_briefings_Status",
                table: "gov_executive_briefings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_executive_briefings_tenant_id",
                table: "gov_executive_briefings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_gov_finops_budget_approvals_ReleaseId",
                table: "gov_finops_budget_approvals",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_finops_budget_approvals_RequestedAt",
                table: "gov_finops_budget_approvals",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_finops_budget_approvals_ServiceName",
                table: "gov_finops_budget_approvals",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_gov_finops_budget_approvals_Status",
                table: "gov_finops_budget_approvals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_greenops_configurations_TenantId",
                table: "gov_greenops_configurations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_license_compliance_reports_CompliancePercent",
                table: "gov_license_compliance_reports",
                column: "CompliancePercent");

            migrationBuilder.CreateIndex(
                name: "IX_gov_license_compliance_reports_OverallRiskLevel",
                table: "gov_license_compliance_reports",
                column: "OverallRiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_gov_license_compliance_reports_ScannedAt",
                table: "gov_license_compliance_reports",
                column: "ScannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_license_compliance_reports_Scope",
                table: "gov_license_compliance_reports",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_license_compliance_reports_Scope_ScopeKey_ScannedAt",
                table: "gov_license_compliance_reports",
                columns: new[] { "Scope", "ScopeKey", "ScannedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_gov_license_compliance_reports_ScopeKey",
                table: "gov_license_compliance_reports",
                column: "ScopeKey");

            migrationBuilder.CreateIndex(
                name: "IX_gov_license_compliance_reports_tenant_id",
                table: "gov_license_compliance_reports",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_gov_nonprod_schedules_EnvironmentId_TenantId",
                table: "gov_nonprod_schedules",
                columns: new[] { "EnvironmentId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gov_notebooks_created_by",
                table: "gov_notebooks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "ix_gov_notebooks_tenant_persona",
                table: "gov_notebooks",
                columns: new[] { "tenant_id", "Persona" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_notebooks_tenant_status",
                table: "gov_notebooks",
                columns: new[] { "tenant_id", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_gov_outbox_messages_CreatedAt",
                table: "gov_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_outbox_messages_IdempotencyKey",
                table: "gov_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_outbox_messages_ProcessedAt",
                table: "gov_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_pack_versions_PackId",
                table: "gov_pack_versions",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_pack_versions_PackId_Version",
                table: "gov_pack_versions",
                columns: new[] { "PackId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_pack_versions_PublishedAt",
                table: "gov_pack_versions",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_packs_Category",
                table: "gov_packs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_gov_packs_Name",
                table: "gov_packs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_packs_Status",
                table: "gov_packs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_gov_persona_home_user_persona_tenant",
                table: "gov_persona_home_configurations",
                columns: new[] { "UserId", "Persona", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_policy_as_code_EnforcementMode",
                table: "gov_policy_as_code",
                column: "EnforcementMode");

            migrationBuilder.CreateIndex(
                name: "IX_gov_policy_as_code_Name",
                table: "gov_policy_as_code",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_policy_as_code_Status",
                table: "gov_policy_as_code",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_policy_as_code_TenantId",
                table: "gov_policy_as_code",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_gov_presence_resource_active",
                table: "gov_presence_sessions",
                columns: new[] { "tenant_id", "ResourceType", "ResourceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_presence_user_active",
                table: "gov_presence_sessions",
                columns: new[] { "tenant_id", "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_gov_recovery_jobs_InitiatedAt",
                table: "gov_recovery_jobs",
                column: "InitiatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_CompletedAt",
                table: "gov_rollout_records",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_InitiatedAt",
                table: "gov_rollout_records",
                column: "InitiatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_PackId",
                table: "gov_rollout_records",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_Scope",
                table: "gov_rollout_records",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_Status",
                table: "gov_rollout_records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_VersionId",
                table: "gov_rollout_records",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_saml_sso_configurations_TenantId",
                table: "gov_saml_sso_configurations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_gov_sched_report_dashboard_tenant",
                table: "gov_scheduled_dashboard_reports",
                columns: new[] { "DashboardId", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_sched_report_next_run",
                table: "gov_scheduled_dashboard_reports",
                column: "NextRunAt");

            migrationBuilder.CreateIndex(
                name: "ix_gov_sched_report_tenant_active",
                table: "gov_scheduled_dashboard_reports",
                columns: new[] { "tenant_id", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_gov_security_findings_ScanResultId",
                table: "gov_security_findings",
                column: "ScanResultId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_security_findings_Severity",
                table: "gov_security_findings",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_gov_service_maturity_assessments_AssessedAt",
                table: "gov_service_maturity_assessments",
                column: "AssessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_service_maturity_assessments_CurrentLevel",
                table: "gov_service_maturity_assessments",
                column: "CurrentLevel");

            migrationBuilder.CreateIndex(
                name: "IX_gov_service_maturity_assessments_ServiceId",
                table: "gov_service_maturity_assessments",
                column: "ServiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_service_maturity_assessments_tenant_id",
                table: "gov_service_maturity_assessments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_gov_setup_wizard_tenant",
                table: "gov_setup_wizard_steps",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_gov_setup_wizard_tenant_step",
                table: "gov_setup_wizard_steps",
                columns: new[] { "TenantId", "StepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_support_bundles_RequestedAt",
                table: "gov_support_bundles",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_support_bundles_Status",
                table: "gov_support_bundles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_support_bundles_TenantId",
                table: "gov_support_bundles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_DomainId",
                table: "gov_team_domain_links",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_OwnershipType",
                table: "gov_team_domain_links",
                column: "OwnershipType");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_TeamId",
                table: "gov_team_domain_links",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_TeamId_DomainId",
                table: "gov_team_domain_links",
                columns: new[] { "TeamId", "DomainId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_health_snapshots_AssessedAt",
                table: "gov_team_health_snapshots",
                column: "AssessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_health_snapshots_OverallScore",
                table: "gov_team_health_snapshots",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_health_snapshots_TeamId",
                table: "gov_team_health_snapshots",
                column: "TeamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_health_snapshots_tenant_id",
                table: "gov_team_health_snapshots",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_gov_teams_Name",
                table: "gov_teams",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_teams_ParentOrganizationUnit",
                table: "gov_teams",
                column: "ParentOrganizationUnit");

            migrationBuilder.CreateIndex(
                name: "IX_gov_teams_Status",
                table: "gov_teams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_technical_debt_items_CreatedAt",
                table: "gov_technical_debt_items",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_technical_debt_items_DebtType",
                table: "gov_technical_debt_items",
                column: "DebtType");

            migrationBuilder.CreateIndex(
                name: "IX_gov_technical_debt_items_ServiceName",
                table: "gov_technical_debt_items",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_gov_technical_debt_items_Severity",
                table: "gov_technical_debt_items",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_gov_technical_debt_items_tenant_id",
                table: "gov_technical_debt_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_ExpiresAt",
                table: "gov_waivers",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_PackId",
                table: "gov_waivers",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_RequestedBy",
                table: "gov_waivers",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_Scope",
                table: "gov_waivers",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_Status",
                table: "gov_waivers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_gov_widget_snapshot_lookup",
                table: "gov_widget_snapshots",
                columns: new[] { "TenantId", "DashboardId", "WidgetId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_gov_widget_snapshot_widget",
                table: "gov_widget_snapshots",
                columns: new[] { "TenantId", "DashboardId", "WidgetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gov_change_cost_impacts");

            migrationBuilder.DropTable(
                name: "gov_compliance_gaps");

            migrationBuilder.DropTable(
                name: "gov_cost_attributions");

            migrationBuilder.DropTable(
                name: "gov_custom_dashboards");

            migrationBuilder.DropTable(
                name: "gov_dashboard_comments");

            migrationBuilder.DropTable(
                name: "gov_dashboard_monitors");

            migrationBuilder.DropTable(
                name: "gov_dashboard_revisions");

            migrationBuilder.DropTable(
                name: "gov_dashboard_templates");

            migrationBuilder.DropTable(
                name: "gov_dashboard_usage_events");

            migrationBuilder.DropTable(
                name: "gov_delegated_administrations");

            migrationBuilder.DropTable(
                name: "gov_demo_seed_state");

            migrationBuilder.DropTable(
                name: "gov_domains");

            migrationBuilder.DropTable(
                name: "gov_evidence_items");

            migrationBuilder.DropTable(
                name: "gov_executive_briefings");

            migrationBuilder.DropTable(
                name: "gov_finops_budget_approvals");

            migrationBuilder.DropTable(
                name: "gov_greenops_configurations");

            migrationBuilder.DropTable(
                name: "gov_license_compliance_reports");

            migrationBuilder.DropTable(
                name: "gov_nonprod_schedules");

            migrationBuilder.DropTable(
                name: "gov_notebooks");

            migrationBuilder.DropTable(
                name: "gov_outbox_messages");

            migrationBuilder.DropTable(
                name: "gov_pack_versions");

            migrationBuilder.DropTable(
                name: "gov_persona_home_configurations");

            migrationBuilder.DropTable(
                name: "gov_policy_as_code");

            migrationBuilder.DropTable(
                name: "gov_presence_sessions");

            migrationBuilder.DropTable(
                name: "gov_recovery_jobs");

            migrationBuilder.DropTable(
                name: "gov_rollout_records");

            migrationBuilder.DropTable(
                name: "gov_saml_sso_configurations");

            migrationBuilder.DropTable(
                name: "gov_scheduled_dashboard_reports");

            migrationBuilder.DropTable(
                name: "gov_security_findings");

            migrationBuilder.DropTable(
                name: "gov_service_maturity_assessments");

            migrationBuilder.DropTable(
                name: "gov_setup_wizard_steps");

            migrationBuilder.DropTable(
                name: "gov_support_bundles");

            migrationBuilder.DropTable(
                name: "gov_team_domain_links");

            migrationBuilder.DropTable(
                name: "gov_team_health_snapshots");

            migrationBuilder.DropTable(
                name: "gov_teams");

            migrationBuilder.DropTable(
                name: "gov_technical_debt_items");

            migrationBuilder.DropTable(
                name: "gov_waivers");

            migrationBuilder.DropTable(
                name: "gov_widget_snapshots");

            migrationBuilder.DropTable(
                name: "OtelMetrics");

            migrationBuilder.DropTable(
                name: "gov_evidence_packages");

            migrationBuilder.DropTable(
                name: "gov_security_scan_results");

            migrationBuilder.DropTable(
                name: "gov_packs");
        }
    }
}
