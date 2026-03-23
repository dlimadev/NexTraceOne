using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                name: "gov_delegated_administrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GranteeUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GranteeDisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DomainId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_delegated_administrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_domains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CapabilityClassification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_domains", x => x.Id);
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
                name: "gov_pack_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rules = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultEnforcementMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_pack_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_packs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_packs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_rollout_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnforcementMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InitiatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InitiatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_rollout_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_team_domain_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnershipType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_team_domain_links", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ParentOrganizationUnit = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_waivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Justification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EvidenceLinks = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_waivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_outbox_messages",
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
                    table.PrimaryKey("PK_gov_outbox_messages", x => x.Id);
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
                name: "IX_gov_delegated_administrations_DomainId",
                table: "gov_delegated_administrations",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_ExpiresAt",
                table: "gov_delegated_administrations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_GranteeUserId",
                table: "gov_delegated_administrations",
                column: "GranteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_IsActive",
                table: "gov_delegated_administrations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_Scope",
                table: "gov_delegated_administrations",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_TeamId",
                table: "gov_delegated_administrations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_domains_CapabilityClassification",
                table: "gov_domains",
                column: "CapabilityClassification");

            migrationBuilder.CreateIndex(
                name: "IX_gov_domains_Criticality",
                table: "gov_domains",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_gov_domains_Name",
                table: "gov_domains",
                column: "Name",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_gov_pack_versions_PackId",
                table: "gov_pack_versions",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_pack_versions_PackId_Version",
                table: "gov_pack_versions",
                columns: new[] { "PackId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_pack_versions_PublishedAt",
                table: "gov_pack_versions",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_packs_Category",
                table: "gov_packs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_gov_packs_Name",
                table: "gov_packs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_packs_Status",
                table: "gov_packs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_CompletedAt",
                table: "gov_rollout_records",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_InitiatedAt",
                table: "gov_rollout_records",
                column: "InitiatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_PackId",
                table: "gov_rollout_records",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_Scope",
                table: "gov_rollout_records",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_Status",
                table: "gov_rollout_records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_VersionId",
                table: "gov_rollout_records",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_DomainId",
                table: "gov_team_domain_links",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_OwnershipType",
                table: "gov_team_domain_links",
                column: "OwnershipType");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_TeamId",
                table: "gov_team_domain_links",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_TeamId_DomainId",
                table: "gov_team_domain_links",
                columns: new[] { "TeamId", "DomainId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_teams_Name",
                table: "gov_teams",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_teams_ParentOrganizationUnit",
                table: "gov_teams",
                column: "ParentOrganizationUnit");

            migrationBuilder.CreateIndex(
                name: "IX_gov_teams_Status",
                table: "gov_teams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_ExpiresAt",
                table: "gov_waivers",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_PackId",
                table: "gov_waivers",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_RequestedBy",
                table: "gov_waivers",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_Scope",
                table: "gov_waivers",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_Status",
                table: "gov_waivers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_outbox_messages_CreatedAt",
                table: "gov_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_outbox_messages_IdempotencyKey",
                table: "gov_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_outbox_messages_ProcessedAt",
                table: "gov_outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gov_analytics_events");

            migrationBuilder.DropTable(
                name: "gov_delegated_administrations");

            migrationBuilder.DropTable(
                name: "gov_domains");

            migrationBuilder.DropTable(
                name: "gov_ingestion_executions");

            migrationBuilder.DropTable(
                name: "gov_pack_versions");

            migrationBuilder.DropTable(
                name: "gov_packs");

            migrationBuilder.DropTable(
                name: "gov_rollout_records");

            migrationBuilder.DropTable(
                name: "gov_team_domain_links");

            migrationBuilder.DropTable(
                name: "gov_teams");

            migrationBuilder.DropTable(
                name: "gov_waivers");

            migrationBuilder.DropTable(
                name: "gov_outbox_messages");

            migrationBuilder.DropTable(
                name: "gov_ingestion_sources");

            migrationBuilder.DropTable(
                name: "gov_integration_connectors");
        }
    }
}
