using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "knw_outbox_messages",
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
                    table.PrimaryKey("PK_knw_outbox_messages", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "IX_knw_documents_AuthorId",
                table: "knw_documents",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_documents_Category",
                table: "knw_documents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_knw_documents_CreatedAt",
                table: "knw_documents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_knw_documents_Slug",
                table: "knw_documents",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knw_documents_Status",
                table: "knw_documents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_knw_knowledge_graph_snapshots_GeneratedAt",
                table: "knw_knowledge_graph_snapshots",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_knw_knowledge_graph_snapshots_Status",
                table: "knw_knowledge_graph_snapshots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_knw_knowledge_graph_snapshots_tenant_id",
                table: "knw_knowledge_graph_snapshots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_AuthorId",
                table: "knw_operational_notes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_ContextEntityId",
                table: "knw_operational_notes",
                column: "ContextEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_ContextType",
                table: "knw_operational_notes",
                column: "ContextType");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_ContextType_ContextEntityId",
                table: "knw_operational_notes",
                columns: new[] { "ContextType", "ContextEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_CreatedAt",
                table: "knw_operational_notes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_IsResolved",
                table: "knw_operational_notes",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_NoteType",
                table: "knw_operational_notes",
                column: "NoteType");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_Origin",
                table: "knw_operational_notes",
                column: "Origin");

            migrationBuilder.CreateIndex(
                name: "IX_knw_operational_notes_Severity",
                table: "knw_operational_notes",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_knw_outbox_messages_CreatedAt",
                table: "knw_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_knw_outbox_messages_IdempotencyKey",
                table: "knw_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knw_outbox_messages_ProcessedAt",
                table: "knw_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "uix_knw_proposed_runbooks_incident",
                table: "knw_proposed_runbooks",
                column: "source_incident_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knw_relations_SourceEntityId",
                table: "knw_relations",
                column: "SourceEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_relations_SourceEntityId_TargetEntityId",
                table: "knw_relations",
                columns: new[] { "SourceEntityId", "TargetEntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knw_relations_TargetEntityId",
                table: "knw_relations",
                column: "TargetEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_knw_relations_TargetType",
                table: "knw_relations",
                column: "TargetType");

            migrationBuilder.CreateIndex(
                name: "IX_knw_relations_TargetType_TargetEntityId",
                table: "knw_relations",
                columns: new[] { "TargetType", "TargetEntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "knw_documents");

            migrationBuilder.DropTable(
                name: "knw_knowledge_graph_snapshots");

            migrationBuilder.DropTable(
                name: "knw_operational_notes");

            migrationBuilder.DropTable(
                name: "knw_outbox_messages");

            migrationBuilder.DropTable(
                name: "knw_proposed_runbooks");

            migrationBuilder.DropTable(
                name: "knw_relations");
        }
    }
}
