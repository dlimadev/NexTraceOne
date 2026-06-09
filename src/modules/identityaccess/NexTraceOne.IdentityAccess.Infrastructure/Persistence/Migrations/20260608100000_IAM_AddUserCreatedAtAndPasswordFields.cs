using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IAM_AddUserCreatedAtAndPasswordFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 1.2 + 2.1: CreatedAt, MustChangePassword, LastPasswordChangeAt para iam_users
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "iam_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "iam_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPasswordChangeAt",
                table: "iam_users",
                type: "timestamp with time zone",
                nullable: true);

            // Fase 2.7: ContactEmail, Timezone para iam_tenants
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "iam_tenants",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "iam_tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Fase 2.4: ExternalSubscriptionId, MaxOverageHostUnits para iam_tenant_licenses
            migrationBuilder.AddColumn<string>(
                name: "ExternalSubscriptionId",
                table: "iam_tenant_licenses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxOverageHostUnits",
                table: "iam_tenant_licenses",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedAt", table: "iam_users");
            migrationBuilder.DropColumn(name: "MustChangePassword", table: "iam_users");
            migrationBuilder.DropColumn(name: "LastPasswordChangeAt", table: "iam_users");
            migrationBuilder.DropColumn(name: "ContactEmail", table: "iam_tenants");
            migrationBuilder.DropColumn(name: "Timezone", table: "iam_tenants");
            migrationBuilder.DropColumn(name: "ExternalSubscriptionId", table: "iam_tenant_licenses");
            migrationBuilder.DropColumn(name: "MaxOverageHostUnits", table: "iam_tenant_licenses");
        }
    }
}
