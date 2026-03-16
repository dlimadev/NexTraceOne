using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialRulesetGovernanceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "rg_lint_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RulesetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalFindings = table.Column<int>(type: "integer", nullable: false),
                    Findings = table.Column<string>(type: "text", nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rg_lint_results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rg_ruleset_bindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RulesetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BindingCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rg_ruleset_bindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rg_rulesets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    RulesetType = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RulesetCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rg_rulesets", x => x.Id);
                });

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_outbox_messages_CreatedAt"" ON outbox_messages (""CreatedAt"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_outbox_messages_ProcessedAt"" ON outbox_messages (""ProcessedAt"");");

            migrationBuilder.CreateIndex(
                name: "IX_rg_lint_results_ReleaseId",
                table: "rg_lint_results",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_rg_ruleset_bindings_RulesetId_AssetType",
                table: "rg_ruleset_bindings",
                columns: new[] { "RulesetId", "AssetType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS outbox_messages;");

            migrationBuilder.DropTable(
                name: "rg_lint_results");

            migrationBuilder.DropTable(
                name: "rg_ruleset_bindings");

            migrationBuilder.DropTable(
                name: "rg_rulesets");
        }
    }
}
