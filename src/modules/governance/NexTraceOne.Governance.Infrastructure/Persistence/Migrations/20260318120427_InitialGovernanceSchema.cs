using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialGovernanceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gov_delegated_administrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GranteeUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GranteeDisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DomainId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_delegated_administrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_domains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CapabilityClassification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_domains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_pack_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rules = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultEnforcementMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_pack_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_packs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_packs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_rollout_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnforcementMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InitiatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InitiatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_rollout_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_team_domain_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnershipType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_team_domain_links", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ParentOrganizationUnit = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gov_waivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Justification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EvidenceLinks = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_waivers", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_DomainId",
                table: "gov_delegated_administrations",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_ExpiresAt",
                table: "gov_delegated_administrations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_GranteeUserId",
                table: "gov_delegated_administrations",
                column: "GranteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_IsActive",
                table: "gov_delegated_administrations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_Scope",
                table: "gov_delegated_administrations",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_delegated_administrations_TeamId",
                table: "gov_delegated_administrations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_domains_CapabilityClassification",
                table: "gov_domains",
                column: "CapabilityClassification");

            migrationBuilder.CreateIndex(
                name: "IX_gov_domains_Criticality",
                table: "gov_domains",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_gov_domains_Name",
                table: "gov_domains",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_pack_versions_PackId",
                table: "gov_pack_versions",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_pack_versions_PackId_Version",
                table: "gov_pack_versions",
                columns: new[] { "PackId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_pack_versions_PublishedAt",
                table: "gov_pack_versions",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_packs_Category",
                table: "gov_packs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_gov_packs_Name",
                table: "gov_packs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_packs_Status",
                table: "gov_packs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_CompletedAt",
                table: "gov_rollout_records",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_InitiatedAt",
                table: "gov_rollout_records",
                column: "InitiatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_PackId",
                table: "gov_rollout_records",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_Scope",
                table: "gov_rollout_records",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_Status",
                table: "gov_rollout_records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_rollout_records_VersionId",
                table: "gov_rollout_records",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_DomainId",
                table: "gov_team_domain_links",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_OwnershipType",
                table: "gov_team_domain_links",
                column: "OwnershipType");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_TeamId",
                table: "gov_team_domain_links",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_team_domain_links_TeamId_DomainId",
                table: "gov_team_domain_links",
                columns: new[] { "TeamId", "DomainId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_teams_Name",
                table: "gov_teams",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gov_teams_ParentOrganizationUnit",
                table: "gov_teams",
                column: "ParentOrganizationUnit");

            migrationBuilder.CreateIndex(
                name: "IX_gov_teams_Status",
                table: "gov_teams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_ExpiresAt",
                table: "gov_waivers",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_PackId",
                table: "gov_waivers",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_RequestedBy",
                table: "gov_waivers",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_Scope",
                table: "gov_waivers",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_gov_waivers_Status",
                table: "gov_waivers",
                column: "Status");

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
                name: "gov_delegated_administrations");

            migrationBuilder.DropTable(
                name: "gov_domains");

            migrationBuilder.DropTable(
                name: "gov_pack_versions");

            migrationBuilder.DropTable(
                name: "gov_packs");

            migrationBuilder.DropTable(
                name: "gov_rollout_records");

            migrationBuilder.DropTable(
                name: "gov_team_domain_links");

            migrationBuilder.DropTable(
                name: "gov_teams");

            migrationBuilder.DropTable(
                name: "gov_waivers");

            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
