using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dp_code_generation_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContractVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GenerationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GeneratedCode = table.Column<string>(type: "text", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    TemplateId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dp_code_generation_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dp_playground_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RequestPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequestBody = table.Column<string>(type: "text", nullable: true),
                    RequestHeaders = table.Column<string>(type: "text", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dp_playground_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dp_portal_analytics_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SearchQuery = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ZeroResults = table.Column<bool>(type: "boolean", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dp_portal_analytics_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dp_saved_searches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SearchQuery = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Filters = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dp_saved_searches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dp_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ConsumerServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConsumerServiceVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    WebhookUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastNotifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dp_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
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
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dp_code_generation_records_ApiAssetId",
                table: "dp_code_generation_records",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_dp_code_generation_records_RequestedById",
                table: "dp_code_generation_records",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_dp_playground_sessions_ApiAssetId",
                table: "dp_playground_sessions",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_dp_playground_sessions_UserId",
                table: "dp_playground_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_dp_portal_analytics_events_EventType",
                table: "dp_portal_analytics_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_dp_portal_analytics_events_OccurredAt",
                table: "dp_portal_analytics_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_dp_portal_analytics_events_UserId",
                table: "dp_portal_analytics_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_dp_saved_searches_UserId",
                table: "dp_saved_searches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_dp_subscriptions_ApiAssetId",
                table: "dp_subscriptions",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_dp_subscriptions_ApiAssetId_SubscriberId",
                table: "dp_subscriptions",
                columns: new[] { "ApiAssetId", "SubscriberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dp_subscriptions_SubscriberId",
                table: "dp_subscriptions",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_CreatedAt",
                table: "outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_IdempotencyKey",
                table: "outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                table: "outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dp_code_generation_records");

            migrationBuilder.DropTable(
                name: "dp_playground_sessions");

            migrationBuilder.DropTable(
                name: "dp_portal_analytics_events");

            migrationBuilder.DropTable(
                name: "dp_saved_searches");

            migrationBuilder.DropTable(
                name: "dp_subscriptions");

            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
