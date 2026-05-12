using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aik_contexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContextType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    TokenEstimate = table.Column<int>(type: "integer", nullable: false),
                    AssembledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_contexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_knowledge_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Relevance = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValidatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SuggestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_knowledge_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_orch_conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Topic = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TurnCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastTurnAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Summary = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_orch_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aik_test_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TestFramework = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GeneratedCode = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_test_artifacts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aik_contexts_AssembledAt",
                table: "aik_contexts",
                column: "AssembledAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_contexts_ContextType",
                table: "aik_contexts",
                column: "ContextType");

            migrationBuilder.CreateIndex(
                name: "IX_aik_contexts_ServiceName",
                table: "aik_contexts",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_aik_knowledge_entries_ConversationId",
                table: "aik_knowledge_entries",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_knowledge_entries_Status",
                table: "aik_knowledge_entries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aik_knowledge_entries_SuggestedAt",
                table: "aik_knowledge_entries",
                column: "SuggestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_orch_conversations_ServiceName",
                table: "aik_orch_conversations",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_aik_orch_conversations_StartedAt",
                table: "aik_orch_conversations",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_orch_conversations_StartedBy",
                table: "aik_orch_conversations",
                column: "StartedBy");

            migrationBuilder.CreateIndex(
                name: "IX_aik_orch_conversations_Status",
                table: "aik_orch_conversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aik_outbox_messages_CreatedAt",
                table: "aik_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_outbox_messages_IdempotencyKey",
                table: "aik_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_outbox_messages_ProcessedAt",
                table: "aik_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_test_artifacts_GeneratedAt",
                table: "aik_test_artifacts",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aik_test_artifacts_ReleaseId",
                table: "aik_test_artifacts",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_aik_test_artifacts_ServiceName",
                table: "aik_test_artifacts",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_aik_test_artifacts_Status",
                table: "aik_test_artifacts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aik_contexts");

            migrationBuilder.DropTable(
                name: "aik_knowledge_entries");

            migrationBuilder.DropTable(
                name: "aik_orch_conversations");

            migrationBuilder.DropTable(
                name: "aik_outbox_messages");

            migrationBuilder.DropTable(
                name: "aik_test_artifacts");
        }
    }
}
