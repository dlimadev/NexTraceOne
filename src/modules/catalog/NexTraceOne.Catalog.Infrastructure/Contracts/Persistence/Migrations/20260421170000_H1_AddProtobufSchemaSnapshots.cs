using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <summary>
    /// Migração que adiciona a tabela ctr_protobuf_schema_snapshots para armazenar snapshots
    /// analisados de schemas Protobuf (.proto). Permite diff semântico e auditoria de breaking changes
    /// em schemas gRPC/Protobuf (messages, fields, services, RPCs).
    /// Wave H.1 — Protobuf Schema Analysis (GAP-CTR-02).
    /// </summary>
    public partial class H1_AddProtobufSchemaSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ctr_protobuf_schema_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    FieldCount = table.Column<int>(type: "integer", nullable: false),
                    ServiceCount = table.Column<int>(type: "integer", nullable: false),
                    RpcCount = table.Column<int>(type: "integer", nullable: false),
                    MessageNamesJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    FieldsByMessageJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    RpcsByServiceJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    Syntax = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_protobuf_schema_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_protobuf_snapshots_api_tenant_captured",
                table: "ctr_protobuf_schema_snapshots",
                columns: new[] { "ApiAssetId", "TenantId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_protobuf_snapshots_tenant_captured",
                table: "ctr_protobuf_schema_snapshots",
                columns: new[] { "TenantId", "CapturedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ctr_protobuf_schema_snapshots");
        }
    }
}
