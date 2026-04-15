using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// E-A04: Cria a tabela aik_execution_plans para persistência de planos de execução de IA
/// e adiciona a coluna ExecutionId (FK para aik_agent_executions).
/// Permite rastrear o plano associado a cada execução de agent.
/// </summary>
public partial class AddExecutionIdToAiExecutionPlan : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "aik_execution_plans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                InputQuery = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                Persona = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                UseCaseType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SelectedModel = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                SelectedProvider = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                RoutingPath = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SelectedSources = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                SourceWeightingSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                PolicyDecision = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                EstimatedCostClass = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                RationaleSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                ConfidenceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                EscalationReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PlannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "text", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_aik_execution_plans", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_aik_execution_plans_CorrelationId",
            table: "aik_execution_plans",
            column: "CorrelationId");

        migrationBuilder.CreateIndex(
            name: "IX_aik_execution_plans_ExecutionId",
            table: "aik_execution_plans",
            column: "ExecutionId");

        migrationBuilder.CreateIndex(
            name: "IX_aik_execution_plans_PlannedAt",
            table: "aik_execution_plans",
            column: "PlannedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "aik_execution_plans");
    }
}
