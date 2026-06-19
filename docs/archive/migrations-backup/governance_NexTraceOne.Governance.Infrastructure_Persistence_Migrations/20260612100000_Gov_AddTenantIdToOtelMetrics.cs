using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Gov_AddTenantIdToOtelMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Isolamento de tenant na telemetria OTEL — métricas de tenants
            // distintos estavam armazenadas sem discriminador.
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "OtelMetrics",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_OtelMetrics_TenantId_ServiceName_MetricName_Timestamp",
                table: "OtelMetrics",
                columns: new[] { "TenantId", "ServiceName", "MetricName", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtelMetrics_TenantId_ServiceName_MetricName_Timestamp",
                table: "OtelMetrics");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OtelMetrics");
        }
    }
}
