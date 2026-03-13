using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StronglyTypedIds_LicensingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnforcementLevel",
                table: "licensing_usage_quotas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GracePeriodDays",
                table: "licensing_usage_quotas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OverageDetectedAt",
                table: "licensing_usage_quotas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Edition",
                table: "licensing_licenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GracePeriodDays",
                table: "licensing_licenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TrialConverted",
                table: "licensing_licenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TrialConvertedAt",
                table: "licensing_licenses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrialExtensionCount",
                table: "licensing_licenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "licensing_licenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnforcementLevel",
                table: "licensing_usage_quotas");

            migrationBuilder.DropColumn(
                name: "GracePeriodDays",
                table: "licensing_usage_quotas");

            migrationBuilder.DropColumn(
                name: "OverageDetectedAt",
                table: "licensing_usage_quotas");

            migrationBuilder.DropColumn(
                name: "Edition",
                table: "licensing_licenses");

            migrationBuilder.DropColumn(
                name: "GracePeriodDays",
                table: "licensing_licenses");

            migrationBuilder.DropColumn(
                name: "TrialConverted",
                table: "licensing_licenses");

            migrationBuilder.DropColumn(
                name: "TrialConvertedAt",
                table: "licensing_licenses");

            migrationBuilder.DropColumn(
                name: "TrialExtensionCount",
                table: "licensing_licenses");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "licensing_licenses");
        }
    }
}
