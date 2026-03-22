using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase5Enrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to gov_integration_connectors
            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "gov_integration_connectors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Production");

            migrationBuilder.AddColumn<string>(
                name: "AuthenticationMode",
                table: "gov_integration_connectors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Not configured");

            migrationBuilder.AddColumn<string>(
                name: "PollingMode",
                table: "gov_integration_connectors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Not configured");

            migrationBuilder.AddColumn<string>(
                name: "AllowedTeams",
                table: "gov_integration_connectors",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            // Add new column to gov_ingestion_sources
            migrationBuilder.AddColumn<string>(
                name: "DataDomain",
                table: "gov_ingestion_sources",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Add new column to gov_ingestion_executions
            migrationBuilder.AddColumn<int>(
                name: "RetryAttempt",
                table: "gov_ingestion_executions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Create indices
            migrationBuilder.CreateIndex(
                name: "IX_gov_integration_connectors_Environment",
                table: "gov_integration_connectors",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_sources_DataDomain",
                table: "gov_ingestion_sources",
                column: "DataDomain");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_executions_RetryAttempt",
                table: "gov_ingestion_executions",
                column: "RetryAttempt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indices
            migrationBuilder.DropIndex(
                name: "IX_gov_integration_connectors_Environment",
                table: "gov_integration_connectors");

            migrationBuilder.DropIndex(
                name: "IX_gov_ingestion_sources_DataDomain",
                table: "gov_ingestion_sources");

            migrationBuilder.DropIndex(
                name: "IX_gov_ingestion_executions_RetryAttempt",
                table: "gov_ingestion_executions");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "Environment",
                table: "gov_integration_connectors");

            migrationBuilder.DropColumn(
                name: "AuthenticationMode",
                table: "gov_integration_connectors");

            migrationBuilder.DropColumn(
                name: "PollingMode",
                table: "gov_integration_connectors");

            migrationBuilder.DropColumn(
                name: "AllowedTeams",
                table: "gov_integration_connectors");

            migrationBuilder.DropColumn(
                name: "DataDomain",
                table: "gov_ingestion_sources");

            migrationBuilder.DropColumn(
                name: "RetryAttempt",
                table: "gov_ingestion_executions");
        }
    }
}
