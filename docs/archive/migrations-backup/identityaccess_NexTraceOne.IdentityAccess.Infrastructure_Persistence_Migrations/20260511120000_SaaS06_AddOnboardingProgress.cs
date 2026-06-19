using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SaaS06_AddOnboardingProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── SaaS-06: Onboarding Wizard ────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "iam_onboarding_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStep = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    completed_steps_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    SkippedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_onboarding_progress", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_iam_onboarding_progress_TenantId",
                table: "iam_onboarding_progress",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "iam_onboarding_progress");
        }
    }
}
