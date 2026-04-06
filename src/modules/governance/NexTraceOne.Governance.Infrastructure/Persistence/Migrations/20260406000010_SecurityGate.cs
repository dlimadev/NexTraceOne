using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SecurityGate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_security_scan_results", x => x.Id);
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
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                LineNumber = table.Column<int>(type: "integer", nullable: true),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Remediation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                CweId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                OwaspCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false)
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
            name: "IX_gov_security_findings_ScanResultId",
            table: "gov_security_findings",
            column: "ScanResultId");

        migrationBuilder.CreateIndex(
            name: "IX_gov_security_findings_Severity",
            table: "gov_security_findings",
            column: "Severity");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_security_findings");
        migrationBuilder.DropTable(name: "gov_security_scan_results");
    }
}
