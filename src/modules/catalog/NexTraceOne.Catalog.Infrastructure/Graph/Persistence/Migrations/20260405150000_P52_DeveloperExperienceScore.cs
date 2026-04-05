using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P52_DeveloperExperienceScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_dx_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CycleTimeHours = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    DeploymentFrequencyPerWeek = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    CognitivLoadScore = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    ToilPercentage = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    ScoreLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_dx_scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_productivity_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeploymentCount = table.Column<int>(type: "integer", nullable: false),
                    AverageCycleTimeHours = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    IncidentCount = table.Column<int>(type: "integer", nullable: false),
                    ManualStepsCount = table.Column<int>(type: "integer", nullable: false),
                    SnapshotSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_productivity_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_dx_scores_Period",
                table: "cat_dx_scores",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_cat_dx_scores_ScoreLevel",
                table: "cat_dx_scores",
                column: "ScoreLevel");

            migrationBuilder.CreateIndex(
                name: "IX_cat_dx_scores_TeamId",
                table: "cat_dx_scores",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_productivity_snapshots_PeriodEnd",
                table: "cat_productivity_snapshots",
                column: "PeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_cat_productivity_snapshots_PeriodStart",
                table: "cat_productivity_snapshots",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_cat_productivity_snapshots_TeamId",
                table: "cat_productivity_snapshots",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "cat_dx_scores");
            migrationBuilder.DropTable(name: "cat_productivity_snapshots");
        }
    }
}
