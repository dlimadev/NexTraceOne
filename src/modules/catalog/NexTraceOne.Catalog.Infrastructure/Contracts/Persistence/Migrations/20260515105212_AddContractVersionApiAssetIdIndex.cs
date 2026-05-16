using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractVersionApiAssetIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_ApiAssetId",
                table: "ctr_contract_versions",
                column: "ApiAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ctr_contract_versions_ApiAssetId",
                table: "ctr_contract_versions");
        }
    }
}
