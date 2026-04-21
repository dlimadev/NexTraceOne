using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <summary>
    /// Migração que adiciona a tabela ctr_graphql_schema_snapshots para armazenar snapshots
    /// analisados de schemas GraphQL. Permite diff semântico e auditoria de breaking changes.
    /// Wave G.3 — GraphQL Schema Analysis (GAP-CTR-01).
    /// </summary>
    public partial class G3_AddGraphQlSchemaSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ctr_graphql_schema_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    TypeCount = table.Column<int>(type: "integer", nullable: false),
                    FieldCount = table.Column<int>(type: "integer", nullable: false),
                    OperationCount = table.Column<int>(type: "integer", nullable: false),
                    TypeNamesJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    OperationsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    FieldsByTypeJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    HasQueryType = table.Column<bool>(type: "boolean", nullable: false),
                    HasMutationType = table.Column<bool>(type: "boolean", nullable: false),
                    HasSubscriptionType = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ctr_graphql_schema_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_graphql_snapshots_api_tenant_captured",
                table: "ctr_graphql_schema_snapshots",
                columns: new[] { "ApiAssetId", "TenantId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_ctr_graphql_snapshots_tenant_captured",
                table: "ctr_graphql_schema_snapshots",
                columns: new[] { "TenantId", "CapturedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ctr_graphql_schema_snapshots");
        }
    }
}
