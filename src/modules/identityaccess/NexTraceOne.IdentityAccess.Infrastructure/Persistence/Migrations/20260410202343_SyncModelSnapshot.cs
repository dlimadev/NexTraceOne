using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "iam_roles",
                columns: new[] { "Id", "Description", "IsSystem", "Name" },
                values: new object[] { new Guid("1e91a557-fade-46df-b248-0f5f5899c008"), "Restricted to AI assistant access only", true, "AiUser" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "iam_roles",
                keyColumn: "Id",
                keyValue: new Guid("1e91a557-fade-46df-b248-0f5f5899c008"));
        }
    }
}
