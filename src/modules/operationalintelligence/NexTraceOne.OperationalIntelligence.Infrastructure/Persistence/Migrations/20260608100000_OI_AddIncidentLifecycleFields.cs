using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OI_AddIncidentLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 1.1: ResolvedAt, AcknowledgedAt, AcknowledgedBy para opi_incident_records
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "Incidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcknowledgedAt",
                table: "Incidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcknowledgedBy",
                table: "Incidents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ResolvedAt", table: "Incidents");
            migrationBuilder.DropColumn(name: "AcknowledgedAt", table: "Incidents");
            migrationBuilder.DropColumn(name: "AcknowledgedBy", table: "Incidents");
        }
    }
}
