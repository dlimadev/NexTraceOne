using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ops_incident_narratives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    NarrativeText = table.Column<string>(type: "text", nullable: false),
                    SymptomsSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    TimelineSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    ProbableCauseSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    MitigationSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    RelatedChangesSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    AffectedServicesSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    ModelUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefreshCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_incident_narratives", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ops_incident_narratives_IncidentId",
                table: "ops_incident_narratives",
                column: "IncidentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ops_incident_narratives_tenant_id",
                table: "ops_incident_narratives",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_incident_narratives");
        }
    }
}
