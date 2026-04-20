using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ProductAnalytics.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_Module_EventType",
                table: "pan_analytics_events",
                columns: new[] { "Module", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_SessionId_OccurredAt",
                table: "pan_analytics_events",
                columns: new[] { "SessionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_TenantId_UserId_OccurredAt",
                table: "pan_analytics_events",
                columns: new[] { "TenantId", "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pan_analytics_events_Module_EventType",
                table: "pan_analytics_events");

            migrationBuilder.DropIndex(
                name: "IX_pan_analytics_events_SessionId_OccurredAt",
                table: "pan_analytics_events");

            migrationBuilder.DropIndex(
                name: "IX_pan_analytics_events_TenantId_UserId_OccurredAt",
                table: "pan_analytics_events");
        }
    }
}
