using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLicensingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "licensing_licenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MaxActivations = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licensing_licenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "licensing_activations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HardwareFingerprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActivatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licensing_activations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_licensing_activations_licensing_licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "licensing_licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "licensing_capabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licensing_capabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_licensing_capabilities_licensing_licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "licensing_licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "licensing_hardware_bindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BoundAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licensing_hardware_bindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_licensing_hardware_bindings_licensing_licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "licensing_licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "licensing_usage_quotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Limit = table.Column<long>(type: "bigint", nullable: false),
                    CurrentUsage = table.Column<long>(type: "bigint", nullable: false),
                    AlertThresholdPercentage = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licensing_usage_quotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_licensing_usage_quotas_licensing_licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "licensing_licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_licensing_activations_LicenseId",
                table: "licensing_activations",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_licensing_capabilities_LicenseId",
                table: "licensing_capabilities",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_licensing_hardware_bindings_LicenseId",
                table: "licensing_hardware_bindings",
                column: "LicenseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_licensing_licenses_LicenseKey",
                table: "licensing_licenses",
                column: "LicenseKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_licensing_usage_quotas_LicenseId",
                table: "licensing_usage_quotas",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_CreatedAt",
                table: "outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                table: "outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "licensing_activations");

            migrationBuilder.DropTable(
                name: "licensing_capabilities");

            migrationBuilder.DropTable(
                name: "licensing_hardware_bindings");

            migrationBuilder.DropTable(
                name: "licensing_usage_quotas");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "licensing_licenses");
        }
    }
}
