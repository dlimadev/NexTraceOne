using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceLinksAndDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_discovered_services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ServiceNamespace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TraceCount = table.Column<long>(type: "bigint", nullable: false),
                    EndpointCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MatchedServiceAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscoveryRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    IgnoreReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_discovered_services", x => x.Id);
                    table.CheckConstraint("CK_cat_discovered_services_Status", "\"Status\" IN ('Pending', 'Matched', 'Ignored', 'Registered')");
                });

            migrationBuilder.CreateTable(
                name: "cat_discovery_match_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Pattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TargetServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_discovery_match_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_discovery_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServicesFound = table.Column<int>(type: "integer", nullable: false),
                    NewServicesFound = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_discovery_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_service_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    IconHint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_service_links", x => x.Id);
                    table.CheckConstraint("CK_cat_service_links_category", "\"Category\" IN ('Repository', 'Documentation', 'CiCd', 'Monitoring', 'Wiki', 'SwaggerUi', 'ApiPortal', 'Backstage', 'Adr', 'Runbook', 'Changelog', 'Dashboard', 'Other')");
                    table.ForeignKey(
                        name: "FK_cat_service_links_cat_service_assets_ServiceAssetId",
                        column: x => x.ServiceAssetId,
                        principalTable: "cat_service_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovered_services_Environment",
                table: "cat_discovered_services",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovered_services_LastSeenAt",
                table: "cat_discovered_services",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovered_services_ServiceName_Environment",
                table: "cat_discovered_services",
                columns: new[] { "ServiceName", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovered_services_Status",
                table: "cat_discovered_services",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovery_match_rules_IsActive",
                table: "cat_discovery_match_rules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovery_match_rules_IsActive_Priority",
                table: "cat_discovery_match_rules",
                columns: new[] { "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovery_runs_Environment",
                table: "cat_discovery_runs",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovery_runs_StartedAt",
                table: "cat_discovery_runs",
                column: "StartedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_links_ServiceAssetId",
                table: "cat_service_links",
                column: "ServiceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_links_ServiceAssetId_Category",
                table: "cat_service_links",
                columns: new[] { "ServiceAssetId", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_discovered_services");

            migrationBuilder.DropTable(
                name: "cat_discovery_match_rules");

            migrationBuilder.DropTable(
                name: "cat_discovery_runs");

            migrationBuilder.DropTable(
                name: "cat_service_links");
        }
    }
}
