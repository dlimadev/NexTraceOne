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
                name: "int_event_consumer_dead_letters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Topic = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PartitionKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    FirstAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_event_consumer_dead_letters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "int_log_to_metric_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Pattern = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MetricName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MetricType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValueExtractor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LabelsJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_log_to_metric_rules", x => x.Id);
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
                name: "int_storage_buckets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BucketName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BackendType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    FilterJson = table.Column<string>(type: "jsonb", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsFallback = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_storage_buckets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "int_tenant_pipeline_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RuleType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SignalType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConditionJson = table.Column<string>(type: "jsonb", nullable: false),
                    ActionJson = table.Column<string>(type: "jsonb", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_tenant_pipeline_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "int_webhook_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EventTypes = table.Column<string>(type: "jsonb", nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeliveryCount = table.Column<long>(type: "bigint", nullable: false),
                    SuccessCount = table.Column<long>(type: "bigint", nullable: false),
                    FailureCount = table.Column<long>(type: "bigint", nullable: false),
                    LastTriggeredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_webhook_subscriptions", x => x.Id);
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ParsedServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ParsedEnvironment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ParsedVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ParsedCommitSha = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ParsedChangeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ParsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "MetadataRecorded")
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
                name: "IX_int_event_consumer_dead_letters_IsResolved",
                table: "int_event_consumer_dead_letters",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_int_event_consumer_dead_letters_TenantId",
                table: "int_event_consumer_dead_letters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_int_event_consumer_dead_letters_TenantId_IsResolved",
                table: "int_event_consumer_dead_letters",
                columns: new[] { "TenantId", "IsResolved" });

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
                name: "IX_int_ingestion_executions_ParsedServiceName",
                table: "int_ingestion_executions",
                column: "ParsedServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_int_ingestion_executions_ProcessingStatus",
                table: "int_ingestion_executions",
                column: "ProcessingStatus");

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
                name: "IX_int_log_to_metric_rules_tenant_id",
                table: "int_log_to_metric_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_int_log_to_metric_rules_tenant_id_IsEnabled",
                table: "int_log_to_metric_rules",
                columns: new[] { "tenant_id", "IsEnabled" });

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

            migrationBuilder.CreateIndex(
                name: "IX_int_storage_buckets_tenant_id",
                table: "int_storage_buckets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_int_storage_buckets_tenant_id_BucketName",
                table: "int_storage_buckets",
                columns: new[] { "tenant_id", "BucketName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_int_storage_buckets_tenant_id_IsEnabled_Priority",
                table: "int_storage_buckets",
                columns: new[] { "tenant_id", "IsEnabled", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_int_tenant_pipeline_rules_tenant_id",
                table: "int_tenant_pipeline_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_int_tenant_pipeline_rules_tenant_id_RuleType_Priority",
                table: "int_tenant_pipeline_rules",
                columns: new[] { "tenant_id", "RuleType", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_int_tenant_pipeline_rules_tenant_id_SignalType_IsEnabled",
                table: "int_tenant_pipeline_rules",
                columns: new[] { "tenant_id", "SignalType", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_int_webhook_subscriptions_tenant_id",
                table: "int_webhook_subscriptions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_int_webhook_subscriptions_tenant_id_IsActive",
                table: "int_webhook_subscriptions",
                columns: new[] { "tenant_id", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_int_webhook_subscriptions_tenant_id_Name",
                table: "int_webhook_subscriptions",
                columns: new[] { "tenant_id", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "int_event_consumer_dead_letters");

            migrationBuilder.DropTable(
                name: "int_ingestion_executions");

            migrationBuilder.DropTable(
                name: "int_log_to_metric_rules");

            migrationBuilder.DropTable(
                name: "int_outbox_messages");

            migrationBuilder.DropTable(
                name: "int_storage_buckets");

            migrationBuilder.DropTable(
                name: "int_tenant_pipeline_rules");

            migrationBuilder.DropTable(
                name: "int_webhook_subscriptions");

            migrationBuilder.DropTable(
                name: "int_ingestion_sources");

            migrationBuilder.DropTable(
                name: "int_connectors");
        }
    }
}
