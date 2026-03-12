using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Contracts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialContractsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ct_contract_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SpecContent = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ImportedFrom = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_contract_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ct_contract_diffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeLevel = table.Column<int>(type: "integer", nullable: false),
                    BreakingChanges = table.Column<string>(type: "text", nullable: false),
                    NonBreakingChanges = table.Column<string>(type: "text", nullable: false),
                    AdditiveChanges = table.Column<string>(type: "text", nullable: false),
                    SuggestedSemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_contract_diffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ct_contract_diffs_ct_contract_versions_ContractVersionId",
                        column: x => x.ContractVersionId,
                        principalTable: "ct_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_versions_ApiAssetId_SemVer",
                table: "ct_contract_versions",
                columns: new[] { "ApiAssetId", "SemVer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_diffs_ContractVersionId",
                table: "ct_contract_diffs",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_CreatedAt",
                table: "outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                table: "outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ct_contract_diffs");

            migrationBuilder.DropTable(
                name: "ct_contract_versions");

            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
