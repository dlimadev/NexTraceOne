using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractCompliancePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cfg_contract_compliance_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerificationApproach = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnBreakingChange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnNonBreakingChange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnNewEndpoint = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnRemovedEndpoint = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnMissingContract = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnContractNotApproved = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AutoGenerateChangelog = table.Column<bool>(type: "boolean", nullable: false),
                    ChangelogFormat = table.Column<int>(type: "integer", nullable: false),
                    RequireChangelogApproval = table.Column<bool>(type: "boolean", nullable: false),
                    EnforceCdct = table.Column<bool>(type: "boolean", nullable: false),
                    CdctFailureAction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnableRuntimeDriftDetection = table.Column<bool>(type: "boolean", nullable: false),
                    DriftDetectionIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    DriftThresholdForAlert = table.Column<decimal>(type: "numeric", nullable: false),
                    DriftThresholdForIncident = table.Column<decimal>(type: "numeric", nullable: false),
                    NotifyOnVerificationFailure = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnBreakingChange = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnDriftDetected = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationChannels = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_contract_compliance_policies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_contract_compliance_policies_IsActive",
                table: "cfg_contract_compliance_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_contract_compliance_policies_Scope",
                table: "cfg_contract_compliance_policies",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_contract_compliance_policies_TenantId",
                table: "cfg_contract_compliance_policies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_contract_compliance_policies_TenantId_Scope_IsActive",
                table: "cfg_contract_compliance_policies",
                columns: new[] { "TenantId", "Scope", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cfg_contract_compliance_policies");
        }
    }
}
