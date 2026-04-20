using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OI_AddRunbookStepExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ops_inc_runbook_step_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunbookId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExecutorUserId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExecutionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OutputSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ErrorDetail = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_inc_runbook_step_executions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ops_inc_runbook_step_executions_RunbookId",
                table: "ops_inc_runbook_step_executions",
                column: "RunbookId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_inc_runbook_step_executions_TenantId",
                table: "ops_inc_runbook_step_executions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_inc_runbook_step_executions");
        }
    }
}
