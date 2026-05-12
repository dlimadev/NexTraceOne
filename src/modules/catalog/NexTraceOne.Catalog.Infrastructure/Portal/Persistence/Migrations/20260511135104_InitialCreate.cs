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
                name: "cat_code_generation_records",
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
                    table.PrimaryKey("PK_cat_code_generation_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_playground_sessions",
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
                    table.PrimaryKey("PK_cat_playground_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_portal_analytics_events",
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
                    table.PrimaryKey("PK_cat_portal_analytics_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_portal_api_keys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_portal_api_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_portal_contract_publications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PublishedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WithdrawnBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WithdrawnAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_portal_contract_publications", x => x.Id);
                    table.CheckConstraint("CK_cat_portal_contract_publications_status", "\"Status\" IN ('PendingPublication', 'Published', 'Withdrawn', 'Deprecated')");
                    table.CheckConstraint("CK_cat_portal_contract_publications_visibility", "\"Visibility\" IN ('Internal', 'External', 'RestrictedToTeams')");
                });

            migrationBuilder.CreateTable(
                name: "cat_portal_outbox_messages",
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
                    table.PrimaryKey("PK_cat_portal_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_portal_rate_limit_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestsPerMinute = table.Column<int>(type: "integer", nullable: false),
                    RequestsPerHour = table.Column<int>(type: "integer", nullable: false),
                    RequestsPerDay = table.Column<int>(type: "integer", nullable: false),
                    BurstLimit = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_portal_rate_limit_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_saved_searches",
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
                    table.PrimaryKey("PK_cat_saved_searches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_subscriptions",
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
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastNotifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_code_generation_records_ApiAssetId",
                table: "cat_code_generation_records",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_code_generation_records_RequestedById",
                table: "cat_code_generation_records",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_cat_playground_sessions_ApiAssetId",
                table: "cat_playground_sessions",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_playground_sessions_UserId",
                table: "cat_playground_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_analytics_events_EventType",
                table: "cat_portal_analytics_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_analytics_events_OccurredAt",
                table: "cat_portal_analytics_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_analytics_events_UserId",
                table: "cat_portal_analytics_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_api_keys_ApiAssetId",
                table: "cat_portal_api_keys",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_api_keys_KeyHash",
                table: "cat_portal_api_keys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_api_keys_OwnerId",
                table: "cat_portal_api_keys",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_contract_publications_ApiAssetId",
                table: "cat_portal_contract_publications",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_contract_publications_ContractVersionId",
                table: "cat_portal_contract_publications",
                column: "ContractVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_contract_publications_IsDeleted",
                table: "cat_portal_contract_publications",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_contract_publications_Status",
                table: "cat_portal_contract_publications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_outbox_messages_CreatedAt",
                table: "cat_portal_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_outbox_messages_IdempotencyKey",
                table: "cat_portal_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_outbox_messages_ProcessedAt",
                table: "cat_portal_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_rate_limit_policies_ApiAssetId",
                table: "cat_portal_rate_limit_policies",
                column: "ApiAssetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_saved_searches_UserId",
                table: "cat_saved_searches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_subscriptions_ApiAssetId",
                table: "cat_subscriptions",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_subscriptions_ApiAssetId_SubscriberId",
                table: "cat_subscriptions",
                columns: new[] { "ApiAssetId", "SubscriberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_subscriptions_SubscriberId",
                table: "cat_subscriptions",
                column: "SubscriberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_code_generation_records");

            migrationBuilder.DropTable(
                name: "cat_playground_sessions");

            migrationBuilder.DropTable(
                name: "cat_portal_analytics_events");

            migrationBuilder.DropTable(
                name: "cat_portal_api_keys");

            migrationBuilder.DropTable(
                name: "cat_portal_contract_publications");

            migrationBuilder.DropTable(
                name: "cat_portal_outbox_messages");

            migrationBuilder.DropTable(
                name: "cat_portal_rate_limit_policies");

            migrationBuilder.DropTable(
                name: "cat_saved_searches");

            migrationBuilder.DropTable(
                name: "cat_subscriptions");
        }
    }
}
