using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class B_ServiceInterfaceAndContractBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Fase 1: Novas colunas em cat_service_assets ──────────────────

            migrationBuilder.AddColumn<string>(
                name: "SubDomain",
                table: "cat_service_assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Capability",
                table: "cat_service_assets",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitRepository",
                table: "cat_service_assets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CiPipelineUrl",
                table: "cat_service_assets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InfrastructureProvider",
                table: "cat_service_assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HostingPlatform",
                table: "cat_service_assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RuntimeLanguage",
                table: "cat_service_assets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RuntimeVersion",
                table: "cat_service_assets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SloTarget",
                table: "cat_service_assets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DataClassification",
                table: "cat_service_assets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegulatoryScope",
                table: "cat_service_assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChangeFrequency",
                table: "cat_service_assets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductOwner",
                table: "cat_service_assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactChannel",
                table: "cat_service_assets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OnCallRotationId",
                table: "cat_service_assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_assets_SubDomain",
                table: "cat_service_assets",
                column: "SubDomain");

            // ── Fase 2: Tabela cat_service_interfaces ─────────────────────────

            migrationBuilder.CreateTable(
                name: "cat_service_interfaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    InterfaceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    ExposureScope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Internal"),
                    BasePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    TopicName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    WsdlNamespace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    GrpcServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    ScheduleCron = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    EnvironmentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    SloTarget = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    RequiresContract = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AuthScheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "None"),
                    RateLimitPolicy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    DocumentationUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    DeprecationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SunsetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecationNotice = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_service_interfaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_service_interfaces_cat_service_assets_ServiceAssetId",
                        column: x => x.ServiceAssetId,
                        principalTable: "cat_service_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_interfaces_ServiceAssetId",
                table: "cat_service_interfaces",
                column: "ServiceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_interfaces_Status",
                table: "cat_service_interfaces",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_interfaces_InterfaceType",
                table: "cat_service_interfaces",
                column: "InterfaceType");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_interfaces_ServiceAssetId_Status_InterfaceType",
                table: "cat_service_interfaces",
                columns: new[] { "ServiceAssetId", "Status", "InterfaceType" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_interfaces_IsDeleted",
                table: "cat_service_interfaces",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            // ── Fase 3: Tabela cat_contract_bindings ──────────────────────────

            migrationBuilder.CreateTable(
                name: "cat_contract_bindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceInterfaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    BindingEnvironment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    IsDefaultVersion = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActivatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeactivatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MigrationNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_bindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_contract_bindings_cat_service_interfaces_ServiceInterfaceId",
                        column: x => x.ServiceInterfaceId,
                        principalTable: "cat_service_interfaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_bindings_ServiceInterfaceId",
                table: "cat_contract_bindings",
                column: "ServiceInterfaceId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_bindings_Status",
                table: "cat_contract_bindings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_bindings_ServiceInterfaceId_Status",
                table: "cat_contract_bindings",
                columns: new[] { "ServiceInterfaceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_bindings_ContractVersionId",
                table: "cat_contract_bindings",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_bindings_IsDeleted",
                table: "cat_contract_bindings",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "cat_contract_bindings");
            migrationBuilder.DropTable(name: "cat_service_interfaces");

            migrationBuilder.DropIndex(
                name: "IX_cat_service_assets_SubDomain",
                table: "cat_service_assets");

            migrationBuilder.DropColumn(name: "SubDomain", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "Capability", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "GitRepository", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "CiPipelineUrl", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "InfrastructureProvider", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "HostingPlatform", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "RuntimeLanguage", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "RuntimeVersion", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "SloTarget", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "DataClassification", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "RegulatoryScope", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "ChangeFrequency", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "ProductOwner", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "ContactChannel", table: "cat_service_assets");
            migrationBuilder.DropColumn(name: "OnCallRotationId", table: "cat_service_assets");
        }
    }
}
