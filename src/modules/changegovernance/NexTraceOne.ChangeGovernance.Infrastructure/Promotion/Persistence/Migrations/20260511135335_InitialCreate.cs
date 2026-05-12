using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chg_deployment_environments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresEvidencePack = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_deployment_environments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_gate_evaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionGateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    EvaluatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EvaluationDetails = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OverrideJustification = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_gate_evaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_prm_outbox_messages",
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
                    table.PrimaryKey("PK_chg_prm_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_promotion_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetEnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Justification = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_promotion_requests", x => x.Id);
                    table.CheckConstraint("CK_chg_promotion_requests_status", "\"Status\" IN ('Pending', 'InEvaluation', 'Approved', 'Rejected', 'Blocked', 'Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "prm_promotion_gates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeploymentEnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    GateName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GateType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prm_promotion_gates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chg_deployment_environments_IsActive",
                table: "chg_deployment_environments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_chg_deployment_environments_Name",
                table: "chg_deployment_environments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chg_deployment_environments_Order",
                table: "chg_deployment_environments",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_chg_gate_evaluations_PromotionGateId",
                table: "chg_gate_evaluations",
                column: "PromotionGateId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_gate_evaluations_PromotionRequestId",
                table: "chg_gate_evaluations",
                column: "PromotionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_gate_evaluations_PromotionRequestId_PromotionGateId",
                table: "chg_gate_evaluations",
                columns: new[] { "PromotionRequestId", "PromotionGateId" });

            migrationBuilder.CreateIndex(
                name: "IX_chg_prm_outbox_messages_CreatedAt",
                table: "chg_prm_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_prm_outbox_messages_IdempotencyKey",
                table: "chg_prm_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chg_prm_outbox_messages_ProcessedAt",
                table: "chg_prm_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_requests_ReleaseId",
                table: "chg_promotion_requests",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_requests_RequestedAt",
                table: "chg_promotion_requests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_requests_Status",
                table: "chg_promotion_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_requests_TargetEnvironmentId",
                table: "chg_promotion_requests",
                column: "TargetEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_prm_promotion_gates_DeploymentEnvironmentId",
                table: "prm_promotion_gates",
                column: "DeploymentEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_prm_promotion_gates_DeploymentEnvironmentId_GateName",
                table: "prm_promotion_gates",
                columns: new[] { "DeploymentEnvironmentId", "GateName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prm_promotion_gates_IsActive",
                table: "prm_promotion_gates",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chg_deployment_environments");

            migrationBuilder.DropTable(
                name: "chg_gate_evaluations");

            migrationBuilder.DropTable(
                name: "chg_prm_outbox_messages");

            migrationBuilder.DropTable(
                name: "chg_promotion_requests");

            migrationBuilder.DropTable(
                name: "prm_promotion_gates");
        }
    }
}
