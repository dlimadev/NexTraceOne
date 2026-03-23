using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeparateSharedEntityOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_orch_contexts");

            migrationBuilder.DropTable(
                name: "ai_orch_conversations");

            migrationBuilder.DropTable(
                name: "ai_orch_knowledge_entries");

            migrationBuilder.DropTable(
                name: "ai_orch_test_artifacts");

            migrationBuilder.DropTable(
                name: "ext_ai_consultations");

            migrationBuilder.DropTable(
                name: "ext_ai_knowledge_captures");

            migrationBuilder.DropTable(
                name: "ext_ai_policies");

            migrationBuilder.DropTable(
                name: "ext_ai_providers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_orch_contexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssembledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ContextType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TokenEstimate = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_orch_contexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_orch_conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastTurnAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Summary = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Topic = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TurnCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_orch_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_orch_knowledge_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Relevance = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SuggestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_orch_knowledge_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_orch_test_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedCode = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TestFramework = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_orch_test_artifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ext_ai_consultations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Context = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Query = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Response = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
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
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReuseCount = table.Column<int>(type: "integer", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
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
                    AllowedContexts = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MaxDailyQueries = table.Column<int>(type: "integer", nullable: false),
                    MaxTokensPerDay = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
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
                    CostPerToken = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MaxTokensPerRequest = table.Column<int>(type: "integer", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ext_ai_providers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_contexts_AssembledAt",
                table: "ai_orch_contexts",
                column: "AssembledAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_contexts_ContextType",
                table: "ai_orch_contexts",
                column: "ContextType");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_contexts_ServiceName",
                table: "ai_orch_contexts",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_conversations_ServiceName",
                table: "ai_orch_conversations",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_conversations_StartedAt",
                table: "ai_orch_conversations",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_conversations_StartedBy",
                table: "ai_orch_conversations",
                column: "StartedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_conversations_Status",
                table: "ai_orch_conversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_knowledge_entries_ConversationId",
                table: "ai_orch_knowledge_entries",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_knowledge_entries_Status",
                table: "ai_orch_knowledge_entries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_knowledge_entries_SuggestedAt",
                table: "ai_orch_knowledge_entries",
                column: "SuggestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_test_artifacts_GeneratedAt",
                table: "ai_orch_test_artifacts",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_test_artifacts_ReleaseId",
                table: "ai_orch_test_artifacts",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_test_artifacts_ServiceName",
                table: "ai_orch_test_artifacts",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_ai_orch_test_artifacts_Status",
                table: "ai_orch_test_artifacts",
                column: "Status");

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
        }
    }
}
