using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapContractScorecardContractVersionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContractVersionId",
                table: "ContractScorecards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ContractScorecards_ContractVersionId",
                table: "ContractScorecards",
                column: "ContractVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContractScorecards_ContractVersionId",
                table: "ContractScorecards");

            migrationBuilder.DropColumn(
                name: "ContractVersionId",
                table: "ContractScorecards");
        }
    }
}
