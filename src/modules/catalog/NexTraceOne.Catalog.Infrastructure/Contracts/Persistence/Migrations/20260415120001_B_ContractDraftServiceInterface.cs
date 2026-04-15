using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <summary>
    /// Migração que adiciona a coluna ServiceInterfaceId (nullable) à tabela ctr_contract_drafts.
    /// Permite vincular um draft de contrato a uma interface de serviço específica.
    /// </summary>
    public partial class B_ContractDraftServiceInterface : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceInterfaceId",
                table: "ctr_contract_drafts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_ServiceInterfaceId",
                table: "ctr_contract_drafts",
                column: "ServiceInterfaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ctr_contract_drafts_ServiceInterfaceId",
                table: "ctr_contract_drafts");

            migrationBuilder.DropColumn(
                name: "ServiceInterfaceId",
                table: "ctr_contract_drafts");
        }
    }
}
