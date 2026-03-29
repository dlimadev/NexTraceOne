using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class W03_ExtendCorrelationWithLegacyAssetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ops_incident_change_correlations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false),
                    MatchType = table.Column<int>(type: "integer", nullable: false),
                    TimeWindowHours = table.Column<int>(type: "integer", nullable: false),
                    CorrelatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ChangeEnvironment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChangeOccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    legacy_asset_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    legacy_asset_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    legacy_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_incident_change_correlations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ops_icc_incident_change_unique",
                table: "ops_incident_change_correlations",
                columns: new[] { "IncidentId", "ChangeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ops_icc_incident_id",
                table: "ops_incident_change_correlations",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_icc_tenant_id",
                table: "ops_incident_change_correlations",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_incident_change_correlations");
        }
    }
}
