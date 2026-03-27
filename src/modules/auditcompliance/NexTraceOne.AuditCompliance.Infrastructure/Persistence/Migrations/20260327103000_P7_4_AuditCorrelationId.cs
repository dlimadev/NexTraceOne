using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class P7_4_AuditCorrelationId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CorrelationId",
            table: "aud_audit_events",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_aud_audit_events_CorrelationId",
            table: "aud_audit_events",
            column: "CorrelationId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_aud_audit_events_CorrelationId",
            table: "aud_audit_events");

        migrationBuilder.DropColumn(
            name: "CorrelationId",
            table: "aud_audit_events");
    }
}
