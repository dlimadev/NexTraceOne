using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// CC-05: AI Evaluation Harness — model comparison datasets and runs.
/// Tabelas: aik_eval_datasets, aik_eval_runs.
/// Distintas das tabelas existentes (aik_evaluation_*) que pertencem ao ADR-009 suite-based harness.
/// </summary>
public partial class CC05_AddAiEvalHarness : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "aik_eval_datasets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                UseCase = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                TestCasesJson = table.Column<string>(type: "jsonb", nullable: false),
                TestCaseCount = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_aik_eval_datasets", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_aik_eval_datasets_tenant_usecase",
            table: "aik_eval_datasets",
            columns: new[] { "tenant_id", "UseCase" });

        migrationBuilder.CreateTable(
            name: "aik_eval_runs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                DatasetId = table.Column<Guid>(type: "uuid", nullable: false),
                ModelId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CasesProcessed = table.Column<int>(type: "integer", nullable: false),
                ExactMatchCount = table.Column<int>(type: "integer", nullable: false),
                AverageSemanticSimilarity = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                ToolCallAccuracy = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                LatencyP50Ms = table.Column<double>(type: "double precision", nullable: false),
                LatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                TotalTokenCost = table.Column<long>(type: "bigint", nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_aik_eval_runs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_aik_eval_runs_tenant_dataset",
            table: "aik_eval_runs",
            columns: new[] { "tenant_id", "DatasetId" });

        migrationBuilder.CreateIndex(
            name: "ix_aik_eval_runs_tenant_model",
            table: "aik_eval_runs",
            columns: new[] { "tenant_id", "ModelId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "aik_eval_runs");
        migrationBuilder.DropTable(name: "aik_eval_datasets");
    }
}
