using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase3To8ConfigurationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Phase 3: User Watch & Alert Rules ────────────────────────────

            migrationBuilder.CreateTable(
                name: "cfg_user_watches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NotifyLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_user_watches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_user_alert_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Condition = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_user_alert_rules", x => x.Id);
                });

            // ── Phase 4: Tags, Custom Fields & Taxonomy ─────────────────────

            migrationBuilder.CreateTable(
                name: "cfg_entity_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_entity_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_service_custom_fields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    FieldType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_service_custom_fields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_taxonomy_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_taxonomy_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_taxonomy_values",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_taxonomy_values", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cfg_taxonomy_values_cfg_taxonomy_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "cfg_taxonomy_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Phase 5: Automation, Checklists & Contract Templates ────────

            migrationBuilder.CreateTable(
                name: "cfg_automation_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Trigger = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConditionsJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ActionsJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RuleCreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_automation_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_change_checklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChangeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Items = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_change_checklists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_contract_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContractType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TemplateJson = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TemplateCreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_contract_templates", x => x.Id);
                });

            // ── Phase 6: Scheduled Reports ──────────────────────────────────

            migrationBuilder.CreateTable(
                name: "cfg_scheduled_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReportType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FiltersJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Schedule = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecipientsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_scheduled_reports", x => x.Id);
                });

            // ── Phase 7: Saved Prompts ──────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "cfg_saved_prompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PromptText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ContextType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TagsCsv = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_saved_prompts", x => x.Id);
                });

            // ── Phase 8: Webhook Templates ──────────────────────────────────

            migrationBuilder.CreateTable(
                name: "cfg_webhook_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayloadTemplate = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    HeadersJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_webhook_templates", x => x.Id);
                });

            // ── Indexes ─────────────────────────────────────────────────────

            // Phase 3 indexes
            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_watches_UserId_TenantId_EntityType_EntityId",
                table: "cfg_user_watches",
                columns: new[] { "UserId", "TenantId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_alert_rules_UserId_TenantId",
                table: "cfg_user_alert_rules",
                columns: new[] { "UserId", "TenantId" });

            // Phase 4 indexes
            migrationBuilder.CreateIndex(
                name: "IX_cfg_entity_tags_TenantId_EntityType_EntityId_Key",
                table: "cfg_entity_tags",
                columns: new[] { "TenantId", "EntityType", "EntityId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_entity_tags_TenantId_Key",
                table: "cfg_entity_tags",
                columns: new[] { "TenantId", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_service_custom_fields_TenantId",
                table: "cfg_service_custom_fields",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_taxonomy_categories_TenantId_Name",
                table: "cfg_taxonomy_categories",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_taxonomy_values_CategoryId_TenantId",
                table: "cfg_taxonomy_values",
                columns: new[] { "CategoryId", "TenantId" });

            // Phase 5 indexes
            migrationBuilder.CreateIndex(
                name: "IX_cfg_automation_rules_TenantId_Trigger",
                table: "cfg_automation_rules",
                columns: new[] { "TenantId", "Trigger" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_change_checklists_TenantId_ChangeType",
                table: "cfg_change_checklists",
                columns: new[] { "TenantId", "ChangeType" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_contract_templates_TenantId_ContractType",
                table: "cfg_contract_templates",
                columns: new[] { "TenantId", "ContractType" });

            // Phase 6 indexes
            migrationBuilder.CreateIndex(
                name: "IX_cfg_scheduled_reports_TenantId_UserId",
                table: "cfg_scheduled_reports",
                columns: new[] { "TenantId", "UserId" });

            // Phase 7 indexes
            migrationBuilder.CreateIndex(
                name: "IX_cfg_saved_prompts_UserId_TenantId",
                table: "cfg_saved_prompts",
                columns: new[] { "UserId", "TenantId" });

            // Phase 8 indexes
            migrationBuilder.CreateIndex(
                name: "IX_cfg_webhook_templates_TenantId_EventType",
                table: "cfg_webhook_templates",
                columns: new[] { "TenantId", "EventType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "cfg_webhook_templates");
            migrationBuilder.DropTable(name: "cfg_saved_prompts");
            migrationBuilder.DropTable(name: "cfg_scheduled_reports");
            migrationBuilder.DropTable(name: "cfg_contract_templates");
            migrationBuilder.DropTable(name: "cfg_change_checklists");
            migrationBuilder.DropTable(name: "cfg_automation_rules");
            migrationBuilder.DropTable(name: "cfg_taxonomy_values");
            migrationBuilder.DropTable(name: "cfg_taxonomy_categories");
            migrationBuilder.DropTable(name: "cfg_service_custom_fields");
            migrationBuilder.DropTable(name: "cfg_entity_tags");
            migrationBuilder.DropTable(name: "cfg_user_alert_rules");
            migrationBuilder.DropTable(name: "cfg_user_watches");
        }
    }
}
