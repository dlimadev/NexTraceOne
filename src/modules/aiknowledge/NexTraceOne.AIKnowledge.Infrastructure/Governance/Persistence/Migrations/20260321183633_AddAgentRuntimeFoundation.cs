using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentRuntimeFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowModelOverride",
                table: "ai_gov_agents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AllowedModelIds",
                table: "ai_gov_agents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AllowedTools",
                table: "ai_gov_agents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "ExecutionCount",
                table: "ai_gov_agents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "InputSchema",
                table: "ai_gov_agents",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Objective",
                table: "ai_gov_agents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OutputSchema",
                table: "ai_gov_agents",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "ai_gov_agents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerTeamId",
                table: "ai_gov_agents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnershipType",
                table: "ai_gov_agents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicationStatus",
                table: "ai_gov_agents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ai_gov_agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "ai_gov_agents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ai_gov_agent_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_gov_agent_artifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_gov_agent_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelIdUsed = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InputJson = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    OutputJson = table.Column<string>(type: "character varying(64000)", maxLength: 64000, nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Steps = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    ContextJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_gov_agent_executions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agents_OwnerId",
                table: "ai_gov_agents",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agents_OwnershipType",
                table: "ai_gov_agents",
                column: "OwnershipType");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agents_PublicationStatus",
                table: "ai_gov_agents",
                column: "PublicationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agent_artifacts_AgentId",
                table: "ai_gov_agent_artifacts",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agent_artifacts_ArtifactType",
                table: "ai_gov_agent_artifacts",
                column: "ArtifactType");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agent_artifacts_ExecutionId",
                table: "ai_gov_agent_artifacts",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agent_artifacts_ReviewStatus",
                table: "ai_gov_agent_artifacts",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agent_executions_AgentId",
                table: "ai_gov_agent_executions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agent_executions_CorrelationId",
                table: "ai_gov_agent_executions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agent_executions_ExecutedBy",
                table: "ai_gov_agent_executions",
                column: "ExecutedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agent_executions_StartedAt",
                table: "ai_gov_agent_executions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_agent_executions_Status",
                table: "ai_gov_agent_executions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_gov_agent_artifacts");

            migrationBuilder.DropTable(
                name: "ai_gov_agent_executions");

            migrationBuilder.DropIndex(
                name: "IX_ai_gov_agents_OwnerId",
                table: "ai_gov_agents");

            migrationBuilder.DropIndex(
                name: "IX_ai_gov_agents_OwnershipType",
                table: "ai_gov_agents");

            migrationBuilder.DropIndex(
                name: "IX_ai_gov_agents_PublicationStatus",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "AllowModelOverride",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "AllowedModelIds",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "AllowedTools",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "ExecutionCount",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "InputSchema",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "Objective",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "OutputSchema",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "OwnerTeamId",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "PublicationStatus",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ai_gov_agents");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "ai_gov_agents");
        }
    }
}
