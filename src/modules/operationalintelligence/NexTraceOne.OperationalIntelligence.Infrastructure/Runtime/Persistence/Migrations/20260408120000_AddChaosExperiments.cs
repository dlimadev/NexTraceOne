using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Migrations;

/// <inheritdoc />
public partial class AddChaosExperiments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ops_chaos_experiments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ExperimentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                TargetPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                Steps = table.Column<string>(type: "jsonb", nullable: false),
                SafetyChecks = table.Column<string>(type: "jsonb", nullable: false),
                ExecutionNotes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ops_chaos_experiments", x => x.Id);
                table.CheckConstraint("CK_ops_chaos_experiments_status", "\"Status\" >= 0 AND \"Status\" <= 4");
                table.CheckConstraint("CK_ops_chaos_experiments_duration", "\"DurationSeconds\" >= 10 AND \"DurationSeconds\" <= 3600");
                table.CheckConstraint("CK_ops_chaos_experiments_target_pct", "\"TargetPercentage\" >= 1 AND \"TargetPercentage\" <= 100");
            });

        migrationBuilder.CreateIndex(
            name: "IX_ops_chaos_experiments_TenantId",
            table: "ops_chaos_experiments",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_ops_chaos_experiments_TenantId_ServiceName",
            table: "ops_chaos_experiments",
            columns: new[] { "TenantId", "ServiceName" });

        migrationBuilder.CreateIndex(
            name: "IX_ops_chaos_experiments_TenantId_Status",
            table: "ops_chaos_experiments",
            columns: new[] { "TenantId", "Status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ops_chaos_experiments");
    }
}
