using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aud_audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceModule = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_audit_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aud_retention_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_retention_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aud_outbox_messages",
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
                    table.PrimaryKey("PK_aud_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aud_audit_chain_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    CurrentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AuditEventId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_audit_chain_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_aud_audit_chain_links_aud_audit_events_AuditEventId",
                        column: x => x.AuditEventId,
                        principalTable: "aud_audit_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_chain_links_AuditEventId",
                table: "aud_audit_chain_links",
                column: "AuditEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_chain_links_CurrentHash",
                table: "aud_audit_chain_links",
                column: "CurrentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_chain_links_SequenceNumber",
                table: "aud_audit_chain_links",
                column: "SequenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_ActionType",
                table: "aud_audit_events",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_OccurredAt",
                table: "aud_audit_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_PerformedBy",
                table: "aud_audit_events",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_SourceModule",
                table: "aud_audit_events",
                column: "SourceModule");

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_TenantId",
                table: "aud_audit_events",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_retention_policies_IsActive",
                table: "aud_retention_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aud_outbox_messages_CreatedAt",
                table: "aud_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aud_outbox_messages_IdempotencyKey",
                table: "aud_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aud_outbox_messages_ProcessedAt",
                table: "aud_outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aud_audit_chain_links");

            migrationBuilder.DropTable(
                name: "aud_retention_policies");

            migrationBuilder.DropTable(
                name: "aud_outbox_messages");

            migrationBuilder.DropTable(
                name: "aud_audit_events");
        }
    }
}
