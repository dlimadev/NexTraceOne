using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adiciona tabela gov_support_bundles para persistência de bundles de suporte gerados pela plataforma.
/// Substitui geração sintética por geração real de ZIP com dados de governança e configuração sanitizada.
/// </summary>
public partial class AddSupportBundles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "gov_support_bundles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                SizeMb = table.Column<double>(type: "double precision", nullable: true),
                ZipContent = table.Column<byte[]>(type: "bytea", nullable: true),
                IncludesLogs = table.Column<bool>(type: "boolean", nullable: false),
                IncludesConfig = table.Column<bool>(type: "boolean", nullable: false),
                IncludesDb = table.Column<bool>(type: "boolean", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_support_bundles", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_gov_support_bundles_Status",
            table: "gov_support_bundles",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_gov_support_bundles_TenantId",
            table: "gov_support_bundles",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_gov_support_bundles_RequestedAt",
            table: "gov_support_bundles",
            column: "RequestedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_support_bundles");
    }
}
