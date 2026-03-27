using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P7_3_NotificationSourceEventId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceEventId",
                table: "ntf_notifications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceEventId",
                table: "ntf_notifications");
        }
    }
}
