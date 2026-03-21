using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
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
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wf_approval_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecidedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_approval_decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wf_evidence_packs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractDiffSummary = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    BlastRadiusScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    SpectralScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    ChangeIntelligenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    ApprovalHistory = table.Column<string>(type: "jsonb", nullable: true),
                    ContractHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CompletenessPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_evidence_packs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wf_sla_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaxDurationHours = table.Column<int>(type: "integer", nullable: false),
                    EscalationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EscalationTargetRole = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_sla_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wf_workflow_instances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentStageIndex = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_workflow_instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wf_workflow_stages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StageOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequiredApprovers = table.Column<int>(type: "integer", nullable: false),
                    CurrentApprovals = table.Column<int>(type: "integer", nullable: false),
                    CommentRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SlaDurationHours = table.Column<int>(type: "integer", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_workflow_stages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wf_workflow_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ChangeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiCriticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetEnvironment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MinimumApprovers = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_workflow_templates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_CreatedAt",
                table: "outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_IdempotencyKey",
                table: "outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                table: "outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_wf_approval_decisions_WorkflowInstanceId",
                table: "wf_approval_decisions",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_wf_approval_decisions_WorkflowStageId",
                table: "wf_approval_decisions",
                column: "WorkflowStageId");

            migrationBuilder.CreateIndex(
                name: "IX_wf_evidence_packs_ReleaseId",
                table: "wf_evidence_packs",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_wf_evidence_packs_WorkflowInstanceId",
                table: "wf_evidence_packs",
                column: "WorkflowInstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wf_sla_policies_WorkflowTemplateId",
                table: "wf_sla_policies",
                column: "WorkflowTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_wf_sla_policies_WorkflowTemplateId_StageName",
                table: "wf_sla_policies",
                columns: new[] { "WorkflowTemplateId", "StageName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wf_workflow_instances_ReleaseId",
                table: "wf_workflow_instances",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_wf_workflow_instances_Status",
                table: "wf_workflow_instances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_wf_workflow_instances_WorkflowTemplateId",
                table: "wf_workflow_instances",
                column: "WorkflowTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_wf_workflow_stages_WorkflowInstanceId",
                table: "wf_workflow_stages",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_wf_workflow_stages_WorkflowInstanceId_StageOrder",
                table: "wf_workflow_stages",
                columns: new[] { "WorkflowInstanceId", "StageOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wf_workflow_templates_ChangeType",
                table: "wf_workflow_templates",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "IX_wf_workflow_templates_IsActive",
                table: "wf_workflow_templates",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "wf_approval_decisions");

            migrationBuilder.DropTable(
                name: "wf_evidence_packs");

            migrationBuilder.DropTable(
                name: "wf_sla_policies");

            migrationBuilder.DropTable(
                name: "wf_workflow_instances");

            migrationBuilder.DropTable(
                name: "wf_workflow_stages");

            migrationBuilder.DropTable(
                name: "wf_workflow_templates");
        }
    }
}
