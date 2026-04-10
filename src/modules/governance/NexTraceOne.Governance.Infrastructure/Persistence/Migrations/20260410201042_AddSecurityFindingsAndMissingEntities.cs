using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityFindingsAndMissingEntities : Migration
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
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_change_cost_impacts", x => x.Id);
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
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_cost_attributions", x => x.Id);
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
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_executive_briefings", x => x.Id);
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
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_license_compliance_reports", x => x.Id);
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
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastReassessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReassessmentCount = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_service_maturity_assessments", x => x.Id);
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
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_team_health_snapshots", x => x.Id);
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gov_change_cost_impacts");

            migrationBuilder.DropTable(
                name: "gov_cost_attributions");

            migrationBuilder.DropTable(
                name: "gov_executive_briefings");

            migrationBuilder.DropTable(
                name: "gov_license_compliance_reports");

            migrationBuilder.DropTable(
                name: "gov_service_maturity_assessments");

            migrationBuilder.DropTable(
                name: "gov_team_health_snapshots");
        }
    }
}
