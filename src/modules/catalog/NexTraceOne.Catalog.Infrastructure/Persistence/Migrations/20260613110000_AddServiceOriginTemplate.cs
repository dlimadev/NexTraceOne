using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOriginTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OriginTemplateId",
                table: "ServiceAssets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginTemplateVersion",
                table: "ServiceAssets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginTemplateId",
                table: "ServiceAssets");

            migrationBuilder.DropColumn(
                name: "OriginTemplateVersion",
                table: "ServiceAssets");
        }
    }
}
