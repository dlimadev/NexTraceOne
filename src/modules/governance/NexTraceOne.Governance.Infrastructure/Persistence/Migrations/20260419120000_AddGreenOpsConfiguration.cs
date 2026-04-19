using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adiciona tabela gov_greenops_configurations para persistência da configuração GreenOps por tenant.
/// Substitui leitura exclusiva de IConfiguration por dados persistidos em base de dados.
/// </summary>
public partial class AddGreenOpsConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "gov_greenops_configurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IntensityFactorKgPerKwh = table.Column<double>(type: "double precision", nullable: false),
                EsgTargetKgCo2PerMonth = table.Column<double>(type: "double precision", nullable: false),
                DatacenterRegion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_greenops_configurations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_gov_greenops_configurations_TenantId",
            table: "gov_greenops_configurations",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_greenops_configurations");
    }
}
