using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CostCurrency",
                table: "aik_token_usage_ledger",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

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

            migrationBuilder.CreateTable(
                name: "aik_guardrails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "aik_prompt_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Variables = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetPersonas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ScopeHint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Relevance = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "aik_tool_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
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
                name: "IX_aik_evaluations_AgentExecutionId",
                table: "aik_evaluations",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_ConversationId",
                table: "aik_evaluations",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_EvaluatedAt",
                table: "aik_evaluations",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_OverallScore",
                table: "aik_evaluations",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_TenantId",
                table: "aik_evaluations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_evaluations_UserId",
                table: "aik_evaluations",
                column: "UserId");

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
                name: "IX_aik_guardrails_Name",
                table: "aik_guardrails",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_guardrails_Priority",
                table: "aik_guardrails",
                column: "Priority");

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
                name: "IX_aik_prompt_templates_Name",
                table: "aik_prompt_templates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_aik_prompt_templates_Name_Version",
                table: "aik_prompt_templates",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_tool_definitions_Category",
                table: "aik_tool_definitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_aik_tool_definitions_IsActive",
                table: "aik_tool_definitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_tool_definitions_Name",
                table: "aik_tool_definitions",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aik_evaluations");

            migrationBuilder.DropTable(
                name: "aik_guardrails");

            migrationBuilder.DropTable(
                name: "aik_prompt_templates");

            migrationBuilder.DropTable(
                name: "aik_tool_definitions");

            migrationBuilder.DropColumn(
                name: "CostCurrency",
                table: "aik_token_usage_ledger");

            migrationBuilder.DropColumn(
                name: "CostPerInputToken",
                table: "aik_token_usage_ledger");

            migrationBuilder.DropColumn(
                name: "CostPerOutputToken",
                table: "aik_token_usage_ledger");

            migrationBuilder.DropColumn(
                name: "EstimatedCostUsd",
                table: "aik_token_usage_ledger");
        }
    }
}
