using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oi_reliability_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric", nullable: false),
                    RuntimeHealthScore = table.Column<decimal>(type: "numeric", nullable: false),
                    IncidentImpactScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ObservabilityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    OpenIncidentCount = table.Column<int>(type: "integer", nullable: false),
                    RuntimeHealthStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrendDirection = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_reliability_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_rel_outbox_messages",
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
                    table.PrimaryKey("PK_oi_rel_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_oi_reliability_snapshots_TenantId_ComputedAt",
                table: "oi_reliability_snapshots",
                columns: new[] { "TenantId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_oi_reliability_snapshots_TenantId_ServiceId_ComputedAt",
                table: "oi_reliability_snapshots",
                columns: new[] { "TenantId", "ServiceId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_oi_rel_outbox_messages_CreatedAt",
                table: "oi_rel_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_oi_rel_outbox_messages_IdempotencyKey",
                table: "oi_rel_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oi_rel_outbox_messages_ProcessedAt",
                table: "oi_rel_outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oi_reliability_snapshots");

            migrationBuilder.DropTable(
                name: "oi_rel_outbox_messages");
        }
    }
}
