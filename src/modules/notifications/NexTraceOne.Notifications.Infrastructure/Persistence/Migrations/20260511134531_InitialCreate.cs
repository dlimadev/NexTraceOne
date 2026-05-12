using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                name: "ntf_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SourceModule = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceEntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceEntityId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequiresAction = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    AcknowledgeComment = table.Column<string>(type: "text", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationKey = table.Column<string>(type: "text", nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    LastOccurrenceAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SnoozedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SnoozedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsEscalated = table.Column<bool>(type: "boolean", nullable: false),
                    EscalatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelatedIncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSuppressed = table.Column<bool>(type: "boolean", nullable: false),
                    SuppressionReason = table.Column<string>(type: "text", nullable: true),
                    SourceEventId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ntf_notifications", x => x.Id);
                    table.CheckConstraint("CK_ntf_notifications_category", "\"Category\" IN ('Incident', 'Approval', 'Change', 'Contract', 'Security', 'Compliance', 'FinOps', 'AI', 'Integration', 'Platform', 'Informational')");
                    table.CheckConstraint("CK_ntf_notifications_severity", "\"Severity\" IN ('Info', 'ActionRequired', 'Warning', 'Critical')");
                    table.CheckConstraint("CK_ntf_notifications_status", "\"Status\" IN ('Unread', 'Read', 'Acknowledged', 'Archived', 'Dismissed')");
                });

            migrationBuilder.CreateTable(
                name: "ntf_outbox_messages",
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
                    table.PrimaryKey("PK_ntf_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ntf_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ntf_preferences", x => x.Id);
                    table.CheckConstraint("CK_ntf_preferences_category", "\"Category\" IN ('Incident', 'Approval', 'Change', 'Contract', 'Security', 'Compliance', 'FinOps', 'AI', 'Integration', 'Platform', 'Informational')");
                    table.CheckConstraint("CK_ntf_preferences_channel", "\"Channel\" IN ('InApp', 'Email', 'MicrosoftTeams')");
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
                    EncryptedPassword = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
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

            migrationBuilder.CreateTable(
                name: "ntf_deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecipientAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ntf_deliveries", x => x.Id);
                    table.CheckConstraint("CK_ntf_deliveries_channel", "\"Channel\" IN ('InApp', 'Email', 'MicrosoftTeams')");
                    table.CheckConstraint("CK_ntf_deliveries_status", "\"Status\" IN ('Pending', 'Delivered', 'Failed', 'Skipped', 'RetryScheduled')");
                    table.ForeignKey(
                        name: "FK_ntf_deliveries_ntf_notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "ntf_notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_ntf_deliveries_Channel",
                table: "ntf_deliveries",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_deliveries_NotificationId",
                table: "ntf_deliveries",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_deliveries_Status",
                table: "ntf_deliveries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_deliveries_Status_NextRetryAt",
                table: "ntf_deliveries",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ntf_deliveries_Status_RetryCount",
                table: "ntf_deliveries",
                columns: new[] { "Status", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_ntf_notifications_CreatedAt",
                table: "ntf_notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_notifications_RecipientUserId",
                table: "ntf_notifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_notifications_RecipientUserId_Status",
                table: "ntf_notifications",
                columns: new[] { "RecipientUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ntf_notifications_Status",
                table: "ntf_notifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_notifications_TenantId",
                table: "ntf_notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_outbox_messages_CreatedAt",
                table: "ntf_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_outbox_messages_IdempotencyKey",
                table: "ntf_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ntf_outbox_messages_ProcessedAt",
                table: "ntf_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_preferences_TenantId",
                table: "ntf_preferences",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ntf_preferences_TenantId_UserId_Category_Channel",
                table: "ntf_preferences",
                columns: new[] { "TenantId", "UserId", "Category", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ntf_preferences_UserId",
                table: "ntf_preferences",
                column: "UserId");

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
                name: "ntf_deliveries");

            migrationBuilder.DropTable(
                name: "ntf_outbox_messages");

            migrationBuilder.DropTable(
                name: "ntf_preferences");

            migrationBuilder.DropTable(
                name: "ntf_smtp_configurations");

            migrationBuilder.DropTable(
                name: "ntf_templates");

            migrationBuilder.DropTable(
                name: "ntf_notifications");
        }
    }
}
