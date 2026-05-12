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
                name: "ops_auto_outbox_messages",
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
                    table.PrimaryKey("PK_ops_auto_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_automation_workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_automation_workflows", x => x.Id);
                    table.CheckConstraint("CK_ops_automation_workflows_approval_status", "\"ApprovalStatus\" IN ('NotRequired','Pending','Approved','Rejected','Escalated')");
                    table.CheckConstraint("CK_ops_automation_workflows_risk_level", "\"RiskLevel\" IN ('Low','Medium','High','Critical')");
                    table.CheckConstraint("CK_ops_automation_workflows_status", "\"Status\" IN ('Draft','PendingPreconditions','AwaitingApproval','Approved','ReadyToExecute','Executing','AwaitingValidation','Completed','Failed','Cancelled','Rejected')");
                });

            migrationBuilder.CreateTable(
                name: "ops_automation_audit_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ops_automation_audit_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ops_automation_audit_records_ops_automation_workflows_Workf~",
                        column: x => x.WorkflowId,
                        principalTable: "ops_automation_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ops_automation_validations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Successful"),
                    ValidatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ObservedOutcome = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_automation_validations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ops_automation_validations_ops_automation_workflows_Workflo~",
                        column: x => x.WorkflowId,
                        principalTable: "ops_automation_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ops_auto_outbox_messages_CreatedAt",
                table: "ops_auto_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_auto_outbox_messages_IdempotencyKey",
                table: "ops_auto_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_auto_outbox_messages_ProcessedAt",
                table: "ops_auto_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_audit_records_Action",
                table: "ops_automation_audit_records",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_audit_records_OccurredAt",
                table: "ops_automation_audit_records",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_audit_records_ServiceId",
                table: "ops_automation_audit_records",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_audit_records_TeamId",
                table: "ops_automation_audit_records",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_audit_records_WorkflowId",
                table: "ops_automation_audit_records",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_validations_Outcome",
                table: "ops_automation_validations",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_validations_WorkflowId",
                table: "ops_automation_validations",
                column: "WorkflowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_workflows_ActionId",
                table: "ops_automation_workflows",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_workflows_CreatedAt",
                table: "ops_automation_workflows",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_workflows_IncidentId",
                table: "ops_automation_workflows",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_workflows_RequestedBy",
                table: "ops_automation_workflows",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_workflows_ServiceId",
                table: "ops_automation_workflows",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_automation_workflows_Status",
                table: "ops_automation_workflows",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_auto_outbox_messages");

            migrationBuilder.DropTable(
                name: "ops_automation_audit_records");

            migrationBuilder.DropTable(
                name: "ops_automation_validations");

            migrationBuilder.DropTable(
                name: "ops_automation_workflows");
        }
    }
}
