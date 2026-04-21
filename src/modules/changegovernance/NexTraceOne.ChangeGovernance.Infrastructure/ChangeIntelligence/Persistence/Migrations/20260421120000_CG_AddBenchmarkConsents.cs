using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CG_AddBenchmarkConsents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tabela de consentimentos LGPD/GDPR para participação em benchmarks cross-tenant anonimizados
            migrationBuilder.CreateTable(
                name: "chg_benchmark_consents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConsentedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsentedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LgpdLawfulBasis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_benchmark_consents", x => x.Id);
                });

            // Tabela de snapshots de métricas DORA para benchmarks cross-tenant
            migrationBuilder.CreateTable(
                name: "chg_benchmark_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeploymentFrequencyPerWeek = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    LeadTimeForChangesHours = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    ChangeFailureRatePercent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    MeanTimeToRestoreHours = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    MaturityScore = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    CostPerRequestUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    ServiceCount = table.Column<int>(type: "integer", nullable: false),
                    IsAnonymizedForBenchmarks = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_benchmark_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chg_benchmark_consents_tenant_id",
                table: "chg_benchmark_consents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_benchmark_snapshots_tenant_id",
                table: "chg_benchmark_snapshots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_chg_benchmark_snapshots_period",
                table: "chg_benchmark_snapshots",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_benchmark_snapshots_anonymized",
                table: "chg_benchmark_snapshots",
                column: "IsAnonymizedForBenchmarks",
                filter: "\"IsAnonymizedForBenchmarks\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "chg_benchmark_consents");
            migrationBuilder.DropTable(name: "chg_benchmark_snapshots");
        }
    }
}
