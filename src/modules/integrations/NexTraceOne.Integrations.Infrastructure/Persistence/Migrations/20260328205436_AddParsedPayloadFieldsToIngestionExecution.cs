using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParsedPayloadFieldsToIngestionExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ParsedAt",
                table: "int_ingestion_executions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedChangeType",
                table: "int_ingestion_executions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedCommitSha",
                table: "int_ingestion_executions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedEnvironment",
                table: "int_ingestion_executions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedServiceName",
                table: "int_ingestion_executions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedVersion",
                table: "int_ingestion_executions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "int_ingestion_executions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "MetadataRecorded");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_executions_ParsedServiceName",
                table: "int_ingestion_executions",
                column: "ParsedServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_executions_ProcessingStatus",
                table: "int_ingestion_executions",
                column: "ProcessingStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_int_ingestion_executions_ParsedServiceName",
                table: "int_ingestion_executions");

            migrationBuilder.DropIndex(
                name: "IX_int_ingestion_executions_ProcessingStatus",
                table: "int_ingestion_executions");

            migrationBuilder.DropColumn(
                name: "ParsedAt",
                table: "int_ingestion_executions");

            migrationBuilder.DropColumn(
                name: "ParsedChangeType",
                table: "int_ingestion_executions");

            migrationBuilder.DropColumn(
                name: "ParsedCommitSha",
                table: "int_ingestion_executions");

            migrationBuilder.DropColumn(
                name: "ParsedEnvironment",
                table: "int_ingestion_executions");

            migrationBuilder.DropColumn(
                name: "ParsedServiceName",
                table: "int_ingestion_executions");

            migrationBuilder.DropColumn(
                name: "ParsedVersion",
                table: "int_ingestion_executions");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "int_ingestion_executions");
        }
    }
}
