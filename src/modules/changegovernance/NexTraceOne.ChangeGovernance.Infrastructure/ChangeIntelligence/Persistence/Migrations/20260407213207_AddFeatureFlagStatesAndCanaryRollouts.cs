using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureFlagStatesAndCanaryRollouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "chg_releases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExternalValidationPassed",
                table: "chg_releases",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBreakingChanges",
                table: "chg_releases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReleaseName",
                table: "chg_releases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "chg_canary_rollouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolloutPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ActiveInstances = table.Column<int>(type: "integer", nullable: false),
                    TotalInstances = table.Column<int>(type: "integer", nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsPromoted = table.Column<bool>(type: "boolean", nullable: false),
                    IsAborted = table.Column<bool>(type: "boolean", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_canary_rollouts", x => x.Id);
                    table.CheckConstraint("CK_chg_canary_rollouts_active_instances", "\"ActiveInstances\" >= 0");
                    table.CheckConstraint("CK_chg_canary_rollouts_rollout_percentage", "\"RolloutPercentage\" >= 0 AND \"RolloutPercentage\" <= 100");
                    table.CheckConstraint("CK_chg_canary_rollouts_total_instances", "\"TotalInstances\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "chg_feature_flag_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActiveFlagCount = table.Column<int>(type: "integer", nullable: false),
                    CriticalFlagCount = table.Column<int>(type: "integer", nullable: false),
                    NewFeatureFlagCount = table.Column<int>(type: "integer", nullable: false),
                    FlagProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FlagsJson = table.Column<string>(type: "jsonb", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_feature_flag_states", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chg_canary_rollouts_ReleaseId",
                table: "chg_canary_rollouts",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chg_feature_flag_states_ReleaseId",
                table: "chg_feature_flag_states",
                column: "ReleaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chg_canary_rollouts");

            migrationBuilder.DropTable(
                name: "chg_feature_flag_states");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "chg_releases");

            migrationBuilder.DropColumn(
                name: "ExternalValidationPassed",
                table: "chg_releases");

            migrationBuilder.DropColumn(
                name: "HasBreakingChanges",
                table: "chg_releases");

            migrationBuilder.DropColumn(
                name: "ReleaseName",
                table: "chg_releases");
        }
    }
}
