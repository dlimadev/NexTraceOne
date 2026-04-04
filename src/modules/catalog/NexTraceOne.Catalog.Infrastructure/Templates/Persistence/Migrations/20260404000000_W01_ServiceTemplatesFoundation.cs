using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Templates.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class W01_ServiceTemplatesFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tpl_service_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ServiceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    DefaultDomain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DefaultTeam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GovernancePolicyIds = table.Column<string>(type: "text", nullable: false),
                    BaseContractSpec = table.Column<string>(type: "text", nullable: true),
                    ScaffoldingManifestJson = table.Column<string>(type: "text", nullable: true),
                    RepositoryTemplateUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RepositoryTemplateBranch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tpl_service_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tpl_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    IsDeadLettered = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tpl_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tpl_service_templates_slug",
                table: "tpl_service_templates",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tpl_service_templates_tenant",
                table: "tpl_service_templates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tpl_service_templates_type_lang",
                table: "tpl_service_templates",
                columns: new[] { "ServiceType", "Language" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "tpl_outbox_messages");
            migrationBuilder.DropTable(name: "tpl_service_templates");
        }
    }
}
