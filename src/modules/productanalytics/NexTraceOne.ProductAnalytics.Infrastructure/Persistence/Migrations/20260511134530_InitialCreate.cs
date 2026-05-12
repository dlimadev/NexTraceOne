using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ProductAnalytics.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pan_analytics_events",
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
                    table.PrimaryKey("PK_pan_analytics_events", x => x.Id);
                    table.CheckConstraint("CK_pan_analytics_events_event_type", "\"EventType\" IN ('ModuleViewed','EntityViewed','SearchExecuted','SearchResultClicked','ZeroResultSearch','QuickActionTriggered','AssistantPromptSubmitted','AssistantResponseUsed','ContractDraftCreated','ContractPublished','ChangeViewed','IncidentInvestigated','MitigationWorkflowStarted','MitigationWorkflowCompleted','EvidencePackageExported','PolicyViewed','ExecutiveOverviewViewed','RunbookViewed','SourceOfTruthQueried','ReportGenerated','OnboardingStepCompleted','JourneyAbandoned','EmptyStateEncountered','ReliabilityDashboardViewed','AutomationWorkflowManaged')");
                    table.CheckConstraint("CK_pan_analytics_events_module", "\"Module\" IN ('Dashboard','ServiceCatalog','SourceOfTruth','ContractStudio','ChangeIntelligence','Incidents','Reliability','Runbooks','AiAssistant','Governance','ExecutiveViews','FinOps','IntegrationHub','DeveloperPortal','Admin','Automation','Search')");
                });

            migrationBuilder.CreateTable(
                name: "pan_journey_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StepsJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pan_journey_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pan_outbox_messages",
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
                    table.PrimaryKey("PK_pan_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_EventType",
                table: "pan_analytics_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_Module",
                table: "pan_analytics_events",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_Module_EventType",
                table: "pan_analytics_events",
                columns: new[] { "Module", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_OccurredAt",
                table: "pan_analytics_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_Persona",
                table: "pan_analytics_events",
                column: "Persona");

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_SessionId_OccurredAt",
                table: "pan_analytics_events",
                columns: new[] { "SessionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_TenantId_Module_OccurredAt",
                table: "pan_analytics_events",
                columns: new[] { "TenantId", "Module", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_TenantId_OccurredAt",
                table: "pan_analytics_events",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_TenantId_UserId_OccurredAt",
                table: "pan_analytics_events",
                columns: new[] { "TenantId", "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pan_analytics_events_UserId",
                table: "pan_analytics_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_pan_journey_definitions_TenantId_IsActive",
                table: "pan_journey_definitions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_pan_journey_definitions_TenantId_Key",
                table: "pan_journey_definitions",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pan_outbox_messages_CreatedAt",
                table: "pan_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_pan_outbox_messages_IdempotencyKey",
                table: "pan_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pan_outbox_messages_ProcessedAt",
                table: "pan_outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pan_analytics_events");

            migrationBuilder.DropTable(
                name: "pan_journey_definitions");

            migrationBuilder.DropTable(
                name: "pan_outbox_messages");
        }
    }
}
