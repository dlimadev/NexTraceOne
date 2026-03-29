using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Phase 5: Innovation.
/// - Cria tabela aik_guardrails para proteção de input/output de IA.
/// - Cria tabela aik_evaluations para avaliação de qualidade de respostas.
/// </summary>
public partial class P05_Innovation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. Guardrails ───────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "aik_guardrails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                GuardType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Pattern = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                PatternType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                UserMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                ModelId = table.Column<Guid>(type: "uuid", nullable: true),
                Priority = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "text", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_aik_guardrails", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_aik_guardrails_Name",
            table: "aik_guardrails",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_aik_guardrails_Category",
            table: "aik_guardrails",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_aik_guardrails_GuardType",
            table: "aik_guardrails",
            column: "GuardType");

        migrationBuilder.CreateIndex(
            name: "IX_aik_guardrails_IsActive",
            table: "aik_guardrails",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_aik_guardrails_Priority",
            table: "aik_guardrails",
            column: "Priority");

        // ── 2. Evaluations ──────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "aik_evaluations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EvaluationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                AgentExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                ModelName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                PromptTemplateName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                RelevanceScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                AccuracyScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                UsefulnessScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                SafetyScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                OverallScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                Feedback = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "text", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_aik_evaluations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_aik_evaluations_ConversationId",
            table: "aik_evaluations",
            column: "ConversationId");

        migrationBuilder.CreateIndex(
            name: "IX_aik_evaluations_AgentExecutionId",
            table: "aik_evaluations",
            column: "AgentExecutionId");

        migrationBuilder.CreateIndex(
            name: "IX_aik_evaluations_UserId",
            table: "aik_evaluations",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_aik_evaluations_TenantId",
            table: "aik_evaluations",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_aik_evaluations_EvaluatedAt",
            table: "aik_evaluations",
            column: "EvaluatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_aik_evaluations_OverallScore",
            table: "aik_evaluations",
            column: "OverallScore");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "aik_evaluations");
        migrationBuilder.DropTable(name: "aik_guardrails");
    }
}
