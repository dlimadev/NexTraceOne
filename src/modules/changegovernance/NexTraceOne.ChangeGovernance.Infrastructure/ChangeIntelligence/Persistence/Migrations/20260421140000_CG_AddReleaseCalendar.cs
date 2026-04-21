using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CG_AddReleaseCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tabela de janelas do Release Calendar (deploy, freeze, hotfix, maintenance)
            migrationBuilder.CreateTable(
                name: "chg_release_calendar_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WindowType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EnvironmentFilter = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecurrenceTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClosedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_release_calendar_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chg_release_calendar_tenant_id",
                table: "chg_release_calendar_entries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_release_calendar_tenant_type_status",
                table: "chg_release_calendar_entries",
                columns: new[] { "TenantId", "WindowType", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_release_calendar_tenant_period",
                table: "chg_release_calendar_entries",
                columns: new[] { "TenantId", "StartsAt", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "chg_release_calendar_entries");
        }
    }
}
