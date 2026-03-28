using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "int_connectors",
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
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Production"),
                    AuthenticationMode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "Not configured"),
                    PollingMode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "Not configured"),
                    AllowedTeams = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_connectors", x => x.Id);
                    table.CheckConstraint("CK_int_connectors_health", "\"Health\" IN ('Unknown','Healthy','Degraded','Unhealthy','Critical')");
                    table.CheckConstraint("CK_int_connectors_status", "\"Status\" IN ('Pending','Active','Paused','Disabled','Failed','Configuring')");
                });

            migrationBuilder.CreateTable(
                name: "int_outbox_messages",
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
                    table.PrimaryKey("PK_int_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "int_ingestion_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataDomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TrustLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FreshnessStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastDataReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DataItemsProcessed = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedIntervalMinutes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_ingestion_sources", x => x.Id);
                    table.CheckConstraint("CK_int_ingestion_sources_freshness_status", "\"FreshnessStatus\" IN ('Unknown','Fresh','Stale','Outdated','Expired')");
                    table.CheckConstraint("CK_int_ingestion_sources_status", "\"Status\" IN ('Pending','Active','Paused','Disabled','Error')");
                    table.CheckConstraint("CK_int_ingestion_sources_trust_level", "\"TrustLevel\" IN ('Unverified','Basic','Verified','Trusted','Official')");
                    table.ForeignKey(
                        name: "FK_int_ingestion_sources_int_connectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "int_connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "int_ingestion_executions",
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
                    RetryAttempt = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_ingestion_executions", x => x.Id);
                    table.CheckConstraint("CK_int_ingestion_executions_result", "\"Result\" IN ('Running','Success','PartialSuccess','Failed','Cancelled','TimedOut')");
                    table.ForeignKey(
                        name: "FK_int_ingestion_executions_int_connectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "int_connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_int_ingestion_executions_int_ingestion_sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "int_ingestion_sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_int_connectors_ConnectorType",
                table: "int_connectors",
                column: "ConnectorType");

            migrationBuilder.CreateIndex(
                name: "IX_int_connectors_Environment",
                table: "int_connectors",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_int_connectors_Health",
                table: "int_connectors",
                column: "Health");

            migrationBuilder.CreateIndex(
                name: "IX_int_connectors_Name",
                table: "int_connectors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_int_connectors_Provider",
                table: "int_connectors",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_int_connectors_Status",
                table: "int_connectors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_executions_ConnectorId",
                table: "int_ingestion_executions",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_executions_ConnectorId_StartedAt",
                table: "int_ingestion_executions",
                columns: new[] { "ConnectorId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_executions_CorrelationId",
                table: "int_ingestion_executions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_executions_Result",
                table: "int_ingestion_executions",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_executions_RetryAttempt",
                table: "int_ingestion_executions",
                column: "RetryAttempt");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_executions_SourceId",
                table: "int_ingestion_executions",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_executions_StartedAt",
                table: "int_ingestion_executions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_sources_ConnectorId",
                table: "int_ingestion_sources",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_sources_DataDomain",
                table: "int_ingestion_sources",
                column: "DataDomain");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_sources_FreshnessStatus",
                table: "int_ingestion_sources",
                column: "FreshnessStatus");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_sources_Name",
                table: "int_ingestion_sources",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_sources_SourceType",
                table: "int_ingestion_sources",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_sources_Status",
                table: "int_ingestion_sources",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_sources_TrustLevel",
                table: "int_ingestion_sources",
                column: "TrustLevel");

            migrationBuilder.CreateIndex(
                name: "IX_int_outbox_messages_CreatedAt",
                table: "int_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_int_outbox_messages_IdempotencyKey",
                table: "int_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_int_outbox_messages_ProcessedAt",
                table: "int_outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "int_ingestion_executions");

            migrationBuilder.DropTable(
                name: "int_outbox_messages");

            migrationBuilder.DropTable(
                name: "int_ingestion_sources");

            migrationBuilder.DropTable(
                name: "int_connectors");
        }
    }
}
