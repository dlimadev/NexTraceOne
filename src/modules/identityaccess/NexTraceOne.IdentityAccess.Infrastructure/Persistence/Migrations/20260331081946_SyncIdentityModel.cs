using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncIdentityModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c032"));

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "iam_tenants",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentTenantId",
                table: "iam_tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "iam_tenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantType",
                table: "iam_tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "iam_module_access_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Page = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_module_access_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_role_permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GrantedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_role_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_user_role_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_user_role_assignments", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c030"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "change-intelligence:read", "View change intelligence data" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c031"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "change-intelligence:write", "Create and manage changes" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c040"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "workflow:instances:read", "View workflow instances" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c041"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "workflow:instances:write", "Create and manage workflow instances" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c042"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "workflow:templates:write", "Create and manage workflow templates" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c050"),
                column: "Code",
                value: "promotion:requests:read");

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c051"),
                column: "Code",
                value: "promotion:requests:write");

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c052"),
                column: "Code",
                value: "promotion:environments:write");

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c060"),
                columns: new[] { "Code", "Module" },
                values: new object[] { "rulesets:read", "Rulesets" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c061"),
                columns: new[] { "Code", "Module" },
                values: new object[] { "rulesets:write", "Rulesets" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c070"),
                column: "Code",
                value: "audit:trail:read");

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c071"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "audit:reports:read", "View audit reports" });

            migrationBuilder.InsertData(
                table: "iam_permissions",
                columns: new[] { "Id", "Code", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c036"), "operations:mitigation:read", "Operations", "View mitigation actions" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c037"), "operations:mitigation:write", "Operations", "Create and manage mitigation actions" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c038"), "operations:runbooks:read", "Operations", "View runbooks" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c039"), "operations:runbooks:write", "Operations", "Create and manage runbooks" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c03a"), "operations:reliability:read", "Operations", "View service reliability data" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c03b"), "operations:reliability:write", "Operations", "Manage service reliability targets" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c03c"), "operations:runtime:read", "Operations", "View runtime status and health" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c03d"), "operations:runtime:write", "Operations", "Manage runtime configuration" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c03e"), "operations:cost:read", "Operations", "View operational cost data" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c03f"), "operations:cost:write", "Operations", "Manage operational cost allocations" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c043"), "operations:automation:read", "Operations", "View automation rules and history" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c044"), "operations:automation:write", "Operations", "Create and manage automation rules" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c045"), "operations:automation:execute", "Operations", "Execute automation actions" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c046"), "operations:automation:approve", "Operations", "Approve automation execution requests" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c053"), "promotion:gates:override", "Promotion", "Override promotion gates" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c062"), "rulesets:execute", "Rulesets", "Execute rulesets" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c072"), "audit:compliance:read", "Audit", "View compliance audit data" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c073"), "audit:compliance:write", "Audit", "Manage compliance audit records" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c074"), "audit:events:write", "Audit", "Write audit events" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c092"), "platform:admin:read", "Platform", "View platform administration data" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c100"), "developer-portal:read", "DeveloperPortal", "View developer portal content" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c101"), "developer-portal:write", "DeveloperPortal", "Manage developer portal content" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c110"), "governance:admin:read", "Governance", "View governance administration" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c111"), "governance:admin:write", "Governance", "Manage governance administration" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c112"), "governance:compliance:read", "Governance", "View compliance status" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c113"), "governance:controls:read", "Governance", "View governance controls" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c114"), "governance:domains:read", "Governance", "View governance domains" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c115"), "governance:domains:write", "Governance", "Manage governance domains" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c116"), "governance:evidence:read", "Governance", "View governance evidence" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c117"), "governance:finops:read", "Governance", "View FinOps governance data" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c118"), "governance:packs:read", "Governance", "View governance packs" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c119"), "governance:packs:write", "Governance", "Manage governance packs" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c11a"), "governance:policies:read", "Governance", "View governance policies" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c11b"), "governance:reports:read", "Governance", "View governance reports" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c11c"), "governance:risk:read", "Governance", "View risk assessments" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c11d"), "governance:teams:read", "Governance", "View governance team assignments" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c11e"), "governance:teams:write", "Governance", "Manage governance team assignments" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c11f"), "governance:waivers:read", "Governance", "View governance waivers" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c120"), "governance:waivers:write", "Governance", "Manage governance waivers" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c130"), "ai:assistant:read", "AI", "View AI assistant interactions" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c131"), "ai:assistant:write", "AI", "Use AI assistant" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c132"), "ai:governance:read", "AI", "View AI governance policies and usage" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c133"), "ai:governance:write", "AI", "Manage AI governance policies" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c134"), "ai:ide:read", "AI", "View AI IDE extension configuration" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c135"), "ai:ide:write", "AI", "Manage AI IDE extension configuration" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c136"), "ai:runtime:read", "AI", "View AI runtime status and models" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c137"), "ai:runtime:write", "AI", "Manage AI runtime and model registry" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c140"), "integrations:read", "Integrations", "View integrations and connectors" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c141"), "integrations:write", "Integrations", "Manage integrations and connectors" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c150"), "notifications:inbox:read", "Notifications", "View notification inbox" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c151"), "notifications:inbox:write", "Notifications", "Manage notification inbox" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c152"), "notifications:preferences:read", "Notifications", "View notification preferences" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c153"), "notifications:preferences:write", "Notifications", "Manage notification preferences" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c154"), "notifications:configuration:read", "Notifications", "View notification configuration" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c155"), "notifications:configuration:write", "Notifications", "Manage notification configuration" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c156"), "notifications:delivery:read", "Notifications", "View notification delivery status" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c160"), "env:environments:read", "Environment", "View environments" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c161"), "env:environments:write", "Environment", "Create and update environments" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c162"), "env:environments:admin", "Environment", "Administer environments" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c163"), "env:access:read", "Environment", "View environment access policies" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c164"), "env:access:admin", "Environment", "Administer environment access policies" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c170"), "configuration:read", "Configuration", "View system configuration" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c171"), "configuration:write", "Configuration", "Manage system configuration" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c180"), "analytics:read", "Analytics", "View analytics dashboards and data" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c181"), "analytics:write", "Analytics", "Manage analytics configuration" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_iam_tenants_parent",
                table: "iam_tenants",
                column: "ParentTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_tenants_type",
                table: "iam_tenants",
                column: "TenantType");

            migrationBuilder.CreateIndex(
                name: "ix_iam_module_access_policies_role_tenant_module_active",
                table: "iam_module_access_policies",
                columns: new[] { "RoleId", "TenantId", "Module", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_iam_module_access_policies_role_tenant_module_page_action",
                table: "iam_module_access_policies",
                columns: new[] { "RoleId", "TenantId", "Module", "Page", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iam_role_permissions_role_perm_tenant",
                table: "iam_role_permissions",
                columns: new[] { "RoleId", "PermissionCode", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iam_role_permissions_role_tenant_active",
                table: "iam_role_permissions",
                columns: new[] { "RoleId", "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_iam_user_role_assignments_tenant",
                table: "iam_user_role_assignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_iam_user_role_assignments_user_tenant_active",
                table: "iam_user_role_assignments",
                columns: new[] { "UserId", "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_iam_user_role_assignments_user_tenant_role",
                table: "iam_user_role_assignments",
                columns: new[] { "UserId", "TenantId", "RoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "iam_module_access_policies");

            migrationBuilder.DropTable(
                name: "iam_role_permissions");

            migrationBuilder.DropTable(
                name: "iam_user_role_assignments");

            migrationBuilder.DropIndex(
                name: "IX_iam_tenants_parent",
                table: "iam_tenants");

            migrationBuilder.DropIndex(
                name: "IX_iam_tenants_type",
                table: "iam_tenants");

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c036"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c037"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c038"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c039"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c03a"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c03b"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c03c"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c03d"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c03e"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c03f"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c043"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c044"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c045"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c046"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c053"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c062"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c072"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c073"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c074"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c092"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c100"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c101"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c110"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c111"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c112"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c113"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c114"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c115"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c116"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c117"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c118"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c119"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c11a"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c11b"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c11c"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c11d"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c11e"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c11f"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c120"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c130"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c131"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c132"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c133"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c134"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c135"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c136"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c137"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c140"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c141"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c150"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c151"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c152"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c153"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c154"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c155"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c156"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c160"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c161"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c162"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c163"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c164"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c170"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c171"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c180"));

            migrationBuilder.DeleteData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c181"));

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "iam_tenants");

            migrationBuilder.DropColumn(
                name: "ParentTenantId",
                table: "iam_tenants");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "iam_tenants");

            migrationBuilder.DropColumn(
                name: "TenantType",
                table: "iam_tenants");

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c030"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "change-intelligence:releases:read", "View releases" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c031"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "change-intelligence:releases:write", "Create and manage releases" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c040"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "workflow:read", "View workflow instances and templates" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c041"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "workflow:write", "Create and configure workflows" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c042"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "workflow:approve", "Approve or reject workflow stages" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c050"),
                column: "Code",
                value: "promotion:read");

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c051"),
                column: "Code",
                value: "promotion:write");

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c052"),
                column: "Code",
                value: "promotion:promote");

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c060"),
                columns: new[] { "Code", "Module" },
                values: new object[] { "ruleset-governance:read", "RulesetGovernance" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c061"),
                columns: new[] { "Code", "Module" },
                values: new object[] { "ruleset-governance:write", "RulesetGovernance" });

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c070"),
                column: "Code",
                value: "audit:read");

            migrationBuilder.UpdateData(
                table: "iam_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c071"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "audit:export", "Export audit data" });

            migrationBuilder.InsertData(
                table: "iam_permissions",
                columns: new[] { "Id", "Code", "Module", "Name" },
                values: new object[] { new Guid("2e91a557-fade-46df-b248-0f5f5899c032"), "change-intelligence:blast-radius:read", "ChangeIntelligence", "View blast radius reports" });
        }
    }
}
