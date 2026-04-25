using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <summary>
    /// Migration CC-03/04/06: Adiciona entidades de Data Contracts, Consumer Inventory e Breaking Change Proposals.
    ///   - ctr_data_contract_schemas (CC-03): schema de data contracts com classificação PII
    ///   - ctr_contract_consumer_inventory (CC-04): inventário de consumidores reais via OTel
    ///   - ctr_breaking_change_proposals (CC-06): workflow de propostas de breaking change
    /// </summary>
    public partial class CC03_CC04_CC06_AddDataContractFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── CC-03: Data Contract Schemas ──
            migrationBuilder.CreateTable(
                name: "ctr_data_contract_schemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SlaFreshnessHours = table.Column<int>(type: "integer", nullable: false),
                    SchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                    PiiClassification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColumnCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_data_contract_schemas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_data_contract_schemas_api_tenant_captured",
                table: "ctr_data_contract_schemas",
                columns: new[] { "tenant_id", "ApiAssetId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_data_contract_schemas_tenant_captured",
                table: "ctr_data_contract_schemas",
                columns: new[] { "tenant_id", "CapturedAt" });

            // ── CC-04: Contract Consumer Inventory ──
            migrationBuilder.CreateTable(
                name: "ctr_contract_consumer_inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerService = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConsumerEnvironment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FrequencyPerDay = table.Column<double>(type: "double precision", nullable: false),
                    LastCalledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FirstCalledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_consumer_inventory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_consumer_inventory_tenant_contract",
                table: "ctr_contract_consumer_inventory",
                columns: new[] { "tenant_id", "ContractId" });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_consumer_inventory_unique_consumer",
                table: "ctr_contract_consumer_inventory",
                columns: new[] { "tenant_id", "ContractId", "ConsumerService", "ConsumerEnvironment" },
                unique: true);

            // ── CC-06: Breaking Change Proposals ──
            migrationBuilder.CreateTable(
                name: "ctr_breaking_change_proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProposedBreakingChangesJson = table.Column<string>(type: "jsonb", nullable: false),
                    MigrationWindowDays = table.Column<int>(type: "integer", nullable: false),
                    DeprecationPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConsultationOpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecisionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_breaking_change_proposals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_breaking_change_proposals_tenant_id_ContractId",
                table: "ctr_breaking_change_proposals",
                columns: new[] { "tenant_id", "ContractId" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_breaking_change_proposals_tenant_id_Status",
                table: "ctr_breaking_change_proposals",
                columns: new[] { "tenant_id", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ctr_breaking_change_proposals");
            migrationBuilder.DropTable(name: "ctr_contract_consumer_inventory");
            migrationBuilder.DropTable(name: "ctr_data_contract_schemas");
        }
    }
}
