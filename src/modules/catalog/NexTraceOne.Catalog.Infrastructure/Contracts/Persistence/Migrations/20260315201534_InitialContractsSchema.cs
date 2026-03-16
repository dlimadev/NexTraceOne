using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialContractsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ct_contract_drafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "OpenApi"),
                    SpecContent = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ProposedVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Editing"),
                    Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseContractVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AiGenerationPrompt = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    LastEditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastEditedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_contract_drafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ct_contract_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_contract_reviews", x => x.Id);
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
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "OpenApi"),
                    LifecycleState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    ImportedFrom = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignatureFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SignatureAlgorithm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SignedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProvenanceOrigin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProvenanceOriginalFormat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProvenanceParserUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProvenanceStandardVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProvenanceImportedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProvenanceIsAiGenerated = table.Column<bool>(type: "boolean", nullable: true),
                    ProvenanceAiModelVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeprecationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SunsetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecationNotice = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS outbox_messages (
                    ""Id"" uuid NOT NULL,
                    ""EventType"" character varying(1000) NOT NULL,
                    ""Payload"" text NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""ProcessedAt"" timestamp with time zone,
                    ""RetryCount"" integer NOT NULL,
                    ""LastError"" character varying(4000),
                    ""TenantId"" uuid NOT NULL,
                    CONSTRAINT ""PK_outbox_messages"" PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.CreateTable(
                name: "ct_contract_examples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExampleType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_contract_examples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ct_contract_examples_ct_contract_drafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "ct_contract_drafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ct_contract_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_contract_artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ct_contract_artifacts_ct_contract_versions_ContractVersionId",
                        column: x => x.ContractVersionId,
                        principalTable: "ct_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "OpenApi"),
                    ChangeLevel = table.Column<int>(type: "integer", nullable: false),
                    BreakingChanges = table.Column<string>(type: "text", nullable: false),
                    NonBreakingChanges = table.Column<string>(type: "text", nullable: false),
                    AdditiveChanges = table.Column<string>(type: "text", nullable: false),
                    SuggestedSemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.8m),
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

            migrationBuilder.CreateTable(
                name: "ct_contract_rule_violations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RulesetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SuggestedFix = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_contract_rule_violations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ct_contract_rule_violations_ct_contract_versions_ContractVe~",
                        column: x => x.ContractVersionId,
                        principalTable: "ct_contract_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_artifacts_ArtifactType",
                table: "ct_contract_artifacts",
                column: "ArtifactType");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_artifacts_ContractVersionId",
                table: "ct_contract_artifacts",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_diffs_ContractVersionId",
                table: "ct_contract_diffs",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_drafts_Author",
                table: "ct_contract_drafts",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_drafts_Protocol",
                table: "ct_contract_drafts",
                column: "Protocol");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_drafts_ServiceId",
                table: "ct_contract_drafts",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_drafts_Status",
                table: "ct_contract_drafts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_examples_ContractVersionId",
                table: "ct_contract_examples",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_examples_DraftId",
                table: "ct_contract_examples",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_reviews_DraftId",
                table: "ct_contract_reviews",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_rule_violations_ContractVersionId",
                table: "ct_contract_rule_violations",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_rule_violations_RulesetId",
                table: "ct_contract_rule_violations",
                column: "RulesetId");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_versions_ApiAssetId_SemVer",
                table: "ct_contract_versions",
                columns: new[] { "ApiAssetId", "SemVer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_versions_LifecycleState",
                table: "ct_contract_versions",
                column: "LifecycleState");

            migrationBuilder.CreateIndex(
                name: "IX_ct_contract_versions_Protocol",
                table: "ct_contract_versions",
                column: "Protocol");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_outbox_messages_CreatedAt"" ON outbox_messages (""CreatedAt"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_outbox_messages_ProcessedAt"" ON outbox_messages (""ProcessedAt"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ct_contract_artifacts");

            migrationBuilder.DropTable(
                name: "ct_contract_diffs");

            migrationBuilder.DropTable(
                name: "ct_contract_examples");

            migrationBuilder.DropTable(
                name: "ct_contract_reviews");

            migrationBuilder.DropTable(
                name: "ct_contract_rule_violations");

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS outbox_messages;");

            migrationBuilder.DropTable(
                name: "ct_contract_drafts");

            migrationBuilder.DropTable(
                name: "ct_contract_versions");
        }
    }
}
