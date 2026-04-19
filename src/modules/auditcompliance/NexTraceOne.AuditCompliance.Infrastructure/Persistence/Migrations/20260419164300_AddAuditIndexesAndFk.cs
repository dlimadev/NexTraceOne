using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adiciona:
///   - Index composto (TenantId, OccurredAt) em aud_audit_events para acelerar queries por tenant + período.
///   - Index composto (ResourceType, ResourceId) em aud_audit_events para acelerar GetTrailByResource.
///   - FK de aud_compliance_results → aud_compliance_policies para garantir integridade referencial.
/// </summary>
public partial class AddAuditIndexesAndFk : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Composite index: TenantId + OccurredAt — acelera pesquisa por tenant num intervalo de datas
        migrationBuilder.CreateIndex(
            name: "IX_aud_audit_events_TenantId_OccurredAt",
            table: "aud_audit_events",
            columns: ["TenantId", "OccurredAt"]);

        // Composite index: ResourceType + ResourceId — acelera GetTrailByResource
        migrationBuilder.CreateIndex(
            name: "IX_aud_audit_events_ResourceType_ResourceId",
            table: "aud_audit_events",
            columns: ["ResourceType", "ResourceId"]);

        // FK: aud_compliance_results.PolicyId → aud_compliance_policies.Id
        // Garante que não existam resultados órfãos sem política correspondente.
        // RESTRICT para impedir eliminação acidental de políticas com histórico.
        migrationBuilder.AddForeignKey(
            name: "FK_aud_compliance_results_aud_compliance_policies_PolicyId",
            table: "aud_compliance_results",
            column: "PolicyId",
            principalTable: "aud_compliance_policies",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_aud_compliance_results_aud_compliance_policies_PolicyId",
            table: "aud_compliance_results");

        migrationBuilder.DropIndex(
            name: "IX_aud_audit_events_ResourceType_ResourceId",
            table: "aud_audit_events");

        migrationBuilder.DropIndex(
            name: "IX_aud_audit_events_TenantId_OccurredAt",
            table: "aud_audit_events");
    }
}
