using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvironmentProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "identity_environments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Criticality",
                table: "identity_environments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "identity_environments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProductionLike",
                table: "identity_environments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Profile",
                table: "identity_environments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "identity_environments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "identity_environments");

            migrationBuilder.DropColumn(
                name: "Criticality",
                table: "identity_environments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "identity_environments");

            migrationBuilder.DropColumn(
                name: "IsProductionLike",
                table: "identity_environments");

            migrationBuilder.DropColumn(
                name: "Profile",
                table: "identity_environments");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "identity_environments");
        }
    }
}
