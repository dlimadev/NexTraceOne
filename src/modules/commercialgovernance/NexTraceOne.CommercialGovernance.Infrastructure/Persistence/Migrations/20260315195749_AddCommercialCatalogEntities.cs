using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.CommercialGovernance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialCatalogEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivationMode",
                table: "licensing_licenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommercialModel",
                table: "licensing_licenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeploymentModel",
                table: "licensing_licenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MeteringMode",
                table: "licensing_licenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "licensing_licenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "cc_feature_packs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cc_feature_packs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cc_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CommercialModel = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    DeploymentModel = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TrialDurationDays = table.Column<int>(type: "integer", nullable: true),
                    GracePeriodDays = table.Column<int>(type: "integer", nullable: false),
                    MaxActivations = table.Column<int>(type: "integer", nullable: false),
                    PriceTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cc_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "licensing_telemetry_consents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AllowUsageMetrics = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPerformanceData = table.Column<bool>(type: "boolean", nullable: false),
                    AllowErrorDiagnostics = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licensing_telemetry_consents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cc_feature_pack_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeaturePackId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CapabilityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DefaultLimit = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cc_feature_pack_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cc_feature_pack_items_cc_feature_packs_FeaturePackId",
                        column: x => x.FeaturePackId,
                        principalTable: "cc_feature_packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cc_feature_pack_items_FeaturePackId_CapabilityCode",
                table: "cc_feature_pack_items",
                columns: new[] { "FeaturePackId", "CapabilityCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cc_feature_packs_Code",
                table: "cc_feature_packs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cc_plans_Code",
                table: "cc_plans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_licensing_telemetry_consents_LicenseId",
                table: "licensing_telemetry_consents",
                column: "LicenseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cc_feature_pack_items");

            migrationBuilder.DropTable(
                name: "cc_plans");

            migrationBuilder.DropTable(
                name: "licensing_telemetry_consents");

            migrationBuilder.DropTable(
                name: "cc_feature_packs");

            migrationBuilder.DropColumn(
                name: "ActivationMode",
                table: "licensing_licenses");

            migrationBuilder.DropColumn(
                name: "CommercialModel",
                table: "licensing_licenses");

            migrationBuilder.DropColumn(
                name: "DeploymentModel",
                table: "licensing_licenses");

            migrationBuilder.DropColumn(
                name: "MeteringMode",
                table: "licensing_licenses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "licensing_licenses");
        }
    }
}
