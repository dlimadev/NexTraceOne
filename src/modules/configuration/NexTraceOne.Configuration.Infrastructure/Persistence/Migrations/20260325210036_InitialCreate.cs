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
                name: "IX_cfg_definitions_Category",
                table: "cfg_definitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_definitions_Key",
                table: "cfg_definitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_definitions_SortOrder",
                table: "cfg_definitions",
                column: "SortOrder");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cfg_audit_entries");

            migrationBuilder.DropTable(
                name: "cfg_outbox_messages");

            migrationBuilder.DropTable(
                name: "cfg_entries");

            migrationBuilder.DropTable(
                name: "cfg_definitions");
        }
    }
}
