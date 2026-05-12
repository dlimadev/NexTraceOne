using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IAM_AddEnvironmentAccessPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── W5-05: Fine-Grained Auth per Environment ──────────────────────
            migrationBuilder.CreateTable(
                name: "iam_environment_access_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Environments = table.Column<string>(type: "jsonb", nullable: false),
                    AllowedRoles = table.Column<string>(type: "jsonb", nullable: false),
                    RequireJitForRoles = table.Column<string>(type: "jsonb", nullable: false),
                    JitApprovalRequiredFrom = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_environment_access_policies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_iam_environment_access_policies_tenant",
                table: "iam_environment_access_policies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_environment_access_policies_tenant_name",
                table: "iam_environment_access_policies",
                columns: new[] { "TenantId", "PolicyName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "iam_environment_access_policies");
        }
    }
}
