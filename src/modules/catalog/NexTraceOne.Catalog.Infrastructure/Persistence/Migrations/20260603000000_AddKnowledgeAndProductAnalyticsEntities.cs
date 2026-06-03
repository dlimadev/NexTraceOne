using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeAndProductAnalyticsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Knowledge tables ─────────────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "knw_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Slug = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastEditorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    freshness_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    last_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_documents", x => x.Id);
                    table.CheckConstraint("CK_knw_documents_category", "\"Category\" IN ('General','Runbook','Troubleshooting','Architecture','Procedure','PostMortem','Reference')");
                    table.CheckConstraint("CK_knw_documents_status", "\"Status\" IN ('Draft','Published','Archived','Deprecated')");
                });

            migrationBuilder.CreateTable(
                name: "knw_knowledge_graph_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalNodes = table.Column<int>(type: "integer", nullable: false),
                    TotalEdges = table.Column<int>(type: "integer", nullable: false),
                    ConnectedComponents = table.Column<int>(type: "integer", nullable: false),
                    IsolatedNodes = table.Column<int>(type: "integer", nullable: false),
                    CoverageScore = table.Column<int>(type: "integer", nullable: false),
                    NodeTypeDistribution = table.Column<string>(type: "jsonb", nullable: false),
                    EdgeTypeDistribution = table.Column<string>(type: "jsonb", nullable: false),
                    TopConnectedEntities = table.Column<string>(type: "jsonb", nullable: true),
                    OrphanEntities = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_knowledge_graph_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "knw_operational_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NoteType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContextType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "jsonb", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_operational_notes", x => x.Id);
                    table.CheckConstraint("CK_knw_operational_notes_note_type", "\"NoteType\" IN ('Observation','Mitigation','Decision','Hypothesis','FollowUp')");
                    table.CheckConstraint("CK_knw_operational_notes_severity", "\"Severity\" IN ('Info','Warning','Critical')");
                });

            migrationBuilder.CreateTable(
                name: "knw_proposed_runbooks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content_markdown = table.Column<string>(type: "text", nullable: false),
                    source_incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    team_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    proposed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_proposed_runbooks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knw_relations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Context = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_relations", x => x.Id);
                    table.CheckConstraint("CK_knw_relations_source_entity_type", "\"SourceEntityType\" IN ('KnowledgeDocument','OperationalNote')");
                    table.CheckConstraint("CK_knw_relations_target_type", "\"TargetType\" IN ('Service','Contract','Change','Incident','KnowledgeDocument','Runbook','Other')");
                });

            // ── ProductAnalytics tables ──────────────────────────────────────────

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
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pan_analytics_events", x => x.Id);
                    table.CheckConstraint("CK_pan_analytics_events_event_type", "\"EventType\" IN ('ModuleViewed','EntityViewed','SearchExecuted','SearchResultClicked','ZeroResultSearch','QuickActionTriggered','AssistantPromptSubmitted','AssistantResponseUsed','ContractDraftCreated','ContractPublished','ChangeViewed','IncidentInvestigated','MitigationWorkflowStarted','MitigationWorkflowCompleted','EvidencePackageExported','PolicyViewed','ExecutiveOverviewViewed','RunbookViewed','SourceOfTruthQueried','ReportGenerated','OnboardingStepCompleted','JourneyAbandoned','EmptyStateEncountered','ReliabilityDashboardViewed','AutomationWorkflowManaged','ServiceCreated')");
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

            // ── Knowledge indexes ────────────────────────────────────────────────

            migrationBuilder.CreateIndex("IX_knw_documents_AuthorId", "knw_documents", "AuthorId");
            migrationBuilder.CreateIndex("IX_knw_documents_Category", "knw_documents", "Category");
            migrationBuilder.CreateIndex("IX_knw_documents_CreatedAt", "knw_documents", "CreatedAt");
            migrationBuilder.CreateIndex("IX_knw_documents_Slug", "knw_documents", "Slug", unique: true);
            migrationBuilder.CreateIndex("IX_knw_documents_Status", "knw_documents", "Status");

            migrationBuilder.CreateIndex("IX_knw_knowledge_graph_snapshots_GeneratedAt", "knw_knowledge_graph_snapshots", "GeneratedAt");
            migrationBuilder.CreateIndex("IX_knw_knowledge_graph_snapshots_Status", "knw_knowledge_graph_snapshots", "Status");
            migrationBuilder.CreateIndex("ix_knw_knowledge_graph_snapshots_tenant_id", "knw_knowledge_graph_snapshots", "TenantId");

            migrationBuilder.CreateIndex("IX_knw_operational_notes_AuthorId", "knw_operational_notes", "AuthorId");
            migrationBuilder.CreateIndex("IX_knw_operational_notes_ContextEntityId", "knw_operational_notes", "ContextEntityId");
            migrationBuilder.CreateIndex("IX_knw_operational_notes_ContextType", "knw_operational_notes", "ContextType");
            migrationBuilder.CreateIndex("IX_knw_operational_notes_ContextType_ContextEntityId", "knw_operational_notes", new[] { "ContextType", "ContextEntityId" });
            migrationBuilder.CreateIndex("IX_knw_operational_notes_CreatedAt", "knw_operational_notes", "CreatedAt");
            migrationBuilder.CreateIndex("IX_knw_operational_notes_IsResolved", "knw_operational_notes", "IsResolved");
            migrationBuilder.CreateIndex("IX_knw_operational_notes_NoteType", "knw_operational_notes", "NoteType");
            migrationBuilder.CreateIndex("IX_knw_operational_notes_Origin", "knw_operational_notes", "Origin");
            migrationBuilder.CreateIndex("IX_knw_operational_notes_Severity", "knw_operational_notes", "Severity");

            migrationBuilder.CreateIndex("uix_knw_proposed_runbooks_incident", "knw_proposed_runbooks", "source_incident_id", unique: true);

            migrationBuilder.CreateIndex("IX_knw_relations_SourceEntityId", "knw_relations", "SourceEntityId");
            migrationBuilder.CreateIndex("IX_knw_relations_SourceEntityId_TargetEntityId", "knw_relations", new[] { "SourceEntityId", "TargetEntityId" }, unique: true);
            migrationBuilder.CreateIndex("IX_knw_relations_TargetEntityId", "knw_relations", "TargetEntityId");
            migrationBuilder.CreateIndex("IX_knw_relations_TargetType", "knw_relations", "TargetType");
            migrationBuilder.CreateIndex("IX_knw_relations_TargetType_TargetEntityId", "knw_relations", new[] { "TargetType", "TargetEntityId" });

            // ── ProductAnalytics indexes ─────────────────────────────────────────

            migrationBuilder.CreateIndex("IX_pan_analytics_events_CreatedAt", "pan_analytics_events", "CreatedAt");
            migrationBuilder.CreateIndex("IX_pan_analytics_events_EventType", "pan_analytics_events", "EventType");
            migrationBuilder.CreateIndex("IX_pan_analytics_events_Module", "pan_analytics_events", "Module");
            migrationBuilder.CreateIndex("IX_pan_analytics_events_Module_EventType", "pan_analytics_events", new[] { "Module", "EventType" });
            migrationBuilder.CreateIndex("IX_pan_analytics_events_OccurredAt", "pan_analytics_events", "OccurredAt");
            migrationBuilder.CreateIndex("IX_pan_analytics_events_Persona", "pan_analytics_events", "Persona");
            migrationBuilder.CreateIndex("IX_pan_analytics_events_SessionId_OccurredAt", "pan_analytics_events", new[] { "SessionId", "OccurredAt" });
            migrationBuilder.CreateIndex("IX_pan_analytics_events_TenantId_Module_OccurredAt", "pan_analytics_events", new[] { "TenantId", "Module", "OccurredAt" });
            migrationBuilder.CreateIndex("IX_pan_analytics_events_TenantId_OccurredAt", "pan_analytics_events", new[] { "TenantId", "OccurredAt" });
            migrationBuilder.CreateIndex("IX_pan_analytics_events_TenantId_UserId_OccurredAt", "pan_analytics_events", new[] { "TenantId", "UserId", "OccurredAt" });
            migrationBuilder.CreateIndex("IX_pan_analytics_events_UserId", "pan_analytics_events", "UserId");

            migrationBuilder.CreateIndex("IX_pan_journey_definitions_TenantId_IsActive", "pan_journey_definitions", new[] { "TenantId", "IsActive" });
            migrationBuilder.CreateIndex("UX_pan_journey_definitions_TenantId_Key", "pan_journey_definitions", new[] { "TenantId", "Key" }, unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "knw_documents");
            migrationBuilder.DropTable(name: "knw_knowledge_graph_snapshots");
            migrationBuilder.DropTable(name: "knw_operational_notes");
            migrationBuilder.DropTable(name: "knw_proposed_runbooks");
            migrationBuilder.DropTable(name: "knw_relations");
            migrationBuilder.DropTable(name: "pan_analytics_events");
            migrationBuilder.DropTable(name: "pan_journey_definitions");
        }
    }
}
