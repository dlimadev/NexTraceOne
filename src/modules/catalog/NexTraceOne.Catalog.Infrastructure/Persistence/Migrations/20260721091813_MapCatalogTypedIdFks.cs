using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapCatalogTypedIdFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackageDependencies_ServiceDependencyProfiles_ServiceDepend~",
                table: "PackageDependencies");

            migrationBuilder.AddColumn<Guid>(
                name: "SystemId",
                table: "ZosConnectBindings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ContractDraftId",
                table: "SoapDraftMetadata",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ContractVersionId",
                table: "SoapContractDetails",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceAssetId",
                table: "ServiceInterfaces",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DraftId",
                table: "Reviews",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceDependencyProfileId",
                table: "PackageDependencies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SystemId",
                table: "MqMessageContracts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ListingId",
                table: "MarketplaceReviews",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SystemId",
                table: "ImsTransactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceAssetId",
                table: "FrameworkAssetDetails",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ContractDraftId",
                table: "EventDraftMetadata",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ContractVersionId",
                table: "EventContractDetails",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SystemId",
                table: "Db2Artifacts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CopybookId",
                table: "CopybookVersions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SystemId",
                table: "Copybooks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CopybookId",
                table: "CopybookProgramUsages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProgramId",
                table: "CopybookProgramUsages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CopybookId",
                table: "CopybookFields",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BaseVersionId",
                table: "CopybookDiffs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CopybookId",
                table: "CopybookDiffs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TargetVersionId",
                table: "CopybookDiffs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CopybookId",
                table: "CopybookContractMappings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ContractVersionId",
                table: "ContractEvidencePacks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ContractVersionId",
                table: "ContractDeployments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceInterfaceId",
                table: "ContractBindings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConsumerAssetId",
                table: "ConsumerRelationships",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SystemId",
                table: "CobolPrograms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SystemId",
                table: "CicsTransactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CanonicalEntityId",
                table: "CanonicalEntityVersions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ContractDraftId",
                table: "BackgroundServiceDraftMetadata",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceAssetId",
                table: "AssetDeploymentStates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ZosConnectBindings_SystemId",
                table: "ZosConnectBindings",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_SoapDraftMetadata_ContractDraftId",
                table: "SoapDraftMetadata",
                column: "ContractDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_SoapContractDetails_ContractVersionId",
                table: "SoapContractDetails",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInterfaces_ServiceAssetId",
                table: "ServiceInterfaces",
                column: "ServiceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_DraftId",
                table: "Reviews",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_MqMessageContracts_SystemId",
                table: "MqMessageContracts",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceReviews_ListingId",
                table: "MarketplaceReviews",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ImsTransactions_SystemId",
                table: "ImsTransactions",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkAssetDetails_ServiceAssetId",
                table: "FrameworkAssetDetails",
                column: "ServiceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_EventDraftMetadata_ContractDraftId",
                table: "EventDraftMetadata",
                column: "ContractDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_EventContractDetails_ContractVersionId",
                table: "EventContractDetails",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Db2Artifacts_SystemId",
                table: "Db2Artifacts",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_CopybookVersions_CopybookId",
                table: "CopybookVersions",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_Copybooks_SystemId",
                table: "Copybooks",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_CopybookProgramUsages_CopybookId",
                table: "CopybookProgramUsages",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_CopybookProgramUsages_ProgramId",
                table: "CopybookProgramUsages",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_CopybookFields_CopybookId",
                table: "CopybookFields",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_CopybookDiffs_BaseVersionId",
                table: "CopybookDiffs",
                column: "BaseVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CopybookDiffs_CopybookId",
                table: "CopybookDiffs",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_CopybookDiffs_TargetVersionId",
                table: "CopybookDiffs",
                column: "TargetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CopybookContractMappings_CopybookId",
                table: "CopybookContractMappings",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractEvidencePacks_ContractVersionId",
                table: "ContractEvidencePacks",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractDeployments_ContractVersionId",
                table: "ContractDeployments",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractBindings_ServiceInterfaceId",
                table: "ContractBindings",
                column: "ServiceInterfaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerRelationships_ConsumerAssetId",
                table: "ConsumerRelationships",
                column: "ConsumerAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CobolPrograms_SystemId",
                table: "CobolPrograms",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_CicsTransactions_SystemId",
                table: "CicsTransactions",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalEntityVersions_CanonicalEntityId",
                table: "CanonicalEntityVersions",
                column: "CanonicalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundServiceDraftMetadata_ContractDraftId",
                table: "BackgroundServiceDraftMetadata",
                column: "ContractDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDeploymentStates_ServiceAssetId",
                table: "AssetDeploymentStates",
                column: "ServiceAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_PackageDependencies_ServiceDependencyProfiles_ServiceDepend~",
                table: "PackageDependencies",
                column: "ServiceDependencyProfileId",
                principalTable: "ServiceDependencyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackageDependencies_ServiceDependencyProfiles_ServiceDepend~",
                table: "PackageDependencies");

            migrationBuilder.DropIndex(
                name: "IX_ZosConnectBindings_SystemId",
                table: "ZosConnectBindings");

            migrationBuilder.DropIndex(
                name: "IX_SoapDraftMetadata_ContractDraftId",
                table: "SoapDraftMetadata");

            migrationBuilder.DropIndex(
                name: "IX_SoapContractDetails_ContractVersionId",
                table: "SoapContractDetails");

            migrationBuilder.DropIndex(
                name: "IX_ServiceInterfaces_ServiceAssetId",
                table: "ServiceInterfaces");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_DraftId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_MqMessageContracts_SystemId",
                table: "MqMessageContracts");

            migrationBuilder.DropIndex(
                name: "IX_MarketplaceReviews_ListingId",
                table: "MarketplaceReviews");

            migrationBuilder.DropIndex(
                name: "IX_ImsTransactions_SystemId",
                table: "ImsTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FrameworkAssetDetails_ServiceAssetId",
                table: "FrameworkAssetDetails");

            migrationBuilder.DropIndex(
                name: "IX_EventDraftMetadata_ContractDraftId",
                table: "EventDraftMetadata");

            migrationBuilder.DropIndex(
                name: "IX_EventContractDetails_ContractVersionId",
                table: "EventContractDetails");

            migrationBuilder.DropIndex(
                name: "IX_Db2Artifacts_SystemId",
                table: "Db2Artifacts");

            migrationBuilder.DropIndex(
                name: "IX_CopybookVersions_CopybookId",
                table: "CopybookVersions");

            migrationBuilder.DropIndex(
                name: "IX_Copybooks_SystemId",
                table: "Copybooks");

            migrationBuilder.DropIndex(
                name: "IX_CopybookProgramUsages_CopybookId",
                table: "CopybookProgramUsages");

            migrationBuilder.DropIndex(
                name: "IX_CopybookProgramUsages_ProgramId",
                table: "CopybookProgramUsages");

            migrationBuilder.DropIndex(
                name: "IX_CopybookFields_CopybookId",
                table: "CopybookFields");

            migrationBuilder.DropIndex(
                name: "IX_CopybookDiffs_BaseVersionId",
                table: "CopybookDiffs");

            migrationBuilder.DropIndex(
                name: "IX_CopybookDiffs_CopybookId",
                table: "CopybookDiffs");

            migrationBuilder.DropIndex(
                name: "IX_CopybookDiffs_TargetVersionId",
                table: "CopybookDiffs");

            migrationBuilder.DropIndex(
                name: "IX_CopybookContractMappings_CopybookId",
                table: "CopybookContractMappings");

            migrationBuilder.DropIndex(
                name: "IX_ContractEvidencePacks_ContractVersionId",
                table: "ContractEvidencePacks");

            migrationBuilder.DropIndex(
                name: "IX_ContractDeployments_ContractVersionId",
                table: "ContractDeployments");

            migrationBuilder.DropIndex(
                name: "IX_ContractBindings_ServiceInterfaceId",
                table: "ContractBindings");

            migrationBuilder.DropIndex(
                name: "IX_ConsumerRelationships_ConsumerAssetId",
                table: "ConsumerRelationships");

            migrationBuilder.DropIndex(
                name: "IX_CobolPrograms_SystemId",
                table: "CobolPrograms");

            migrationBuilder.DropIndex(
                name: "IX_CicsTransactions_SystemId",
                table: "CicsTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CanonicalEntityVersions_CanonicalEntityId",
                table: "CanonicalEntityVersions");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundServiceDraftMetadata_ContractDraftId",
                table: "BackgroundServiceDraftMetadata");

            migrationBuilder.DropIndex(
                name: "IX_AssetDeploymentStates_ServiceAssetId",
                table: "AssetDeploymentStates");

            migrationBuilder.DropColumn(
                name: "SystemId",
                table: "ZosConnectBindings");

            migrationBuilder.DropColumn(
                name: "ContractDraftId",
                table: "SoapDraftMetadata");

            migrationBuilder.DropColumn(
                name: "ContractVersionId",
                table: "SoapContractDetails");

            migrationBuilder.DropColumn(
                name: "ServiceAssetId",
                table: "ServiceInterfaces");

            migrationBuilder.DropColumn(
                name: "DraftId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SystemId",
                table: "MqMessageContracts");

            migrationBuilder.DropColumn(
                name: "ListingId",
                table: "MarketplaceReviews");

            migrationBuilder.DropColumn(
                name: "SystemId",
                table: "ImsTransactions");

            migrationBuilder.DropColumn(
                name: "ServiceAssetId",
                table: "FrameworkAssetDetails");

            migrationBuilder.DropColumn(
                name: "ContractDraftId",
                table: "EventDraftMetadata");

            migrationBuilder.DropColumn(
                name: "ContractVersionId",
                table: "EventContractDetails");

            migrationBuilder.DropColumn(
                name: "SystemId",
                table: "Db2Artifacts");

            migrationBuilder.DropColumn(
                name: "CopybookId",
                table: "CopybookVersions");

            migrationBuilder.DropColumn(
                name: "SystemId",
                table: "Copybooks");

            migrationBuilder.DropColumn(
                name: "CopybookId",
                table: "CopybookProgramUsages");

            migrationBuilder.DropColumn(
                name: "ProgramId",
                table: "CopybookProgramUsages");

            migrationBuilder.DropColumn(
                name: "CopybookId",
                table: "CopybookFields");

            migrationBuilder.DropColumn(
                name: "BaseVersionId",
                table: "CopybookDiffs");

            migrationBuilder.DropColumn(
                name: "CopybookId",
                table: "CopybookDiffs");

            migrationBuilder.DropColumn(
                name: "TargetVersionId",
                table: "CopybookDiffs");

            migrationBuilder.DropColumn(
                name: "CopybookId",
                table: "CopybookContractMappings");

            migrationBuilder.DropColumn(
                name: "ContractVersionId",
                table: "ContractEvidencePacks");

            migrationBuilder.DropColumn(
                name: "ContractVersionId",
                table: "ContractDeployments");

            migrationBuilder.DropColumn(
                name: "ServiceInterfaceId",
                table: "ContractBindings");

            migrationBuilder.DropColumn(
                name: "ConsumerAssetId",
                table: "ConsumerRelationships");

            migrationBuilder.DropColumn(
                name: "SystemId",
                table: "CobolPrograms");

            migrationBuilder.DropColumn(
                name: "SystemId",
                table: "CicsTransactions");

            migrationBuilder.DropColumn(
                name: "CanonicalEntityId",
                table: "CanonicalEntityVersions");

            migrationBuilder.DropColumn(
                name: "ContractDraftId",
                table: "BackgroundServiceDraftMetadata");

            migrationBuilder.DropColumn(
                name: "ServiceAssetId",
                table: "AssetDeploymentStates");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceDependencyProfileId",
                table: "PackageDependencies",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_PackageDependencies_ServiceDependencyProfiles_ServiceDepend~",
                table: "PackageDependencies",
                column: "ServiceDependencyProfileId",
                principalTable: "ServiceDependencyProfiles",
                principalColumn: "Id");
        }
    }
}
