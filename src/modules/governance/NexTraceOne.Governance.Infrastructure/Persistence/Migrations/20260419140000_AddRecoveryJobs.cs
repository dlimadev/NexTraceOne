using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRecoveryJobs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "gov_recovery_jobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RestorePointId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SchemasJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                DryRun = table.Column<bool>(type: "boolean", nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                InitiatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                InitiatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_recovery_jobs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_gov_recovery_jobs_InitiatedAt",
            table: "gov_recovery_jobs",
            column: "InitiatedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_recovery_jobs");
    }
}
