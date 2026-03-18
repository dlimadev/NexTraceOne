using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gov_analytics_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Feature = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Route = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DomainId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClientType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_analytics_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_integration_connectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConnectorType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Health = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastSuccessAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastErrorAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FreshnessLagMinutes = table.Column<int>(type: "integer", nullable: true),
                    TotalExecutions = table.Column<long>(type: "bigint", nullable: false),
                    SuccessfulExecutions = table.Column<long>(type: "bigint", nullable: false),
                    FailedExecutions = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_integration_connectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_ingestion_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TrustLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FreshnessStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastDataReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DataItemsProcessed = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedIntervalMinutes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_ingestion_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gov_ingestion_sources_gov_integration_connectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "gov_integration_connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gov_ingestion_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemsProcessed = table.Column<int>(type: "integer", nullable: false),
                    ItemsSucceeded = table.Column<int>(type: "integer", nullable: false),
                    ItemsFailed = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_ingestion_executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gov_ingestion_executions_gov_ingestion_sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "gov_ingestion_sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_gov_ingestion_executions_gov_integration_connectors_Connect~",
                        column: x => x.ConnectorId,
                        principalTable: "gov_integration_connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gov_analytics_events_EventType",
                table: "gov_analytics_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_gov_analytics_events_Module",
                table: "gov_analytics_events",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_gov_analytics_events_OccurredAt",
                table: "gov_analytics_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_analytics_events_Persona",
                table: "gov_analytics_events",
                column: "Persona");

            migrationBuilder.CreateIndex(
                name: "IX_gov_analytics_events_UserId",
                table: "gov_analytics_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_executions_ConnectorId",
                table: "gov_ingestion_executions",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_executions_ConnectorId_StartedAt",
                table: "gov_ingestion_executions",
                columns: new[] { "ConnectorId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_executions_CorrelationId",
                table: "gov_ingestion_executions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_executions_Result",
                table: "gov_ingestion_executions",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_executions_SourceId",
                table: "gov_ingestion_executions",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_executions_StartedAt",
                table: "gov_ingestion_executions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_sources_ConnectorId",
                table: "gov_ingestion_sources",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_sources_FreshnessStatus",
                table: "gov_ingestion_sources",
                column: "FreshnessStatus");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_sources_Name",
                table: "gov_ingestion_sources",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_sources_SourceType",
                table: "gov_ingestion_sources",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_sources_Status",
                table: "gov_ingestion_sources",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_ingestion_sources_TrustLevel",
                table: "gov_ingestion_sources",
                column: "TrustLevel");

            migrationBuilder.CreateIndex(
                name: "IX_gov_integration_connectors_ConnectorType",
                table: "gov_integration_connectors",
                column: "ConnectorType");

            migrationBuilder.CreateIndex(
                name: "IX_gov_integration_connectors_Health",
                table: "gov_integration_connectors",
                column: "Health");

            migrationBuilder.CreateIndex(
                name: "IX_gov_integration_connectors_Name",
                table: "gov_integration_connectors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_integration_connectors_Provider",
                table: "gov_integration_connectors",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_gov_integration_connectors_Status",
                table: "gov_integration_connectors",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gov_analytics_events");

            migrationBuilder.DropTable(
                name: "gov_ingestion_executions");

            migrationBuilder.DropTable(
                name: "gov_ingestion_sources");

            migrationBuilder.DropTable(
                name: "gov_integration_connectors");
        }
    }
}
