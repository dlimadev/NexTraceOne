using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Migrations;

/// <inheritdoc />
public partial class AddTenantContextToIncidents : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "tenant_id",
            table: "oi_incidents",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "environment_id",
            table: "oi_incidents",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_oi_incidents_tenant_id",
            table: "oi_incidents",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_oi_incidents_tenant_environment",
            table: "oi_incidents",
            columns: new[] { "tenant_id", "environment_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ix_oi_incidents_tenant_environment", table: "oi_incidents");
        migrationBuilder.DropIndex(name: "ix_oi_incidents_tenant_id", table: "oi_incidents");
        migrationBuilder.DropColumn(name: "tenant_id", table: "oi_incidents");
        migrationBuilder.DropColumn(name: "environment_id", table: "oi_incidents");
    }
}
