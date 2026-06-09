using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AIK_DropAiUsageEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 2.5: AIUsageEntry migrado de PostgreSQL (UsageEntries) para ClickHouse (ai_usage_entries).
            // O schema ClickHouse está definido em build/clickhouse/ai-analytics-schema.sql.
            migrationBuilder.DropTable(name: "UsageEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
