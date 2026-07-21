using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapAiHubTypedIdFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageEntries");

            migrationBuilder.AddColumn<Guid>(
                name: "SkillExecutionId",
                table: "SkillFeedbacks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SkillId",
                table: "SkillExecutions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "PromptVersions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConsultationId",
                table: "KnowledgeCaptures",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "KnowledgeCaptureEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SuiteId",
                table: "EvaluationRuns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SuiteId",
                table: "EvaluationCases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "Consultations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ExecutionId",
                table: "AgentTrajectoryFeedbacks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AgentId",
                table: "AgentPerformanceMetrics",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AgentId",
                table: "AgentExecutions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AgentId",
                table: "AgentArtifacts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ExecutionId",
                table: "AgentArtifacts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SkillFeedbacks_SkillExecutionId",
                table: "SkillFeedbacks",
                column: "SkillExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillExecutions_SkillId",
                table: "SkillExecutions",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptVersions_AssetId",
                table: "PromptVersions",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeCaptures_ConsultationId",
                table: "KnowledgeCaptures",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeCaptureEntries_ConversationId",
                table: "KnowledgeCaptureEntries",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_SuiteId",
                table: "EvaluationRuns",
                column: "SuiteId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCases_SuiteId",
                table: "EvaluationCases",
                column: "SuiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_ProviderId",
                table: "Consultations",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTrajectoryFeedbacks_ExecutionId",
                table: "AgentTrajectoryFeedbacks",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentPerformanceMetrics_AgentId",
                table: "AgentPerformanceMetrics",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_AgentId",
                table: "AgentExecutions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentArtifacts_AgentId",
                table: "AgentArtifacts",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentArtifacts_ExecutionId",
                table: "AgentArtifacts",
                column: "ExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SkillFeedbacks_SkillExecutionId",
                table: "SkillFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_SkillExecutions_SkillId",
                table: "SkillExecutions");

            migrationBuilder.DropIndex(
                name: "IX_PromptVersions_AssetId",
                table: "PromptVersions");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeCaptures_ConsultationId",
                table: "KnowledgeCaptures");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeCaptureEntries_ConversationId",
                table: "KnowledgeCaptureEntries");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationRuns_SuiteId",
                table: "EvaluationRuns");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationCases_SuiteId",
                table: "EvaluationCases");

            migrationBuilder.DropIndex(
                name: "IX_Consultations_ProviderId",
                table: "Consultations");

            migrationBuilder.DropIndex(
                name: "IX_AgentTrajectoryFeedbacks_ExecutionId",
                table: "AgentTrajectoryFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_AgentPerformanceMetrics_AgentId",
                table: "AgentPerformanceMetrics");

            migrationBuilder.DropIndex(
                name: "IX_AgentExecutions_AgentId",
                table: "AgentExecutions");

            migrationBuilder.DropIndex(
                name: "IX_AgentArtifacts_AgentId",
                table: "AgentArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_AgentArtifacts_ExecutionId",
                table: "AgentArtifacts");

            migrationBuilder.DropColumn(
                name: "SkillExecutionId",
                table: "SkillFeedbacks");

            migrationBuilder.DropColumn(
                name: "SkillId",
                table: "SkillExecutions");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "PromptVersions");

            migrationBuilder.DropColumn(
                name: "ConsultationId",
                table: "KnowledgeCaptures");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "KnowledgeCaptureEntries");

            migrationBuilder.DropColumn(
                name: "SuiteId",
                table: "EvaluationRuns");

            migrationBuilder.DropColumn(
                name: "SuiteId",
                table: "EvaluationCases");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "ExecutionId",
                table: "AgentTrajectoryFeedbacks");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "AgentPerformanceMetrics");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "AgentExecutions");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "AgentArtifacts");

            migrationBuilder.DropColumn(
                name: "ExecutionId",
                table: "AgentArtifacts");

            migrationBuilder.CreateTable(
                name: "UsageEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientType = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    ContextScope = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyName = table.Column<string>(type: "text", nullable: true),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    UserDisplayName = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageEntries", x => x.Id);
                });
        }
    }
}
