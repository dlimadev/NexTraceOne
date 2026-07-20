using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapTypedIdFksToShadowColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractArtifacts_ContractVersions_ContractVersionId1",
                table: "ContractArtifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractRuleViolations_ContractVersions_ContractVersionId1",
                table: "ContractRuleViolations");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceLinks_ServiceAssets_ServiceAssetId1",
                table: "ServiceLinks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceAssetId1",
                table: "ServiceLinks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ContractVersionId1",
                table: "ContractRuleViolations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ContractVersionId1",
                table: "ContractArtifacts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractArtifacts_ContractVersions_ContractVersionId1",
                table: "ContractArtifacts",
                column: "ContractVersionId1",
                principalTable: "ContractVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractRuleViolations_ContractVersions_ContractVersionId1",
                table: "ContractRuleViolations",
                column: "ContractVersionId1",
                principalTable: "ContractVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceLinks_ServiceAssets_ServiceAssetId1",
                table: "ServiceLinks",
                column: "ServiceAssetId1",
                principalTable: "ServiceAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractArtifacts_ContractVersions_ContractVersionId1",
                table: "ContractArtifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractRuleViolations_ContractVersions_ContractVersionId1",
                table: "ContractRuleViolations");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceLinks_ServiceAssets_ServiceAssetId1",
                table: "ServiceLinks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceAssetId1",
                table: "ServiceLinks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ContractVersionId1",
                table: "ContractRuleViolations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ContractVersionId1",
                table: "ContractArtifacts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractArtifacts_ContractVersions_ContractVersionId1",
                table: "ContractArtifacts",
                column: "ContractVersionId1",
                principalTable: "ContractVersions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractRuleViolations_ContractVersions_ContractVersionId1",
                table: "ContractRuleViolations",
                column: "ContractVersionId1",
                principalTable: "ContractVersions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceLinks_ServiceAssets_ServiceAssetId1",
                table: "ServiceLinks",
                column: "ServiceAssetId1",
                principalTable: "ServiceAssets",
                principalColumn: "Id");
        }
    }
}
