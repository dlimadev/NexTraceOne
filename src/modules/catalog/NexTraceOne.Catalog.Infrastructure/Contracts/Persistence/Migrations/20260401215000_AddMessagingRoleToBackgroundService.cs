using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations;

/// <summary>
/// Adiciona campos de messaging role (Producer/Consumer/Both/None) e tópicos
/// consumidos/produzidos às tabelas de BackgroundServiceContractDetail e BackgroundServiceDraftMetadata.
/// Permite que um Background Service declare explicitamente se é producer, consumer, ou ambos,
/// e quais tópicos/entidades/serviços consome e produz.
/// </summary>
public partial class AddMessagingRoleToBackgroundService : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── BackgroundServiceContractDetail (published contracts) ────────────────

        migrationBuilder.AddColumn<string>(
            name: "MessagingRole",
            table: "ctr_background_service_contract_details",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "None");

        migrationBuilder.AddColumn<string>(
            name: "ConsumedTopicsJson",
            table: "ctr_background_service_contract_details",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "ProducedTopicsJson",
            table: "ctr_background_service_contract_details",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "ConsumedServicesJson",
            table: "ctr_background_service_contract_details",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "ProducedEventsJson",
            table: "ctr_background_service_contract_details",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddCheckConstraint(
            name: "CK_ctr_bg_service_details_messaging_role",
            table: "ctr_background_service_contract_details",
            sql: "\"MessagingRole\" IN ('None', 'Producer', 'Consumer', 'Both')");

        // ── BackgroundServiceDraftMetadata (draft contracts) ─────────────────────

        migrationBuilder.AddColumn<string>(
            name: "MessagingRole",
            table: "ctr_background_service_draft_metadata",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "None");

        migrationBuilder.AddColumn<string>(
            name: "ConsumedTopicsJson",
            table: "ctr_background_service_draft_metadata",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "ProducedTopicsJson",
            table: "ctr_background_service_draft_metadata",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "ConsumedServicesJson",
            table: "ctr_background_service_draft_metadata",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "ProducedEventsJson",
            table: "ctr_background_service_draft_metadata",
            type: "text",
            nullable: false,
            defaultValue: "[]");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // ── BackgroundServiceContractDetail ──────────────────────────────────────

        migrationBuilder.DropCheckConstraint(
            name: "CK_ctr_bg_service_details_messaging_role",
            table: "ctr_background_service_contract_details");

        migrationBuilder.DropColumn(name: "MessagingRole", table: "ctr_background_service_contract_details");
        migrationBuilder.DropColumn(name: "ConsumedTopicsJson", table: "ctr_background_service_contract_details");
        migrationBuilder.DropColumn(name: "ProducedTopicsJson", table: "ctr_background_service_contract_details");
        migrationBuilder.DropColumn(name: "ConsumedServicesJson", table: "ctr_background_service_contract_details");
        migrationBuilder.DropColumn(name: "ProducedEventsJson", table: "ctr_background_service_contract_details");

        // ── BackgroundServiceDraftMetadata ───────────────────────────────────────

        migrationBuilder.DropColumn(name: "MessagingRole", table: "ctr_background_service_draft_metadata");
        migrationBuilder.DropColumn(name: "ConsumedTopicsJson", table: "ctr_background_service_draft_metadata");
        migrationBuilder.DropColumn(name: "ProducedTopicsJson", table: "ctr_background_service_draft_metadata");
        migrationBuilder.DropColumn(name: "ConsumedServicesJson", table: "ctr_background_service_draft_metadata");
        migrationBuilder.DropColumn(name: "ProducedEventsJson", table: "ctr_background_service_draft_metadata");
    }
}
