using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chg_change_confidence_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfidenceBefore = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceAfter = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Details = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_change_confidence_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_promotion_gate_evaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RuleResults = table.Column<string>(type: "jsonb", nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EvaluatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_promotion_gate_evaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chg_release_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalSummary = table.Column<string>(type: "text", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "text", nullable: true),
                    NewEndpointsSection = table.Column<string>(type: "text", nullable: true),
                    BreakingChangesSection = table.Column<string>(type: "text", nullable: true),
                    AffectedServicesSection = table.Column<string>(type: "text", nullable: true),
                    ConfidenceMetricsSection = table.Column<string>(type: "text", nullable: true),
                    EvidenceLinksSection = table.Column<string>(type: "text", nullable: true),
                    ModelUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRegeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RegenerationCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_release_notes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chg_change_confidence_events_OccurredAt",
                table: "chg_change_confidence_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_change_confidence_events_ReleaseId",
                table: "chg_change_confidence_events",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gate_evaluations_ChangeId",
                table: "chg_promotion_gate_evaluations",
                column: "ChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gate_evaluations_EvaluatedAt",
                table: "chg_promotion_gate_evaluations",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gate_evaluations_GateId",
                table: "chg_promotion_gate_evaluations",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_promotion_gate_evaluations_TenantId",
                table: "chg_promotion_gate_evaluations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_release_notes_GeneratedAt",
                table: "chg_release_notes",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chg_release_notes_ReleaseId",
                table: "chg_release_notes",
                column: "ReleaseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chg_change_confidence_events");

            migrationBuilder.DropTable(
                name: "chg_promotion_gate_evaluations");

            migrationBuilder.DropTable(
                name: "chg_release_notes");
        }
    }
}
