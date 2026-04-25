using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migration PIP-03/04/06: Adiciona tabelas de pipeline de ingestão configurável por tenant.
    ///   - int_tenant_pipeline_rules (PIP-03): regras de masking/filtering/enrichment/transform
    ///   - int_storage_buckets (PIP-04): buckets de routing com retenção e filtro por sinal
    ///   - int_log_to_metric_rules (PIP-06): regras de transformação log → métrica
    /// </summary>
    public partial class PIP03_PIP04_PIP06_AddPipelineInfrastructure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── PIP-03: Tenant Pipeline Rules ──
            migrationBuilder.CreateTable(
                name: "int_tenant_pipeline_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RuleType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SignalType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConditionJson = table.Column<string>(type: "jsonb", nullable: false),
                    ActionJson = table.Column<string>(type: "jsonb", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_tenant_pipeline_rules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_int_tenant_pipeline_rules_tenant_id",
                table: "int_tenant_pipeline_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_int_tenant_pipeline_rules_tenant_id_SignalType_IsEnabled",
                table: "int_tenant_pipeline_rules",
                columns: new[] { "tenant_id", "SignalType", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_int_tenant_pipeline_rules_tenant_id_RuleType_Priority",
                table: "int_tenant_pipeline_rules",
                columns: new[] { "tenant_id", "RuleType", "Priority" });

            // ── PIP-04: Storage Buckets ──
            migrationBuilder.CreateTable(
                name: "int_storage_buckets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BucketName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BackendType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    FilterJson = table.Column<string>(type: "jsonb", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsFallback = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_storage_buckets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_int_storage_buckets_tenant_id",
                table: "int_storage_buckets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_int_storage_buckets_tenant_id_IsEnabled_Priority",
                table: "int_storage_buckets",
                columns: new[] { "tenant_id", "IsEnabled", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_int_storage_buckets_tenant_id_BucketName",
                table: "int_storage_buckets",
                columns: new[] { "tenant_id", "BucketName" },
                unique: true);

            // ── PIP-06: Log-to-Metric Rules ──
            migrationBuilder.CreateTable(
                name: "int_log_to_metric_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Pattern = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MetricName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MetricType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValueExtractor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LabelsJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_int_log_to_metric_rules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_int_log_to_metric_rules_tenant_id",
                table: "int_log_to_metric_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_int_log_to_metric_rules_tenant_id_IsEnabled",
                table: "int_log_to_metric_rules",
                columns: new[] { "tenant_id", "IsEnabled" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "int_log_to_metric_rules");
            migrationBuilder.DropTable(name: "int_storage_buckets");
            migrationBuilder.DropTable(name: "int_tenant_pipeline_rules");
        }
    }
}
