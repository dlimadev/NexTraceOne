using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPrimaryProductionToEnvironment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimaryProduction",
                table: "identity_environments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_identity_environments_tenant_primary_production_unique",
                table: "identity_environments",
                columns: new[] { "TenantId", "IsPrimaryProduction" },
                unique: true,
                filter: "\"IsPrimaryProduction\" = true AND \"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_identity_environments_tenant_primary_production_unique",
                table: "identity_environments");

            migrationBuilder.DropColumn(
                name: "IsPrimaryProduction",
                table: "identity_environments");
        }
    }
}
