using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dep_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dep_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dep_service_dependency_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastScanAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SbomFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SbomContent = table.Column<string>(type: "text", nullable: true),
                    HealthScore = table.Column<int>(type: "integer", nullable: false),
                    TotalDependencies = table.Column<int>(type: "integer", nullable: false),
                    DirectDependencies = table.Column<int>(type: "integer", nullable: false),
                    TransitiveDependencies = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dep_service_dependency_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dep_vulnerability_advisory_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdvisoryId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CvssScore = table.Column<decimal>(type: "numeric(4,1)", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PackageName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AffectedVersionRange = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FixedInVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dep_vulnerability_advisory_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dep_package_dependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ecosystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDirect = table.Column<bool>(type: "boolean", nullable: false),
                    License = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LicenseRisk = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LatestStableVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsOutdated = table.Column<bool>(type: "boolean", nullable: false),
                    DeprecationNotice = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Vulnerabilities = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dep_package_dependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dep_package_dependencies_dep_service_dependency_profiles_Pr~",
                        column: x => x.ProfileId,
                        principalTable: "dep_service_dependency_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dep_outbox_messages_CreatedAt",
                table: "dep_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_dep_outbox_messages_IdempotencyKey",
                table: "dep_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dep_outbox_messages_ProcessedAt",
                table: "dep_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_dep_dependencies_package",
                table: "dep_package_dependencies",
                columns: new[] { "PackageName", "Ecosystem" });

            migrationBuilder.CreateIndex(
                name: "IX_dep_dependencies_profile",
                table: "dep_package_dependencies",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_dep_profiles_service",
                table: "dep_service_dependency_profiles",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_dep_profiles_template",
                table: "dep_service_dependency_profiles",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "ix_dep_vuln_advisory_active",
                table: "dep_vulnerability_advisory_records",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_dep_vuln_advisory_service",
                table: "dep_vulnerability_advisory_records",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "ix_dep_vuln_advisory_severity",
                table: "dep_vulnerability_advisory_records",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "uix_dep_vuln_advisory_service_advisory",
                table: "dep_vulnerability_advisory_records",
                columns: new[] { "ServiceId", "AdvisoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dep_outbox_messages");

            migrationBuilder.DropTable(
                name: "dep_package_dependencies");

            migrationBuilder.DropTable(
                name: "dep_vulnerability_advisory_records");

            migrationBuilder.DropTable(
                name: "dep_service_dependency_profiles");
        }
    }
}
