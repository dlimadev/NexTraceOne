using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P6_1_SloSlaBudgetBurnRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ops_slo_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TargetPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    AlertThresholdPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    WindowDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_slo_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_burn_rate_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SloDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Window = table.Column<int>(type: "integer", nullable: false),
                    BurnRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ObservedErrorRate = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    ToleratedErrorRate = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_burn_rate_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ops_burn_rate_snapshots_ops_slo_definitions_SloDefinitionId",
                        column: x => x.SloDefinitionId,
                        principalTable: "ops_slo_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ops_error_budget_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SloDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalBudgetMinutes = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ConsumedBudgetMinutes = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RemainingBudgetMinutes = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ConsumedPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_error_budget_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ops_error_budget_snapshots_ops_slo_definitions_SloDefinitio~",
                        column: x => x.SloDefinitionId,
                        principalTable: "ops_slo_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ops_sla_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SloDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContractualTargetPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HasPenaltyClauses = table.Column<bool>(type: "boolean", nullable: false),
                    PenaltyNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_sla_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ops_sla_definitions_ops_slo_definitions_SloDefinitionId",
                        column: x => x.SloDefinitionId,
                        principalTable: "ops_slo_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ops_burn_rate_snapshots_SloDefinitionId",
                table: "ops_burn_rate_snapshots",
                column: "SloDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_burn_rate_snapshots_TenantId_ServiceId_Window_ComputedAt",
                table: "ops_burn_rate_snapshots",
                columns: new[] { "TenantId", "ServiceId", "Window", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_burn_rate_snapshots_TenantId_SloDefinitionId_ComputedAt",
                table: "ops_burn_rate_snapshots",
                columns: new[] { "TenantId", "SloDefinitionId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_error_budget_snapshots_SloDefinitionId",
                table: "ops_error_budget_snapshots",
                column: "SloDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_error_budget_snapshots_TenantId_ServiceId_ComputedAt",
                table: "ops_error_budget_snapshots",
                columns: new[] { "TenantId", "ServiceId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_error_budget_snapshots_TenantId_SloDefinitionId_Compute~",
                table: "ops_error_budget_snapshots",
                columns: new[] { "TenantId", "SloDefinitionId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_sla_definitions_SloDefinitionId",
                table: "ops_sla_definitions",
                column: "SloDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_sla_definitions_TenantId_IsActive",
                table: "ops_sla_definitions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_sla_definitions_TenantId_SloDefinitionId",
                table: "ops_sla_definitions",
                columns: new[] { "TenantId", "SloDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_slo_definitions_TenantId_IsActive",
                table: "ops_slo_definitions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_slo_definitions_TenantId_ServiceId_Environment",
                table: "ops_slo_definitions",
                columns: new[] { "TenantId", "ServiceId", "Environment" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_burn_rate_snapshots");

            migrationBuilder.DropTable(
                name: "ops_error_budget_snapshots");

            migrationBuilder.DropTable(
                name: "ops_sla_definitions");

            migrationBuilder.DropTable(
                name: "ops_slo_definitions");
        }
    }
}
