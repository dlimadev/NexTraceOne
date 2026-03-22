using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3ComplianceDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aud_compliance_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EvaluationCriteria = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_compliance_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aud_campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CampaignType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledStartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aud_compliance_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    EvaluatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_compliance_results", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_policies_Category",
                table: "aud_compliance_policies",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_policies_IsActive",
                table: "aud_compliance_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_policies_Severity",
                table: "aud_compliance_policies",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_policies_TenantId",
                table: "aud_compliance_policies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_campaigns_CampaignType",
                table: "aud_campaigns",
                column: "CampaignType");

            migrationBuilder.CreateIndex(
                name: "IX_aud_campaigns_Status",
                table: "aud_campaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aud_campaigns_TenantId",
                table: "aud_campaigns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_results_CampaignId",
                table: "aud_compliance_results",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_results_EvaluatedAt",
                table: "aud_compliance_results",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_results_Outcome",
                table: "aud_compliance_results",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_results_PolicyId",
                table: "aud_compliance_results",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_results_TenantId",
                table: "aud_compliance_results",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aud_compliance_results");

            migrationBuilder.DropTable(
                name: "aud_campaigns");

            migrationBuilder.DropTable(
                name: "aud_compliance_policies");
        }
    }
}
