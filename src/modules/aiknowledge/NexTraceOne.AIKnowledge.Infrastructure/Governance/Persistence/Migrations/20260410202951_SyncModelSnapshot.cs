using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_ide_query_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IdeClient = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IdeClientVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QueryType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    QueryText = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    QueryContext = table.Column<string>(type: "jsonb", nullable: true),
                    ResponseText = table.Column<string>(type: "text", nullable: true),
                    ModelUsed = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GovernanceCheckResult = table.Column<string>(type: "jsonb", nullable: true),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_ai_ide_query_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_onboarding_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ExperienceLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChecklistItems = table.Column<string>(type: "jsonb", nullable: false),
                    CompletedItems = table.Column<int>(type: "integer", nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    ServicesExplored = table.Column<string>(type: "jsonb", nullable: true),
                    ContractsReviewed = table.Column<string>(type: "jsonb", nullable: true),
                    RunbooksRead = table.Column<string>(type: "jsonb", nullable: true),
                    AiInteractionCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ai_onboarding_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_gov_feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rating = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Comment = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    AgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    QueryCategory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_gov_feedbacks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_IdeClient",
                table: "ai_ide_query_sessions",
                column: "IdeClient");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_ModelUsed",
                table: "ai_ide_query_sessions",
                column: "ModelUsed");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_QueryType",
                table: "ai_ide_query_sessions",
                column: "QueryType");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_Status",
                table: "ai_ide_query_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_SubmittedAt",
                table: "ai_ide_query_sessions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_TenantId",
                table: "ai_ide_query_sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_ide_query_sessions_UserId",
                table: "ai_ide_query_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_onboarding_sessions_StartedAt",
                table: "ai_onboarding_sessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_onboarding_sessions_Status",
                table: "ai_onboarding_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ai_onboarding_sessions_TeamId",
                table: "ai_onboarding_sessions",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_onboarding_sessions_TenantId",
                table: "ai_onboarding_sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_onboarding_sessions_UserId",
                table: "ai_onboarding_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_AgentExecutionId",
                table: "aik_gov_feedbacks",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_AgentName",
                table: "aik_gov_feedbacks",
                column: "AgentName");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_ConversationId",
                table: "aik_gov_feedbacks",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_CreatedByUserId",
                table: "aik_gov_feedbacks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_Rating",
                table: "aik_gov_feedbacks",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_SubmittedAt",
                table: "aik_gov_feedbacks",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_gov_feedbacks_TenantId",
                table: "aik_gov_feedbacks",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_ide_query_sessions");

            migrationBuilder.DropTable(
                name: "ai_onboarding_sessions");

            migrationBuilder.DropTable(
                name: "aik_gov_feedbacks");
        }
    }
}
