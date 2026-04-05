using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P52B_DeveloperSurveys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dx_developer_surveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RespondentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NpsScore = table.Column<int>(type: "integer", nullable: false),
                    ToolSatisfaction = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ProcessSatisfaction = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    PlatformSatisfaction = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    NpsCategory = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dx_developer_surveys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dx_surveys_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dx_surveys_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dx_developer_surveys_Period",
                table: "dx_developer_surveys",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_dx_developer_surveys_SubmittedAt",
                table: "dx_developer_surveys",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_dx_developer_surveys_TeamId",
                table: "dx_developer_surveys",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_dx_developer_surveys_TeamId_Period",
                table: "dx_developer_surveys",
                columns: new[] { "TeamId", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_dx_surveys_outbox_messages_CreatedAt",
                table: "dx_surveys_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_dx_surveys_outbox_messages_IdempotencyKey",
                table: "dx_surveys_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dx_surveys_outbox_messages_ProcessedAt",
                table: "dx_surveys_outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "dx_developer_surveys");
            migrationBuilder.DropTable(name: "dx_surveys_outbox_messages");
        }
    }
}
