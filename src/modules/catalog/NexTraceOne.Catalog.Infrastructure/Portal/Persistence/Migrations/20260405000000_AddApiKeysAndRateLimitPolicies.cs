using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeysAndRateLimitPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to cat_subscriptions for approval workflow
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "cat_subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "cat_subscriptions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "cat_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "cat_subscriptions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            // Create API Keys table
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
                name: "IX_cat_portal_api_keys_ApiAssetId",
                table: "cat_portal_api_keys",
                column: "ApiAssetId");

            // Create Rate Limit Policies table
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

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_rate_limit_policies_ApiAssetId",
                table: "cat_portal_rate_limit_policies",
                column: "ApiAssetId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "cat_portal_api_keys");
            migrationBuilder.DropTable(name: "cat_portal_rate_limit_policies");

            migrationBuilder.DropColumn(name: "Status", table: "cat_subscriptions");
            migrationBuilder.DropColumn(name: "ApprovedBy", table: "cat_subscriptions");
            migrationBuilder.DropColumn(name: "ApprovedAt", table: "cat_subscriptions");
            migrationBuilder.DropColumn(name: "RejectionReason", table: "cat_subscriptions");
        }
    }
}
