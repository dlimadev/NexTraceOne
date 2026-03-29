using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class W04_LegacyContractGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_copybook_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CopybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: false),
                    FieldCount = table.Column<int>(type: "integer", nullable: false),
                    TotalLength = table.Column<int>(type: "integer", nullable: false),
                    RecordFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_copybook_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_copybook_versions_cat_copybooks_CopybookId",
                        column: x => x.CopybookId,
                        principalTable: "cat_copybooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cat_copybook_diffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CopybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeLevel = table.Column<int>(type: "integer", nullable: false),
                    BreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    AdditiveChangeCount = table.Column<int>(type: "integer", nullable: false),
                    NonBreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    ChangesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_copybook_diffs", x => x.Id);
                    table.CheckConstraint("CK_cat_copybook_diffs_change_level", "\"ChangeLevel\" >= 0 AND \"ChangeLevel\" <= 4");
                    table.ForeignKey(
                        name: "FK_cat_copybook_diffs_cat_copybooks_CopybookId",
                        column: x => x.CopybookId,
                        principalTable: "cat_copybooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cat_copybook_diffs_cat_copybook_versions_BaseVersionId",
                        column: x => x.BaseVersionId,
                        principalTable: "cat_copybook_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cat_copybook_diffs_cat_copybook_versions_TargetVersionId",
                        column: x => x.TargetVersionId,
                        principalTable: "cat_copybook_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_mq_contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QueueName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MessageFormat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadSchema = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CopybookReference = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxMessageLength = table.Column<int>(type: "integer", nullable: true),
                    HeaderFormat = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EncodingScheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_mq_contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_mq_contracts_cat_mainframe_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "cat_mainframe_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_copybook_contract_mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CopybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MappingType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_copybook_contract_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_copybook_contract_mappings_cat_copybooks_CopybookId",
                        column: x => x.CopybookId,
                        principalTable: "cat_copybooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Índices ───────────────────────────────────────────────────────

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_versions_CopybookId",
                table: "cat_copybook_versions",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_versions_CopybookId_VersionLabel",
                table: "cat_copybook_versions",
                columns: new[] { "CopybookId", "VersionLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_diffs_CopybookId",
                table: "cat_copybook_diffs",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_diffs_BaseVersionId_TargetVersionId",
                table: "cat_copybook_diffs",
                columns: new[] { "BaseVersionId", "TargetVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_mq_contracts_SystemId",
                table: "cat_mq_contracts",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_mq_contracts_QueueName_SystemId",
                table: "cat_mq_contracts",
                columns: new[] { "QueueName", "SystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_contract_mappings_CopybookId",
                table: "cat_copybook_contract_mappings",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_contract_mappings_CopybookId_ContractVersionId",
                table: "cat_copybook_contract_mappings",
                columns: new[] { "CopybookId", "ContractVersionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_copybook_contract_mappings");

            migrationBuilder.DropTable(
                name: "cat_copybook_diffs");

            migrationBuilder.DropTable(
                name: "cat_mq_contracts");

            migrationBuilder.DropTable(
                name: "cat_copybook_versions");
        }
    }
}
