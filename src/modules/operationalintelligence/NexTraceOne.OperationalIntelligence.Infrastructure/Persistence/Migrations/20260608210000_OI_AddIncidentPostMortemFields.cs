using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OI_AddIncidentPostMortemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 4.7: RootCause + SlaBreached para análise pós-incidente (PIR)
            migrationBuilder.AddColumn<string>(
                name: "RootCause",
                table: "Incidents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SlaBreached",
                table: "Incidents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RootCause", table: "Incidents");
            migrationBuilder.DropColumn(name: "SlaBreached", table: "Incidents");
        }
    }
}
