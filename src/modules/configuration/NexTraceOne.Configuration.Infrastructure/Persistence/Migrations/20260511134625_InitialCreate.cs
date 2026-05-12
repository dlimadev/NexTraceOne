using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    RuleCreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                    Items = table.Column<string[]>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_change_checklists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_contract_compliance_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerificationApproach = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnBreakingChange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnNonBreakingChange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnNewEndpoint = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnRemovedEndpoint = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnMissingContract = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnContractNotApproved = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AutoGenerateChangelog = table.Column<bool>(type: "boolean", nullable: false),
                    ChangelogFormat = table.Column<int>(type: "integer", nullable: false),
                    RequireChangelogApproval = table.Column<bool>(type: "boolean", nullable: false),
                    EnforceCdct = table.Column<bool>(type: "boolean", nullable: false),
                    CdctFailureAction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnableRuntimeDriftDetection = table.Column<bool>(type: "boolean", nullable: false),
                    DriftDetectionIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    DriftThresholdForAlert = table.Column<decimal>(type: "numeric", nullable: false),
                    DriftThresholdForIncident = table.Column<decimal>(type: "numeric", nullable: false),
                    NotifyOnVerificationFailure = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnBreakingChange = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnDriftDetected = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationChannels = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_contract_compliance_policies", x => x.Id);
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
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_contract_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_entity_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_entity_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_outbox_messages", x => x.Id);
                });

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
                    LastSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_scheduled_reports", x => x.Id);
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
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_taxonomy_categories", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "cfg_user_bookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_user_bookmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_user_saved_views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Context = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FiltersJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_user_saved_views", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "cfg_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AllowedScopes = table.Column<string[]>(type: "text[]", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ValueType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEditable = table.Column<bool>(type: "boolean", nullable: false),
                    IsInheritable = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationRules = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    UiEditorType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeprecated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeprecatedMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_definitions", x => x.Id);
                    table.CheckConstraint("CK_cfg_definitions_category", "\"Category\" IN ('Bootstrap', 'SensitiveOperational', 'Functional')");
                    table.CheckConstraint("CK_cfg_definitions_value_type", "\"ValueType\" IN ('String', 'Integer', 'Decimal', 'Boolean', 'Json', 'StringList')");
                    table.ForeignKey(
                        name: "FK_cfg_definitions_cfg_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "cfg_modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cfg_feature_flag_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DefaultEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowedScopes = table.Column<string[]>(type: "text[]", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsEditable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_feature_flag_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cfg_feature_flag_definitions_cfg_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "cfg_modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cfg_taxonomy_values",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "cfg_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeReferenceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StructuredValueJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_entries", x => x.Id);
                    table.CheckConstraint("CK_cfg_entries_scope", "\"Scope\" IN ('System', 'Tenant', 'Environment', 'Role', 'Team', 'User')");
                    table.CheckConstraint("CK_cfg_entries_version_positive", "\"Version\" >= 1");
                    table.ForeignKey(
                        name: "FK_cfg_entries_cfg_definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "cfg_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cfg_feature_flag_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeReferenceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_feature_flag_entries", x => x.Id);
                    table.CheckConstraint("CK_cfg_feature_flag_entries_scope", "\"Scope\" IN ('System', 'Tenant', 'Environment', 'Role', 'Team', 'User')");
                    table.ForeignKey(
                        name: "FK_cfg_feature_flag_entries_cfg_feature_flag_definitions_Defin~",
                        column: x => x.DefinitionId,
                        principalTable: "cfg_feature_flag_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cfg_audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeReferenceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreviousValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PreviousVersion = table.Column<int>(type: "integer", nullable: true),
                    NewVersion = table.Column<int>(type: "integer", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_audit_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cfg_audit_entries_cfg_entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "cfg_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_audit_entries_ChangedAt",
                table: "cfg_audit_entries",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_audit_entries_ChangedBy",
                table: "cfg_audit_entries",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_audit_entries_EntryId",
                table: "cfg_audit_entries",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_audit_entries_Key",
                table: "cfg_audit_entries",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_automation_rules_TenantId_Trigger",
                table: "cfg_automation_rules",
                columns: new[] { "TenantId", "Trigger" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_change_checklists_TenantId_ChangeType",
                table: "cfg_change_checklists",
                columns: new[] { "TenantId", "ChangeType" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_contract_compliance_policies_IsActive",
                table: "cfg_contract_compliance_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_contract_compliance_policies_Scope",
                table: "cfg_contract_compliance_policies",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_contract_compliance_policies_TenantId",
                table: "cfg_contract_compliance_policies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_contract_compliance_policies_TenantId_Scope_IsActive",
                table: "cfg_contract_compliance_policies",
                columns: new[] { "TenantId", "Scope", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_contract_templates_TenantId_ContractType",
                table: "cfg_contract_templates",
                columns: new[] { "TenantId", "ContractType" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_definitions_Category",
                table: "cfg_definitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_definitions_Key",
                table: "cfg_definitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_definitions_ModuleId",
                table: "cfg_definitions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_definitions_SortOrder",
                table: "cfg_definitions",
                column: "SortOrder");

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
                name: "IX_cfg_entries_DefinitionId",
                table: "cfg_entries",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_entries_IsActive",
                table: "cfg_entries",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_entries_Key",
                table: "cfg_entries",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_entries_Key_Scope_ScopeReferenceId",
                table: "cfg_entries",
                columns: new[] { "Key", "Scope", "ScopeReferenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_entries_Scope",
                table: "cfg_entries",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_definitions_IsActive",
                table: "cfg_feature_flag_definitions",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_definitions_Key",
                table: "cfg_feature_flag_definitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_definitions_ModuleId",
                table: "cfg_feature_flag_definitions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_DefinitionId",
                table: "cfg_feature_flag_entries",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_IsActive",
                table: "cfg_feature_flag_entries",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_Key",
                table: "cfg_feature_flag_entries",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_Key_Scope_ScopeReferenceId",
                table: "cfg_feature_flag_entries",
                columns: new[] { "Key", "Scope", "ScopeReferenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_Scope",
                table: "cfg_feature_flag_entries",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_modules_IsActive",
                table: "cfg_modules",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_modules_Key",
                table: "cfg_modules",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_modules_SortOrder",
                table: "cfg_modules",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_outbox_messages_CreatedAt",
                table: "cfg_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_outbox_messages_IdempotencyKey",
                table: "cfg_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_outbox_messages_ProcessedAt",
                table: "cfg_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_saved_prompts_UserId_TenantId",
                table: "cfg_saved_prompts",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_scheduled_reports_TenantId_UserId",
                table: "cfg_scheduled_reports",
                columns: new[] { "TenantId", "UserId" });

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

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_alert_rules_UserId_TenantId",
                table: "cfg_user_alert_rules",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_bookmarks_UserId",
                table: "cfg_user_bookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_bookmarks_UserId_TenantId",
                table: "cfg_user_bookmarks",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_bookmarks_UserId_TenantId_EntityType_EntityId",
                table: "cfg_user_bookmarks",
                columns: new[] { "UserId", "TenantId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_saved_views_IsShared",
                table: "cfg_user_saved_views",
                column: "IsShared",
                filter: "\"IsShared\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_saved_views_UserId",
                table: "cfg_user_saved_views",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_saved_views_UserId_Context",
                table: "cfg_user_saved_views",
                columns: new[] { "UserId", "Context" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_saved_views_UserId_TenantId_Context_Name",
                table: "cfg_user_saved_views",
                columns: new[] { "UserId", "TenantId", "Context", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_watches_UserId_TenantId_EntityType_EntityId",
                table: "cfg_user_watches",
                columns: new[] { "UserId", "TenantId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_webhook_templates_TenantId_EventType",
                table: "cfg_webhook_templates",
                columns: new[] { "TenantId", "EventType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cfg_audit_entries");

            migrationBuilder.DropTable(
                name: "cfg_automation_rules");

            migrationBuilder.DropTable(
                name: "cfg_change_checklists");

            migrationBuilder.DropTable(
                name: "cfg_contract_compliance_policies");

            migrationBuilder.DropTable(
                name: "cfg_contract_templates");

            migrationBuilder.DropTable(
                name: "cfg_entity_tags");

            migrationBuilder.DropTable(
                name: "cfg_feature_flag_entries");

            migrationBuilder.DropTable(
                name: "cfg_outbox_messages");

            migrationBuilder.DropTable(
                name: "cfg_saved_prompts");

            migrationBuilder.DropTable(
                name: "cfg_scheduled_reports");

            migrationBuilder.DropTable(
                name: "cfg_service_custom_fields");

            migrationBuilder.DropTable(
                name: "cfg_taxonomy_values");

            migrationBuilder.DropTable(
                name: "cfg_user_alert_rules");

            migrationBuilder.DropTable(
                name: "cfg_user_bookmarks");

            migrationBuilder.DropTable(
                name: "cfg_user_saved_views");

            migrationBuilder.DropTable(
                name: "cfg_user_watches");

            migrationBuilder.DropTable(
                name: "cfg_webhook_templates");

            migrationBuilder.DropTable(
                name: "cfg_entries");

            migrationBuilder.DropTable(
                name: "cfg_feature_flag_definitions");

            migrationBuilder.DropTable(
                name: "cfg_taxonomy_categories");

            migrationBuilder.DropTable(
                name: "cfg_definitions");

            migrationBuilder.DropTable(
                name: "cfg_modules");
        }
    }
}
