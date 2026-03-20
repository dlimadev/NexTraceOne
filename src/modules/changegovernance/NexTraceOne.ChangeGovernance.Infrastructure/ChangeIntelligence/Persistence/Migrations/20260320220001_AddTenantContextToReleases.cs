using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations;

/// <inheritdoc />
public partial class AddTenantContextToReleases : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "tenant_id",
            table: "ci_releases",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "environment_id",
            table: "ci_releases",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_ci_releases_tenant_id",
            table: "ci_releases",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_ci_releases_tenant_environment",
            table: "ci_releases",
            columns: new[] { "tenant_id", "environment_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ix_ci_releases_tenant_environment", table: "ci_releases");
        migrationBuilder.DropIndex(name: "ix_ci_releases_tenant_id", table: "ci_releases");
        migrationBuilder.DropColumn(name: "tenant_id", table: "ci_releases");
        migrationBuilder.DropColumn(name: "environment_id", table: "ci_releases");
    }
}
