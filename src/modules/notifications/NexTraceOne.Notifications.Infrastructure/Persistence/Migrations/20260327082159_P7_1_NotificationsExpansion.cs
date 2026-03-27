using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P7_1_NotificationsExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ntf_channel_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ntf_channel_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ntf_smtp_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    UseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    Username = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EncryptedPassword = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FromAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FromName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ntf_smtp_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ntf_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BodyTemplate = table.Column<string>(type: "text", nullable: false),
                    PlainTextTemplate = table.Column<string>(type: "text", nullable: true),
                    Channel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ntf_templates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ntf_channel_configurations_TenantId",
                table: "ntf_channel_configurations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_channel_configurations_TenantId_ChannelType",
                table: "ntf_channel_configurations",
                columns: new[] { "TenantId", "ChannelType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ntf_smtp_configurations_TenantId",
                table: "ntf_smtp_configurations",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ntf_templates_EventType",
                table: "ntf_templates",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_templates_IsActive",
                table: "ntf_templates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_templates_TenantId",
                table: "ntf_templates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_templates_TenantId_EventType_Channel_Locale",
                table: "ntf_templates",
                columns: new[] { "TenantId", "EventType", "Channel", "Locale" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ntf_channel_configurations");

            migrationBuilder.DropTable(
                name: "ntf_smtp_configurations");

            migrationBuilder.DropTable(
                name: "ntf_templates");
        }
    }
}
