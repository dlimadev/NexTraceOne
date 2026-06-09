using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Config_AddNotificationPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 4.5: Priority em Notification (0=normal, 10=critical)
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "ntf_notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ntf_notifications_RecipientUserId_Priority",
                table: "ntf_notifications",
                columns: new[] { "RecipientUserId", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ntf_notifications_RecipientUserId_Priority",
                table: "ntf_notifications");

            migrationBuilder.DropColumn(name: "Priority", table: "ntf_notifications");
        }
    }
}
