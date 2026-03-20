using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialExternalAiSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ext_ai_consultations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Context = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Query = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Response = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ext_ai_consultations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ext_ai_knowledge_captures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReuseCount = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ext_ai_knowledge_captures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ext_ai_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MaxDailyQueries = table.Column<int>(type: "integer", nullable: false),
                    MaxTokensPerDay = table.Column<long>(type: "bigint", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedContexts = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ext_ai_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ext_ai_providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxTokensPerRequest = table.Column<int>(type: "integer", nullable: false),
                    CostPerToken = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ext_ai_providers", x => x.Id);
                });

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS outbox_messages (
                    ""Id"" uuid NOT NULL,
                    ""EventType"" character varying(1000) NOT NULL,
                    ""Payload"" text NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""ProcessedAt"" timestamp with time zone,
                    ""RetryCount"" integer NOT NULL,
                    ""LastError"" character varying(4000),
                    ""TenantId"" uuid NOT NULL,
                    CONSTRAINT ""PK_outbox_messages"" PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_consultations_ProviderId",
                table: "ext_ai_consultations",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_consultations_RequestedAt",
                table: "ext_ai_consultations",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_consultations_RequestedBy",
                table: "ext_ai_consultations",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_consultations_Status",
                table: "ext_ai_consultations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_knowledge_captures_CapturedAt",
                table: "ext_ai_knowledge_captures",
                column: "CapturedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_knowledge_captures_Category",
                table: "ext_ai_knowledge_captures",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_knowledge_captures_ConsultationId",
                table: "ext_ai_knowledge_captures",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_knowledge_captures_Status",
                table: "ext_ai_knowledge_captures",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_policies_IsActive",
                table: "ext_ai_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_policies_Name",
                table: "ext_ai_policies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_providers_IsActive",
                table: "ext_ai_providers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_providers_Name",
                table: "ext_ai_providers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ext_ai_providers_Priority",
                table: "ext_ai_providers",
                column: "Priority");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_outbox_messages_CreatedAt"" ON outbox_messages (""CreatedAt"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_outbox_messages_ProcessedAt"" ON outbox_messages (""ProcessedAt"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ext_ai_consultations");

            migrationBuilder.DropTable(
                name: "ext_ai_knowledge_captures");

            migrationBuilder.DropTable(
                name: "ext_ai_policies");

            migrationBuilder.DropTable(
                name: "ext_ai_providers");

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS outbox_messages;");
        }
    }
}
