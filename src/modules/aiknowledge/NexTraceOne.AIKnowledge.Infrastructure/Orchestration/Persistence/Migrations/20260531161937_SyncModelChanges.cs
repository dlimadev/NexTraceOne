using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aik_orch_workflow_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InitialInput = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    FinalOutput = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    StepResultsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TotalSteps = table.Column<int>(type: "integer", nullable: false),
                    SuccessfulSteps = table.Column<int>(type: "integer", nullable: false),
                    TotalRetries = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CallerTeamId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_orch_workflow_executions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aik_orch_workflow_executions_CallerTeamId",
                table: "aik_orch_workflow_executions",
                column: "CallerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_orch_workflow_executions_CorrelationId",
                table: "aik_orch_workflow_executions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_orch_workflow_executions_StartedAt",
                table: "aik_orch_workflow_executions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_orch_workflow_executions_Status",
                table: "aik_orch_workflow_executions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aik_orch_workflow_executions_WorkflowName",
                table: "aik_orch_workflow_executions",
                column: "WorkflowName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aik_orch_workflow_executions");
        }
    }
}
