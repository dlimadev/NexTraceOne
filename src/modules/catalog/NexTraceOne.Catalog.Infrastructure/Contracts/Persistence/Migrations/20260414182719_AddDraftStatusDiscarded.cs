using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftStatusDiscarded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_contract_drafts_status",
                table: "ctr_contract_drafts");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_semantic_diff_results",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "cat_schema_evolution_advices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_impact_simulations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_contract_reviews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_contract_listings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_contract_compliance_results",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_contract_compliance_gates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ctr_contract_changelogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiAssetId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FromVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ToVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Entries = table.Column<string>(type: "jsonb", nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MarkdownContent = table.Column<string>(type: "text", nullable: true),
                    JsonContent = table.Column<string>(type: "text", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ApprovedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CommitSha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_changelogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_contract_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiAssetId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpecContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BreakingChangesCount = table.Column<int>(type: "integer", nullable: false),
                    NonBreakingChangesCount = table.Column<int>(type: "integer", nullable: false),
                    AdditiveChangesCount = table.Column<int>(type: "integer", nullable: false),
                    DiffDetails = table.Column<string>(type: "jsonb", nullable: false),
                    ComplianceViolations = table.Column<string>(type: "jsonb", nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceBranch = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CommitSha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PipelineId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EnvironmentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_contract_verifications", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_contract_drafts_status",
                table: "ctr_contract_drafts",
                sql: "\"Status\" IN ('Editing', 'InReview', 'Approved', 'Rejected', 'Published', 'Discarded')");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_changelogs_ApiAssetId",
                table: "ctr_contract_changelogs",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_changelogs_ApiAssetId_IsApproved",
                table: "ctr_contract_changelogs",
                columns: new[] { "ApiAssetId", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_changelogs_IsApproved",
                table: "ctr_contract_changelogs",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_changelogs_TenantId",
                table: "ctr_contract_changelogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_verifications_ApiAssetId",
                table: "ctr_contract_verifications",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_verifications_ApiAssetId_Status",
                table: "ctr_contract_verifications",
                columns: new[] { "ApiAssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_verifications_ServiceName",
                table: "ctr_contract_verifications",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_verifications_Status",
                table: "ctr_contract_verifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_verifications_TenantId",
                table: "ctr_contract_verifications",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ctr_contract_changelogs");

            migrationBuilder.DropTable(
                name: "ctr_contract_verifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_contract_drafts_status",
                table: "ctr_contract_drafts");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_semantic_diff_results",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "cat_schema_evolution_advices",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_impact_simulations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_contract_reviews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_contract_listings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_contract_compliance_results",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "cat_contract_compliance_gates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_contract_drafts_status",
                table: "ctr_contract_drafts",
                sql: "\"Status\" IN ('Editing', 'InReview', 'Approved', 'Rejected', 'Published')");
        }
    }
}
