using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CHG_AddPromotionReviewerAndReleaseTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 2.2: ReviewedBy, ReviewNotes, TenantId para PromotionRequests
            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "PromotionRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "PromotionRequests",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PromotionRequests",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);  // Retrocompatibilidade: requests existentes recebem Guid.Empty

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRequests_TenantId",
                table: "PromotionRequests",
                column: "TenantId");

            // Fase 2.3: DeploymentDurationMs, SucceededAt, FailedAt para Releases
            migrationBuilder.AddColumn<long>(
                name: "DeploymentDurationMs",
                table: "Releases",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SucceededAt",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedAt",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PromotionRequests_TenantId",
                table: "PromotionRequests");

            migrationBuilder.DropColumn(name: "ReviewedBy", table: "PromotionRequests");
            migrationBuilder.DropColumn(name: "ReviewNotes", table: "PromotionRequests");
            migrationBuilder.DropColumn(name: "TenantId", table: "PromotionRequests");
            migrationBuilder.DropColumn(name: "DeploymentDurationMs", table: "Releases");
            migrationBuilder.DropColumn(name: "SucceededAt", table: "Releases");
            migrationBuilder.DropColumn(name: "FailedAt", table: "Releases");
        }
    }
}
