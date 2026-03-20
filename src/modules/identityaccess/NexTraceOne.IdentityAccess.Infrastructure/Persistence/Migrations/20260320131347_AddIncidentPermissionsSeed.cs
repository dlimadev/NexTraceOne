using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NexTraceOne.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentPermissionsSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "identity_permissions",
                columns: new[] { "Id", "Code", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c034"), "operations:incidents:read", "Operations", "View operational incidents" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c035"), "operations:incidents:write", "Operations", "Create and manage operational incidents" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "identity_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c034"));

            migrationBuilder.DeleteData(
                table: "identity_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e91a557-fade-46df-b248-0f5f5899c035"));
        }
    }
}
