using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oi_automation_workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IncidentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChangeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetScope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TargetEnvironment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    ApprovalStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    ApprovedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RiskLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Low"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_automation_workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_automation_validations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Success"),
                    ValidatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ObservedOutcome = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_automation_validations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_oi_automation_validations_oi_automation_workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "oi_automation_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "oi_automation_audit_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "WorkflowCreated"),
                    Actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_automation_audit_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_oi_automation_audit_records_oi_automation_workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "oi_automation_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indices for oi_automation_workflows
            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_workflows_ActionId",
                table: "oi_automation_workflows",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_workflows_ServiceId",
                table: "oi_automation_workflows",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_workflows_IncidentId",
                table: "oi_automation_workflows",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_workflows_Status",
                table: "oi_automation_workflows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_workflows_RequestedBy",
                table: "oi_automation_workflows",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_workflows_CreatedAt",
                table: "oi_automation_workflows",
                column: "CreatedAt");

            // Indices for oi_automation_validations
            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_validations_WorkflowId",
                table: "oi_automation_validations",
                column: "WorkflowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_validations_Outcome",
                table: "oi_automation_validations",
                column: "Outcome");

            // Indices for oi_automation_audit_records
            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_audit_records_WorkflowId",
                table: "oi_automation_audit_records",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_audit_records_ServiceId",
                table: "oi_automation_audit_records",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_audit_records_TeamId",
                table: "oi_automation_audit_records",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_audit_records_OccurredAt",
                table: "oi_automation_audit_records",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_oi_automation_audit_records_Action",
                table: "oi_automation_audit_records",
                column: "Action");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "oi_automation_audit_records");
            migrationBuilder.DropTable(name: "oi_automation_validations");
            migrationBuilder.DropTable(name: "oi_automation_workflows");
        }
    }
}
