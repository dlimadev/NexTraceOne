using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aud_audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceModule = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_audit_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aud_campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CampaignType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledStartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_campaigns", x => x.Id);
                    table.CheckConstraint("CK_aud_campaigns_status", "\"Status\" IN ('Planned','InProgress','Completed','Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "aud_compliance_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EvaluationCriteria = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_compliance_policies", x => x.Id);
                    table.CheckConstraint("CK_aud_compliance_policies_severity", "\"Severity\" IN ('Low','Medium','High','Critical')");
                });

            migrationBuilder.CreateTable(
                name: "aud_outbox_messages",
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
                    table.PrimaryKey("PK_aud_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aud_retention_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_retention_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aud_audit_chain_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    CurrentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AuditEventId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_audit_chain_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_aud_audit_chain_links_aud_audit_events_AuditEventId",
                        column: x => x.AuditEventId,
                        principalTable: "aud_audit_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aud_compliance_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    EvaluatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aud_compliance_results", x => x.Id);
                    table.CheckConstraint("CK_aud_compliance_results_outcome", "\"Outcome\" IN ('Compliant','NonCompliant','PartiallyCompliant','NotApplicable')");
                    table.ForeignKey(
                        name: "FK_aud_compliance_results_aud_compliance_policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "aud_compliance_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_chain_links_AuditEventId",
                table: "aud_audit_chain_links",
                column: "AuditEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_chain_links_CurrentHash",
                table: "aud_audit_chain_links",
                column: "CurrentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_chain_links_SequenceNumber",
                table: "aud_audit_chain_links",
                column: "SequenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_ActionType",
                table: "aud_audit_events",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_CorrelationId",
                table: "aud_audit_events",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_OccurredAt",
                table: "aud_audit_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_PerformedBy",
                table: "aud_audit_events",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_ResourceType_ResourceId",
                table: "aud_audit_events",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_SourceModule",
                table: "aud_audit_events",
                column: "SourceModule");

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_TenantId",
                table: "aud_audit_events",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_audit_events_TenantId_OccurredAt",
                table: "aud_audit_events",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_aud_campaigns_CampaignType",
                table: "aud_campaigns",
                column: "CampaignType");

            migrationBuilder.CreateIndex(
                name: "IX_aud_campaigns_Status",
                table: "aud_campaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aud_campaigns_TenantId",
                table: "aud_campaigns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_policies_Category",
                table: "aud_compliance_policies",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_policies_IsActive",
                table: "aud_compliance_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_policies_Severity",
                table: "aud_compliance_policies",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_policies_TenantId",
                table: "aud_compliance_policies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_results_CampaignId",
                table: "aud_compliance_results",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_results_EvaluatedAt",
                table: "aud_compliance_results",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_results_Outcome",
                table: "aud_compliance_results",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_results_PolicyId",
                table: "aud_compliance_results",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_compliance_results_TenantId",
                table: "aud_compliance_results",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_aud_outbox_messages_CreatedAt",
                table: "aud_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aud_outbox_messages_IdempotencyKey",
                table: "aud_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aud_outbox_messages_ProcessedAt",
                table: "aud_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aud_retention_policies_IsActive",
                table: "aud_retention_policies",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aud_audit_chain_links");

            migrationBuilder.DropTable(
                name: "aud_campaigns");

            migrationBuilder.DropTable(
                name: "aud_compliance_results");

            migrationBuilder.DropTable(
                name: "aud_outbox_messages");

            migrationBuilder.DropTable(
                name: "aud_retention_policies");

            migrationBuilder.DropTable(
                name: "aud_audit_events");

            migrationBuilder.DropTable(
                name: "aud_compliance_policies");
        }
    }
}
