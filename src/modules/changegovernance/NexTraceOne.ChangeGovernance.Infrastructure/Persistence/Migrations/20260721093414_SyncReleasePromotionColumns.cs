using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncReleasePromotionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DeploymentDurationMs",
                table: "Releases",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedAt",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SucceededAt",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "PromotionRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "PromotionRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PromotionRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeploymentDurationMs",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "SucceededAt",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "PromotionRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "PromotionRequests");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PromotionRequests");
        }
    }
}
