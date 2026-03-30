using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase01_AddContractSlaAndWebhookType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SlaAvailabilityTarget",
                table: "ctr_contract_versions",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlaDocumentReference",
                table: "ctr_contract_versions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaLatencyP95Ms",
                table: "ctr_contract_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaLatencyP99Ms",
                table: "ctr_contract_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaMaintenanceWindowMinutes",
                table: "ctr_contract_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SlaMaxErrorRatePercent",
                table: "ctr_contract_versions",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaMinThroughputRps",
                table: "ctr_contract_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlaTier",
                table: "ctr_contract_versions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlaAvailabilityTarget",
                table: "ctr_contract_versions");

            migrationBuilder.DropColumn(
                name: "SlaDocumentReference",
                table: "ctr_contract_versions");

            migrationBuilder.DropColumn(
                name: "SlaLatencyP95Ms",
                table: "ctr_contract_versions");

            migrationBuilder.DropColumn(
                name: "SlaLatencyP99Ms",
                table: "ctr_contract_versions");

            migrationBuilder.DropColumn(
                name: "SlaMaintenanceWindowMinutes",
                table: "ctr_contract_versions");

            migrationBuilder.DropColumn(
                name: "SlaMaxErrorRatePercent",
                table: "ctr_contract_versions");

            migrationBuilder.DropColumn(
                name: "SlaMinThroughputRps",
                table: "ctr_contract_versions");

            migrationBuilder.DropColumn(
                name: "SlaTier",
                table: "ctr_contract_versions");
        }
    }
}
