using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P7_2_DeliveryRetryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ntf_deliveries_status",
                table: "ntf_deliveries");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptAt",
                table: "ntf_deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextRetryAt",
                table: "ntf_deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ntf_deliveries_Status_NextRetryAt",
                table: "ntf_deliveries",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ntf_deliveries_status",
                table: "ntf_deliveries",
                sql: "\"Status\" IN ('Pending', 'Delivered', 'Failed', 'Skipped', 'RetryScheduled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ntf_deliveries_Status_NextRetryAt",
                table: "ntf_deliveries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ntf_deliveries_status",
                table: "ntf_deliveries");

            migrationBuilder.DropColumn(
                name: "LastAttemptAt",
                table: "ntf_deliveries");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "ntf_deliveries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ntf_deliveries_status",
                table: "ntf_deliveries",
                sql: "\"Status\" IN ('Pending', 'Delivered', 'Failed', 'Skipped')");
        }
    }
}
