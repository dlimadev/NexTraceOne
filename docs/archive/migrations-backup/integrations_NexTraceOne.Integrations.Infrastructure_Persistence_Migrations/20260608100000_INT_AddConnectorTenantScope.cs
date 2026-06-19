using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class INT_AddConnectorTenantScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 1.3: TenantId e IsGlobal para int_connectors
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "int_connectors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGlobal",
                table: "int_connectors",
                type: "boolean",
                nullable: false,
                defaultValue: true);  // Retrocompatibilidade: conectores existentes são globais

            migrationBuilder.CreateIndex(
                name: "IX_int_connectors_TenantId",
                table: "int_connectors",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_int_connectors_TenantId",
                table: "int_connectors");

            migrationBuilder.DropColumn(name: "TenantId", table: "int_connectors");
            migrationBuilder.DropColumn(name: "IsGlobal", table: "int_connectors");
        }
    }
}
