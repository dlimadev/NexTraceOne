using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixConstraintQuoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ctr_spectral_rulesets_IsActive",
                table: "ctr_spectral_rulesets");

            migrationBuilder.DropIndex(
                name: "IX_ctr_spectral_rulesets_IsDeleted",
                table: "ctr_spectral_rulesets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_spectral_rulesets_origin",
                table: "ctr_spectral_rulesets");

            migrationBuilder.DropIndex(
                name: "IX_ctr_contract_versions_IsDeleted",
                table: "ctr_contract_versions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_contract_versions_lifecycle_state",
                table: "ctr_contract_versions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_contract_versions_protocol",
                table: "ctr_contract_versions");

            migrationBuilder.DropIndex(
                name: "IX_ctr_contract_drafts_IsDeleted",
                table: "ctr_contract_drafts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_contract_drafts_status",
                table: "ctr_contract_drafts");

            migrationBuilder.DropIndex(
                name: "IX_ctr_canonical_entities_IsDeleted",
                table: "ctr_canonical_entities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_canonical_entities_state",
                table: "ctr_canonical_entities");

            migrationBuilder.CreateTable(
                name: "ctr_background_service_contract_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScheduleExpression = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TriggerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InputsJson = table.Column<string>(type: "text", nullable: false),
                    OutputsJson = table.Column<string>(type: "text", nullable: false),
                    SideEffectsJson = table.Column<string>(type: "text", nullable: false),
                    TimeoutExpression = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AllowsConcurrency = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_background_service_contract_details", x => x.Id);
                    table.CheckConstraint("CK_ctr_bg_service_details_trigger_type", "\"TriggerType\" IN ('Cron', 'Interval', 'EventTriggered', 'OnDemand', 'Continuous')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_background_service_draft_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduleExpression = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InputsJson = table.Column<string>(type: "text", nullable: false),
                    OutputsJson = table.Column<string>(type: "text", nullable: false),
                    SideEffectsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_background_service_draft_metadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_event_contract_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AsyncApiVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DefaultContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChannelsJson = table.Column<string>(type: "text", nullable: false),
                    MessagesJson = table.Column<string>(type: "text", nullable: false),
                    ServersJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_event_contract_details", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_event_draft_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AsyncApiVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DefaultContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChannelsJson = table.Column<string>(type: "text", nullable: false),
                    MessagesJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_event_draft_metadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_soap_contract_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetNamespace = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SoapVersion = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    EndpointUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WsdlSourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PortTypeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BindingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExtractedOperationsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_soap_contract_details", x => x.Id);
                    table.CheckConstraint("CK_ctr_soap_contract_details_soap_version", "\"SoapVersion\" IN ('1.1', '1.2')");
                });

            migrationBuilder.CreateTable(
                name: "ctr_soap_draft_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetNamespace = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SoapVersion = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    EndpointUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PortTypeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BindingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OperationsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_soap_draft_metadata", x => x.Id);
                    table.CheckConstraint("CK_ctr_soap_draft_metadata_soap_version", "\"SoapVersion\" IN ('1.1', '1.2')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_IsActive",
                table: "ctr_spectral_rulesets",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_IsDeleted",
                table: "ctr_spectral_rulesets",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_spectral_rulesets_origin",
                table: "ctr_spectral_rulesets",
                sql: "\"Origin\" IN ('Platform', 'Organization', 'Team', 'Imported')");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_IsDeleted",
                table: "ctr_contract_versions",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_contract_versions_lifecycle_state",
                table: "ctr_contract_versions",
                sql: "\"LifecycleState\" IN ('Draft', 'InReview', 'Approved', 'Locked', 'Deprecated', 'Sunset', 'Retired')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_contract_versions_protocol",
                table: "ctr_contract_versions",
                sql: "\"Protocol\" IN ('OpenApi', 'Swagger', 'Wsdl', 'AsyncApi', 'Protobuf', 'GraphQL')");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_IsDeleted",
                table: "ctr_contract_drafts",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_contract_drafts_status",
                table: "ctr_contract_drafts",
                sql: "\"Status\" IN ('Editing', 'InReview', 'Approved', 'Rejected', 'Published')");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_IsDeleted",
                table: "ctr_canonical_entities",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_canonical_entities_state",
                table: "ctr_canonical_entities",
                sql: "\"State\" IN ('Draft', 'Published', 'Deprecated', 'Retired')");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_background_service_contract_details_ContractVersionId",
                table: "ctr_background_service_contract_details",
                column: "ContractVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_background_service_contract_details_IsDeleted",
                table: "ctr_background_service_contract_details",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_background_service_draft_metadata_ContractDraftId",
                table: "ctr_background_service_draft_metadata",
                column: "ContractDraftId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_event_contract_details_ContractVersionId",
                table: "ctr_event_contract_details",
                column: "ContractVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_event_contract_details_IsDeleted",
                table: "ctr_event_contract_details",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_event_draft_metadata_ContractDraftId",
                table: "ctr_event_draft_metadata",
                column: "ContractDraftId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_soap_contract_details_ContractVersionId",
                table: "ctr_soap_contract_details",
                column: "ContractVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_soap_contract_details_IsDeleted",
                table: "ctr_soap_contract_details",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_soap_draft_metadata_ContractDraftId",
                table: "ctr_soap_draft_metadata",
                column: "ContractDraftId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ctr_background_service_contract_details");

            migrationBuilder.DropTable(
                name: "ctr_background_service_draft_metadata");

            migrationBuilder.DropTable(
                name: "ctr_event_contract_details");

            migrationBuilder.DropTable(
                name: "ctr_event_draft_metadata");

            migrationBuilder.DropTable(
                name: "ctr_soap_contract_details");

            migrationBuilder.DropTable(
                name: "ctr_soap_draft_metadata");

            migrationBuilder.DropIndex(
                name: "IX_ctr_spectral_rulesets_IsActive",
                table: "ctr_spectral_rulesets");

            migrationBuilder.DropIndex(
                name: "IX_ctr_spectral_rulesets_IsDeleted",
                table: "ctr_spectral_rulesets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_spectral_rulesets_origin",
                table: "ctr_spectral_rulesets");

            migrationBuilder.DropIndex(
                name: "IX_ctr_contract_versions_IsDeleted",
                table: "ctr_contract_versions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_contract_versions_lifecycle_state",
                table: "ctr_contract_versions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_contract_versions_protocol",
                table: "ctr_contract_versions");

            migrationBuilder.DropIndex(
                name: "IX_ctr_contract_drafts_IsDeleted",
                table: "ctr_contract_drafts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_contract_drafts_status",
                table: "ctr_contract_drafts");

            migrationBuilder.DropIndex(
                name: "IX_ctr_canonical_entities_IsDeleted",
                table: "ctr_canonical_entities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_canonical_entities_state",
                table: "ctr_canonical_entities");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_IsActive",
                table: "ctr_spectral_rulesets",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_spectral_rulesets_IsDeleted",
                table: "ctr_spectral_rulesets",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_spectral_rulesets_origin",
                table: "ctr_spectral_rulesets",
                sql: "\"Origin\" IN ('Platform', 'Organization', 'Team', 'Imported')");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_versions_IsDeleted",
                table: "ctr_contract_versions",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_contract_versions_lifecycle_state",
                table: "ctr_contract_versions",
                sql: "\"LifecycleState\" IN ('Draft', 'InReview', 'Approved', 'Locked', 'Deprecated', 'Sunset', 'Retired')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_contract_versions_protocol",
                table: "ctr_contract_versions",
                sql: "\"Protocol\" IN ('OpenApi', 'Swagger', 'Wsdl', 'AsyncApi', 'Protobuf', 'GraphQL')");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_contract_drafts_IsDeleted",
                table: "ctr_contract_drafts",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_contract_drafts_status",
                table: "ctr_contract_drafts",
                sql: "\"Status\" IN ('Editing', 'InReview', 'Approved', 'Rejected', 'Published')");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entities_IsDeleted",
                table: "ctr_canonical_entities",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_canonical_entities_state",
                table: "ctr_canonical_entities",
                sql: "\"State\" IN ('Draft', 'Published', 'Deprecated', 'Retired')");
        }
    }
}
