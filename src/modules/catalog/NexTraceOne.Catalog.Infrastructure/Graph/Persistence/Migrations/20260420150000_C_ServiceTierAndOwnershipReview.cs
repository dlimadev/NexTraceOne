using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class C_ServiceTierAndOwnershipReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Adicionar Tier (enum como string) em cat_service_assets ──────
            migrationBuilder.AddColumn<string>(
                name: "tier",
                table: "cat_service_assets",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Standard");

            // ── Adicionar LastOwnershipReviewAt ──────────────────────────────
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_ownership_review_at",
                table: "cat_service_assets",
                type: "timestamp with time zone",
                nullable: true);

            // ── Índice por Tier (filtragem por tier) ─────────────────────────
            migrationBuilder.CreateIndex(
                name: "ix_cat_service_assets_tier",
                table: "cat_service_assets",
                column: "tier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cat_service_assets_tier",
                table: "cat_service_assets");

            migrationBuilder.DropColumn(
                name: "last_ownership_review_at",
                table: "cat_service_assets");

            migrationBuilder.DropColumn(
                name: "tier",
                table: "cat_service_assets");
        }
    }
}
