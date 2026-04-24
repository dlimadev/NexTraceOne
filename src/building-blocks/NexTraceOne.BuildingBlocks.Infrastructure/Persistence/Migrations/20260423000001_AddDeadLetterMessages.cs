using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.BuildingBlocks.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDeadLetterMessages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "bb_dead_letter_messages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                MessageType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Payload = table.Column<string>(type: "text", nullable: false),
                FailureReason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                LastException = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                ExhaustedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ReprocessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bb_dead_letter_messages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bb_dead_letter_messages_ExhaustedAt",
            table: "bb_dead_letter_messages",
            column: "ExhaustedAt");

        migrationBuilder.CreateIndex(
            name: "IX_bb_dead_letter_messages_Status",
            table: "bb_dead_letter_messages",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_bb_dead_letter_messages_TenantId",
            table: "bb_dead_letter_messages",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_bb_dead_letter_messages_TenantId_Status",
            table: "bb_dead_letter_messages",
            columns: new[] { "TenantId", "Status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "bb_dead_letter_messages");
    }
}
