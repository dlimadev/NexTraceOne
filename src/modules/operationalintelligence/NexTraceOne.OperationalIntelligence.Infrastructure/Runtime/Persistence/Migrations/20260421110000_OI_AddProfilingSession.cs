using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OI_AddProfilingSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cria tabela para sessões de profiling contínuo por serviço.
            // Suporta dotnet-trace, pprof, async-profiler e formatos genéricos.
            // Wave D: Continuous Profiling ingest contextualizado por serviço.
            migrationBuilder.CreateTable(
                name: "ops_profiling_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FrameType = table.Column<int>(type: "integer", nullable: false),
                    WindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    TotalCpuSamples = table.Column<long>(type: "bigint", nullable: false),
                    PeakMemoryMb = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PeakThreadCount = table.Column<int>(type: "integer", nullable: false),
                    TopFramesJson = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                    RawDataUri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RawDataHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReleaseVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CommitSha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HasAnomalies = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_profiling_sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ops_profiling_sessions_service_env_window",
                table: "ops_profiling_sessions",
                columns: new[] { "ServiceName", "Environment", "WindowStart" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_profiling_sessions_tenant_id",
                table: "ops_profiling_sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_profiling_sessions_has_anomalies",
                table: "ops_profiling_sessions",
                column: "HasAnomalies",
                filter: "\"HasAnomalies\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ops_profiling_sessions");
        }
    }
}
