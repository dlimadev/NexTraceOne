using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_AddContractDeployments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ctr_contract_deployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    DeployedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeployedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_deployments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_deployments_ContractVersionId",
                table: "ctr_contract_deployments",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_deployments_ContractVersionId_Environment",
                table: "ctr_contract_deployments",
                columns: new[] { "ContractVersionId", "Environment" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_deployments_DeployedAt",
                table: "ctr_contract_deployments",
                column: "DeployedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ctr_contract_deployments");
        }
    }
}
