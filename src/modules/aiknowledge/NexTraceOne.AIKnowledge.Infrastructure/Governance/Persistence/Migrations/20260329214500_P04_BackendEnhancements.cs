using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Phase 4: Backend Enhancements.
/// - Cria tabela aik_prompt_templates para templates de prompt versionados.
/// - Cria tabela aik_tool_definitions para definições persistidas de ferramentas.
/// - Adiciona campos FinOps ao ledger de tokens (atribuição de custo).
/// </summary>
public partial class P04_BackendEnhancements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. Prompt Templates ─────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "aik_prompt_templates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                Variables = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                TargetPersonas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                ScopeHint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Relevance = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                PreferredModelId = table.Column<Guid>(type: "uuid", nullable: true),
                RecommendedTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                MaxOutputTokens = table.Column<int>(type: "integer", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "text", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_aik_prompt_templates", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_aik_prompt_templates_Name",
            table: "aik_prompt_templates",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_aik_prompt_templates_Category",
            table: "aik_prompt_templates",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_aik_prompt_templates_IsActive",
            table: "aik_prompt_templates",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_aik_prompt_templates_IsOfficial",
            table: "aik_prompt_templates",
            column: "IsOfficial");

        migrationBuilder.CreateIndex(
            name: "IX_aik_prompt_templates_Name_Version",
            table: "aik_prompt_templates",
            columns: new[] { "Name", "Version" },
            unique: true);

        // ── 2. Tool Definitions ─────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "aik_tool_definitions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ParametersSchema = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                RiskLevel = table.Column<int>(type: "integer", nullable: false),
                IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                TimeoutMs = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "text", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_aik_tool_definitions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_aik_tool_definitions_Name",
            table: "aik_tool_definitions",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_aik_tool_definitions_Category",
            table: "aik_tool_definitions",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_aik_tool_definitions_IsActive",
            table: "aik_tool_definitions",
            column: "IsActive");

        // ── 3. FinOps: Cost attribution on token ledger ─────────────────
        migrationBuilder.AddColumn<decimal>(
            name: "CostPerInputToken",
            table: "aik_token_usage_ledger",
            type: "numeric(18,12)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "CostPerOutputToken",
            table: "aik_token_usage_ledger",
            type: "numeric(18,12)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "EstimatedCostUsd",
            table: "aik_token_usage_ledger",
            type: "numeric(18,8)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CostCurrency",
            table: "aik_token_usage_ledger",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CostPerInputToken", table: "aik_token_usage_ledger");
        migrationBuilder.DropColumn(name: "CostPerOutputToken", table: "aik_token_usage_ledger");
        migrationBuilder.DropColumn(name: "EstimatedCostUsd", table: "aik_token_usage_ledger");
        migrationBuilder.DropColumn(name: "CostCurrency", table: "aik_token_usage_ledger");

        migrationBuilder.DropTable(name: "aik_tool_definitions");
        migrationBuilder.DropTable(name: "aik_prompt_templates");
    }
}
