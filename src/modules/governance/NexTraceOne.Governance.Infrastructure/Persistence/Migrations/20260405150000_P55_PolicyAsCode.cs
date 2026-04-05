using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class P55_PolicyAsCode : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "gov_policy_as_code",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                DefinitionContent = table.Column<string>(type: "text", nullable: false),
                EnforcementMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                SimulatedAffectedServices = table.Column<int>(type: "integer", nullable: true),
                SimulatedNonCompliantServices = table.Column<int>(type: "integer", nullable: true),
                LastSimulatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RegisteredBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_policy_as_code", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_gov_policy_as_code_Name",
            table: "gov_policy_as_code",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_gov_policy_as_code_Status",
            table: "gov_policy_as_code",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_gov_policy_as_code_EnforcementMode",
            table: "gov_policy_as_code",
            column: "EnforcementMode");

        migrationBuilder.CreateIndex(
            name: "IX_gov_policy_as_code_TenantId",
            table: "gov_policy_as_code",
            column: "TenantId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_policy_as_code");
    }
}
